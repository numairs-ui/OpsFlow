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
            .Include(r => r.Store)
            .AsQueryable();

        if (query.StoreId.HasValue)
            q = q.Where(r => r.StoreId == query.StoreId.Value);

        if (query.IsPaused.HasValue)
            q = q.Where(r => r.IsPaused == query.IsPaused.Value);

        q = q.WhereStoreInScope(spec, r => r.StoreId, r => r.Store!.RegionId);

        return await q.OrderByDescending(r => r.CreatedAt).Select(r => new RecurringAssignmentDto(
            r.Id, r.Name,
            r.ChecklistId, r.Checklist!.Name,
            r.StoreId, r.Store!.Name,
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
