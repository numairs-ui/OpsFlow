using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Infrastructure;
using System.Security.Claims;

namespace OpsFlow.Api.Features.RecurringAssignments.GetRecurringAssignments;

internal sealed class GetRecurringAssignmentsHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<GetRecurringAssignmentsQuery, List<RecurringAssignmentDto>>
{
    public async Task<List<RecurringAssignmentDto>> Handle(GetRecurringAssignmentsQuery query, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var role = user.FindFirstValue("role") ?? "";
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub")!;
        var regionId = user.FindFirstValue("regionId");

        await using var db = await factory.CreateAsync(ct);

        var q = db.RecurringAssignments
            .Include(r => r.Checklist)
            .Include(r => r.Store)
            .AsQueryable();

        if (query.StoreId.HasValue)
            q = q.Where(r => r.StoreId == query.StoreId.Value);

        if (query.IsPaused.HasValue)
            q = q.Where(r => r.IsPaused == query.IsPaused.Value);

        if (role == "store_manager")
        {
            var up = await db.UserProfiles.FindAsync([userId], ct);
            if (up?.StoreId != null) q = q.Where(r => r.StoreId == up.StoreId);
        }
        else if (role == "supervisor" && regionId != null)
        {
            var rid = Guid.Parse(regionId);
            q = q.Where(r => r.Store!.RegionId == rid);
        }

        var list = await q.OrderByDescending(r => r.CreatedAt).ToListAsync(ct);

        return list.Select(r => new RecurringAssignmentDto(
            r.Id, r.Name,
            r.ChecklistId, r.Checklist?.Name ?? "",
            r.StoreId, r.Store?.Name ?? "",
            r.CronExpression, r.StartsAt, r.EndsAt, r.IsPaused,
            r.TaskInstances.Count,
            r.CreatedAt
        )).ToList();
    }
}
