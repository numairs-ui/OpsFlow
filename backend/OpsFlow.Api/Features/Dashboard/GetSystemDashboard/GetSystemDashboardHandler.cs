using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Features.Dashboard.Shared;
using OpsFlow.Api.Security;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Dashboard.GetSystemDashboard;

internal sealed class GetSystemDashboardHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<GetSystemDashboardQuery, SystemDashboardDto>
{
    public async Task<SystemDashboardDto> Handle(GetSystemDashboardQuery query, CancellationToken ct)
    {
        // super_admin sees the whole network; an admin/supervisor sees the same rollup narrowed to
        // its own region set (this is why admins can call the system endpoint directly rather than
        // client-aggregating per-region dashboards). Store-scoped roles have no network view.
        var scope = httpContextAccessor.HttpContext!.User.ToCaller().Scope();
        if (!scope.IsGlobal && !scope.IsRegionScoped)
            throw new UnauthorizedAccessException("You do not have access to the system dashboard.");

        var visibleRegionIds = scope.RegionIds.ToArray();

        await using var db = await factory.CreateAsync(ct);

        var window = DashboardWindow.Today();

        var allStores = await db.Stores
            .Where(s => s.IsActive && (scope.IsGlobal || visibleRegionIds.Contains(s.RegionId)))
            .Select(s => new { s.Id, s.Name, s.RegionId })
            .ToListAsync(ct);

        var allRegions = await db.Regions
            .Where(r => r.IsActive && (scope.IsGlobal || visibleRegionIds.Contains(r.Id)))
            .Select(r => new { r.Id, r.Name })
            .ToListAsync(ct);

        if (allStores.Count == 0)
            return new SystemDashboardDto(0, 0, 0, [], []);

        var storeIds = allStores.Select(s => s.Id).ToList();

        var taskStats = await DashboardMetrics.GetStoreTaskStatsAsync(db, storeIds, window, ct);
        var depositSet = await DashboardMetrics.GetStoresWithDepositLoggedAsync(db, storeIds, window, ct);
        // "Missed deposit" (deadline passed, none logged) is the escalation signal — distinct from
        // the live "logged today" indicator that feeds the per-store deposit score below.
        var missedSet = await DashboardMetrics.GetStoresWithMissedDepositAsync(db, storeIds, window, ct);

        var allTotal = taskStats.Values.Sum(s => s.Total);
        var allCompleted = taskStats.Values.Sum(s => s.Completed);
        var systemRate = allTotal > 0 ? (double)allCompleted / allTotal : 0;
        var totalOpen = taskStats.Values.Sum(s => s.Open);
        var totalOverdue = taskStats.Values.Sum(s => s.Overdue);

        var missedDeposits = allStores
            .Where(s => missedSet.Contains(s.Id))
            .Select(s => new MissedDepositStore(s.Id, s.Name))
            .ToList();

        var regional = allRegions.Select(r =>
        {
            var regionStores = allStores.Where(s => s.RegionId == r.Id).ToList();
            var regionStats = regionStores
                .Select(s => taskStats.GetValueOrDefault(s.Id))
                .Where(s => s != null)
                .Select(s => s!)
                .ToList();

            var rTotal = regionStats.Sum(s => s.Total);
            var rCompleted = regionStats.Sum(s => s.Completed);
            var rRate = rTotal > 0 ? (double)rCompleted / rTotal : 0;
            var rCritical = regionStats.Sum(s => s.Corrective)
                + regionStores.Count(s => missedSet.Contains(s.Id));

            var storeScores = regionStores.Select(store =>
            {
                taskStats.TryGetValue(store.Id, out var stats);
                var completionRate = stats?.CompletionRate ?? 0;
                var corrRate = stats?.CorrectiveRate ?? 0;
                var depositLogged = depositSet.Contains(store.Id);
                var depositScore = depositLogged ? 1.0 : 0.0;
                var composite = (completionRate * 0.5 + (1.0 - corrRate) * 0.3 + depositScore * 0.2) * 100;

                return new StoreScoreDto(
                    store.Id,
                    store.Name,
                    completionRate,
                    stats?.Open ?? 0,
                    stats?.Overdue ?? 0,
                    stats?.Corrective ?? 0,
                    depositLogged,
                    Math.Round(composite, 1));
            })
            .OrderByDescending(s => s.CompositeScore)
            .ToList();

            return new RegionalSummaryDto(r.Id, r.Name, regionStores.Count, Math.Round(rRate, 3), rCritical, storeScores);
        }).ToList();

        return new SystemDashboardDto(
            Math.Round(systemRate, 3),
            totalOpen,
            totalOverdue,
            missedDeposits,
            regional);
    }
}
