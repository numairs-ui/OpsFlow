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
        // The system rollup spans every region — super_admin only.
        httpContextAccessor.HttpContext!.User.ToCaller().Scope().AssertGlobal();

        await using var db = await factory.CreateAsync(ct);

        var window = DashboardWindow.Today();

        var allStores = await db.Stores
            .Where(s => s.IsActive)
            .Select(s => new { s.Id, s.Name, s.RegionId })
            .ToListAsync(ct);

        var allRegions = await db.Regions
            .Where(r => r.IsActive)
            .Select(r => new { r.Id, r.Name })
            .ToListAsync(ct);

        if (allStores.Count == 0)
            return new SystemDashboardDto(0, 0, 0, [], []);

        var storeIds = allStores.Select(s => s.Id).ToList();

        var taskStats = await DashboardMetrics.GetStoreTaskStatsAsync(db, storeIds, window, ct);
        var depositSet = await DashboardMetrics.GetStoresWithDepositLoggedAsync(db, storeIds, window, ct);

        var allTotal = taskStats.Values.Sum(s => s.Total);
        var allCompleted = taskStats.Values.Sum(s => s.Completed);
        var systemRate = allTotal > 0 ? (double)allCompleted / allTotal : 0;
        var totalOpen = taskStats.Values.Sum(s => s.Open);
        var totalOverdue = taskStats.Values.Sum(s => s.Overdue);

        var missedDeposits = allStores
            .Where(s => !depositSet.Contains(s.Id))
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
                + regionStores.Count(s => !depositSet.Contains(s.Id));

            return new RegionalSummaryDto(r.Id, r.Name, regionStores.Count, Math.Round(rRate, 3), rCritical);
        }).ToList();

        return new SystemDashboardDto(
            Math.Round(systemRate, 3),
            totalOpen,
            totalOverdue,
            missedDeposits,
            regional);
    }
}
