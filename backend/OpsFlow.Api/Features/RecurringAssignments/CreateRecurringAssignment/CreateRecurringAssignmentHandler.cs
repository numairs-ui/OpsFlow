using MediatR;
using OpsFlow.Api.Security;
using OpsFlow.Domain.Authorization;
using OpsFlow.Domain.Entities;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.RecurringAssignments.CreateRecurringAssignment;

internal sealed class CreateRecurringAssignmentHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<CreateRecurringAssignmentCommand, Guid>
{
    public async Task<Guid> Handle(CreateRecurringAssignmentCommand cmd, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var tenantId = user.GetTenantId();
        var userId = user.GetUserId();

        await using var db = await factory.CreateAsync(ct);

        var store = await db.Stores.FindAsync([cmd.StoreId], ct)
            ?? throw new KeyNotFoundException($"Store {cmd.StoreId} not found.");

        // Manager (own store), region role (in-region), or super_admin may create assignments.
        user.ToCaller().Scope().AssertCanManageStore(store.RegionId, store.Id);

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
