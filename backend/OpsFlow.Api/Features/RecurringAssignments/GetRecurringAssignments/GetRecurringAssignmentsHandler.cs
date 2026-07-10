using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Security;
using OpsFlow.Domain.Authorization;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.RecurringAssignments.GetRecurringAssignments;

internal sealed class GetRecurringAssignmentsHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<GetRecurringAssignmentsQuery, List<RecurringAssignmentDto>>
{
    public async Task<List<RecurringAssignmentDto>> Handle(GetRecurringAssignmentsQuery query, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var spec = user.ToCaller().Scope();

        await using var db = await factory.CreateAsync(ct);

        var q = db.RecurringAssignments
            .Include(r => r.Checklist)
            .Include(r => r.TargetStores)
                .ThenInclude(ts => ts.Store)
            .AsQueryable();

        if (query.StoreId.HasValue)
            q = q.Where(r => r.TargetStores.Any(ts => ts.StoreId == query.StoreId.Value));

        if (query.IsPaused.HasValue)
            q = q.Where(r => r.IsPaused == query.IsPaused.Value);

        // Visible when any target store is in the caller's scope (store-scoped: own store;
        // region-scoped: a target whose region is in the set; super_admin: all).
        if (!spec.IsGlobal)
        {
            if (spec.IsStoreScoped)
            {
                var storeId = spec.StoreId ?? Guid.Empty;
                q = q.Where(r => r.TargetStores.Any(ts => ts.StoreId == storeId));
            }
            else
            {
                var regionIds = spec.RegionIds.ToList();
                q = q.Where(r => r.TargetStores.Any(ts => regionIds.Contains(ts.Store.RegionId)));
            }
        }

        return await q.OrderByDescending(r => r.CreatedAt).Select(r => new RecurringAssignmentDto(
            r.Id, r.Name,
            r.ChecklistId, r.Checklist!.Name,
            r.TargetStores
                .Select(ts => new RecurringAssignmentTargetDto(ts.StoreId, ts.Store.Name))
                .ToList(),
            r.CronExpression, r.StartsAt, r.EndsAt, r.IsPaused,
            r.TaskInstances.Count,
            r.CreatedAt,
            r.AssignedToUserId,
            r.AssignedToUserId != null
                ? db.UserProfiles.Where(u => u.UserId == r.AssignedToUserId).Select(u => u.DisplayName).FirstOrDefault()
                : null
        )).ToListAsync(ct);
    }
}
