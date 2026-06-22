using MediatR;
using OpsFlow.Domain.Entities;
using OpsFlow.Infrastructure;
using System.Security.Claims;

namespace OpsFlow.Api.Features.RecurringAssignments.CreateRecurringAssignment;

internal sealed class CreateRecurringAssignmentHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<CreateRecurringAssignmentCommand, Guid>
{
    public async Task<Guid> Handle(CreateRecurringAssignmentCommand cmd, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var tenantId = user.FindFirstValue("tenantId")!;
        var role = user.FindFirstValue("role") ?? "";
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub")!;
        var regionId = user.FindFirstValue("regionId");

        await using var db = await factory.CreateAsync(ct);

        var store = await db.Stores.FindAsync([cmd.StoreId], ct)
            ?? throw new KeyNotFoundException($"Store {cmd.StoreId} not found.");

        // Role-based store access check
        if (role == "store_manager")
        {
            var userProfile = await db.UserProfiles.FindAsync([userId], ct);
            if (userProfile?.StoreId != cmd.StoreId)
                throw new UnauthorizedAccessException("Store managers can only create assignments for their own store.");
        }
        else if (role == "supervisor" && regionId != null)
        {
            if (store.RegionId.ToString() != regionId)
                throw new UnauthorizedAccessException("Supervisors can only create assignments for stores in their region.");
        }

        var checklist = await db.Checklists.FindAsync([cmd.ChecklistId], ct)
            ?? throw new KeyNotFoundException($"Checklist {cmd.ChecklistId} not found.");
        if (!checklist.IsActive)
            throw new InvalidOperationException("Cannot assign an inactive checklist.");

        var assignment = new RecurringAssignment
        {
            TenantId = tenantId,
            Name = cmd.Name,
            ChecklistId = cmd.ChecklistId,
            StoreId = cmd.StoreId,
            CronExpression = cmd.CronExpression,
            StartsAt = cmd.StartsAt,
            EndsAt = cmd.EndsAt,
            CreatedByUserId = userId,
            AssignedToUserId = cmd.AssignedToUserId,
        };

        db.RecurringAssignments.Add(assignment);
        await db.SaveChangesAsync(ct);
        return assignment.Id;
    }
}
