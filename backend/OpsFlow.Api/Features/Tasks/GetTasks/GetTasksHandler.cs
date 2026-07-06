using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Security;
using OpsFlow.Domain.Authorization;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Tasks.GetTasks;

internal sealed class GetTasksHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<GetTasksQuery, List<TaskInstanceDto>>
{
    public async Task<List<TaskInstanceDto>> Handle(GetTasksQuery query, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var spec = user.ToCaller().Scope();

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
        if (query.Statuses is { Count: > 0 })
            q = q.Where(t => query.Statuses.Contains(t.Status));
        if (query.From.HasValue)
            q = q.Where(t => t.DueAt >= query.From.Value);
        if (query.To.HasValue)
            q = q.Where(t => t.DueAt <= query.To.Value);

        q = q.WhereStoreInScope(spec, t => t.StoreId, t => t.Store!.RegionId);

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
