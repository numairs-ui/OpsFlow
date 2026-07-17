using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Features.Dashboard.Shared;
using OpsFlow.Api.Security;
using OpsFlow.Domain.Authorization;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Tasks.GetTaskStats;

internal sealed class GetTaskStatsHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<GetTaskStatsQuery, TaskStatsDto>
{
    public async Task<TaskStatsDto> Handle(GetTaskStatsQuery query, CancellationToken ct)
    {
        var spec = httpContextAccessor.HttpContext!.User.ToCaller().Scope();

        await using var db = await factory.CreateAsync(ct);

        var storeIds = await db.Stores
            .Where(s => s.IsActive)
            .WhereStoreInScope(spec, s => s.Id, s => s.RegionId)
            .Select(s => s.Id)
            .ToListAsync(ct);

        if (storeIds.Count == 0)
            return new TaskStatsDto(0, 0, 0, 0, 0, 0);

        var window = DashboardWindow.Today();

        // "Today" bucket (open/completed/completion rate) reuses the same aggregation the Overview
        // dashboards already use. Overdue/Upcoming are deliberately unbounded here — they mirror the
        // Tasks page's own Overdue/Upcoming tabs, which never window to "today" (see tasks.component.ts).
        var taskStats = await DashboardMetrics.GetStoreTaskStatsAsync(db, storeIds, window, ct);
        var openToday = taskStats.Values.Sum(s => s.Open);
        var completedToday = taskStats.Values.Sum(s => s.Completed);
        var totalToday = taskStats.Values.Sum(s => s.Total);

        var overdueCount = await db.TaskInstances
            .Where(t => storeIds.Contains(t.StoreId) && (t.Status == "Overdue" || t.Status == "CorrectiveActionRaised"))
            .CountAsync(ct);

        var correctiveActionCount = await db.TaskInstances
            .Where(t => storeIds.Contains(t.StoreId) && t.Status == "CorrectiveActionRaised")
            .CountAsync(ct);

        var upcomingCount = await db.TaskInstances
            .Where(t => storeIds.Contains(t.StoreId)
                && (t.Status == "Pending" || t.Status == "InProgress")
                && t.DueAt >= window.End)
            .CountAsync(ct);

        var completionRateToday = totalToday > 0 ? (double)completedToday / totalToday : 0;

        return new TaskStatsDto(
            openToday, upcomingCount, overdueCount, correctiveActionCount, completedToday,
            Math.Round(completionRateToday, 3));
    }
}
