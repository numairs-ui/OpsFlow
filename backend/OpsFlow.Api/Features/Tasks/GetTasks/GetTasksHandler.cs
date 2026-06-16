using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Infrastructure;
using System.Security.Claims;

namespace OpsFlow.Api.Features.Tasks.GetTasks;

internal sealed class GetTasksHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<GetTasksQuery, List<TaskInstanceDto>>
{
    public async Task<List<TaskInstanceDto>> Handle(GetTasksQuery query, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var role = user.FindFirstValue("role") ?? "";
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub")!;
        var regionId = user.FindFirstValue("regionId");

        await using var db = await factory.CreateAsync(ct);

        var q = db.TaskInstances
            .Include(t => t.Checklist)
            .Include(t => t.Store)
            .Include(t => t.RecurringAssignment)
            .AsQueryable();

        if (query.StoreId.HasValue)
            q = q.Where(t => t.StoreId == query.StoreId.Value);
        if (!string.IsNullOrWhiteSpace(query.Status))
            q = q.Where(t => t.Status == query.Status);
        if (query.From.HasValue)
            q = q.Where(t => t.DueAt >= query.From.Value);
        if (query.To.HasValue)
            q = q.Where(t => t.DueAt <= query.To.Value);

        if (role == "store_manager" || role == "store_employee")
        {
            var up = await db.UserProfiles.FindAsync([userId], ct);
            if (up?.StoreId != null) q = q.Where(t => t.StoreId == up.StoreId);
        }
        else if (role == "supervisor" && regionId != null)
        {
            var rid = Guid.Parse(regionId);
            q = q.Where(t => t.Store!.RegionId == rid);
        }

        var list = await q.OrderBy(t => t.DueAt).ToListAsync(ct);

        return list.Select(t => new TaskInstanceDto(
            t.Id, t.RecurringAssignmentId, t.RecurringAssignment?.Name,
            t.ChecklistId, t.Checklist?.Name ?? "",
            t.StoreId, t.Store?.Name ?? "",
            t.DueAt, t.Status,
            t.AssignedToUserId, t.CompletedByUserId, t.CompletedAt,
            t.Notes, t.RecurringAssignmentId == null,
            t.CreatedAt
        )).ToList();
    }
}
