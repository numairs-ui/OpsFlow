using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Security;
using OpsFlow.Domain.Authorization;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Tasks.GetTodayTasks;

internal sealed class GetTodayTasksHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<GetTodayTasksQuery, TodayTasksDto>
{
    public async Task<TodayTasksDto> Handle(GetTodayTasksQuery query, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var spec = user.ToCaller().Scope();
        var userId = user.GetUserId();

        await using var db = await factory.CreateAsync(ct);

        // Auth: verify the caller may view this store (region set, or own/assigned store)
        var store = await db.Stores.FindAsync([query.StoreId], ct)
            ?? throw new KeyNotFoundException($"Store {query.StoreId} not found.");

        var assigned = spec.IsStoreScoped
            && await db.UserStoreAssignments.AnyAsync(a => a.UserId == userId && a.StoreId == query.StoreId, ct);
        spec.AssertCanViewStore(store.RegionId, store.Id, assigned);

        var today = DateTimeOffset.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var tasks = await db.TaskInstances
            .Include(t => t.Checklist)
            .Include(t => t.RecurringAssignment)
            .Where(t => t.StoreId == query.StoreId
                && t.DueAt >= today
                && t.DueAt < tomorrow
                && t.Status != "Cancelled"
                && t.Status != "Deferred")
            .OrderBy(t => t.DueAt)
            .ToListAsync(ct);

        var groups = tasks
            .GroupBy(t => t.ChecklistId)
            .Select(g =>
            {
                var items = g.OrderBy(t => t.DueAt).Select(t => new TaskBoardItemDto(
                    t.Id, t.DueAt, t.Status, t.AssignedToUserId,
                    t.RecurringAssignmentId == null,
                    t.RecurringAssignment?.Name,
                    t.CreatedAt
                )).ToList();

                return new TaskGroupDto(
                    g.Key,
                    // Tasks with no checklist collapse into one "Standalone Tasks" bucket on the board.
                    g.Key is null ? "Standalone Tasks" : (g.First().Checklist?.Name ?? "Unknown"),
                    items.Count,
                    items.Count(i => i.Status == "Completed"),
                    items
                );
            })
            .ToList();

        return new TodayTasksDto(
            today.ToString("yyyy-MM-dd"),
            store.Id, store.Name,
            tasks.Count,
            tasks.Count(t => t.Status == "Completed"),
            groups
        );
    }
}
