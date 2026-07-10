using MediatR;
using Microsoft.EntityFrameworkCore;
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
        var scope = user.ToCaller().Scope();

        await using var db = await factory.CreateAsync(ct);

        var targetIds = cmd.TargetStoreIds.Distinct().ToList();
        var stores = await db.Stores.Where(s => targetIds.Contains(s.Id)).ToListAsync(ct);

        // Every target must exist and be manageable by the caller — all-or-nothing, so a broadcast
        // never lands partially on stores the caller doesn't own.
        foreach (var storeId in targetIds)
        {
            var store = stores.FirstOrDefault(s => s.Id == storeId)
                ?? throw new KeyNotFoundException($"Store {storeId} not found.");
            scope.AssertCanManageStore(store.RegionId, store.Id);
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
            CronExpression = cmd.CronExpression,
            StartsAt = cmd.StartsAt,
            EndsAt = cmd.EndsAt,
            CreatedByUserId = userId,
            // Validator guarantees this is null when targeting more than one store.
            AssignedToUserId = cmd.AssignedToUserId,
            TargetStores = targetIds
                .Select(id => new RecurringAssignmentStore { StoreId = id })
                .ToList(),
        };

        db.RecurringAssignments.Add(assignment);
        await db.SaveChangesAsync(ct);
        return assignment.Id;
    }
}
