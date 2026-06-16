using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Infrastructure;
using System.Security.Claims;

namespace OpsFlow.Api.Features.Tasks.GetTodayTasks;

internal sealed class GetTodayTasksHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<GetTodayTasksQuery, TodayTasksDto>
{
    public async Task<TodayTasksDto> Handle(GetTodayTasksQuery query, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var role = user.FindFirstValue("role") ?? "";
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub")!;
        var regionId = user.FindFirstValue("regionId");

        await using var db = await factory.CreateAsync(ct);

        // Auth: verify user has access to this store
        var store = await db.Stores.FindAsync([query.StoreId], ct)
            ?? throw new KeyNotFoundException($"Store {query.StoreId} not found.");

        if (role == "store_manager" || role == "store_employee")
        {
            var up = await db.UserProfiles.FindAsync([userId], ct);
            var isAssigned = up?.StoreId == query.StoreId
                || await db.UserStoreAssignments.AnyAsync(a => a.UserId == userId && a.StoreId == query.StoreId, ct);
            if (!isAssigned)
                throw new UnauthorizedAccessException("You do not have access to this store.");
        }
        else if (role == "supervisor" && regionId != null)
        {
            if (store.RegionId.ToString() != regionId)
                throw new UnauthorizedAccessException("Store is not in your region.");
        }

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
                    g.First().Checklist?.Name ?? "Unknown",
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
