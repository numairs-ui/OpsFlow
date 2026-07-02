using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Features.Dashboard.Shared;
using OpsFlow.Api.Security;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Dashboard.GetRegionDashboard;

internal sealed class GetRegionDashboardHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<GetRegionDashboardQuery, RegionDashboardDto>
{
    public async Task<RegionDashboardDto> Handle(GetRegionDashboardQuery query, CancellationToken ct)
    {
        httpContextAccessor.HttpContext!.User.ToCaller().Scope().AssertCanViewRegion(query.RegionId);

        await using var db = await factory.CreateAsync(ct);

        var window = DashboardWindow.Today();

        var stores = await db.Stores
            .Where(s => s.RegionId == query.RegionId && s.IsActive)
            .Select(s => new { s.Id, s.Name })
            .ToListAsync(ct);

        if (stores.Count == 0)
            return new RegionDashboardDto([]);

        var storeIds = stores.Select(s => s.Id).ToList();

        var taskStats = await DashboardMetrics.GetStoreTaskStatsAsync(db, storeIds, window, ct);
        var depositSet = await DashboardMetrics.GetStoresWithDepositLoggedAsync(db, storeIds, window, ct);

        var result = stores.Select(store =>
        {
            taskStats.TryGetValue(store.Id, out var stats);
            var completionRate = stats?.CompletionRate ?? 0;
            var corrRate = stats?.CorrectiveRate ?? 0;
            var depositLogged = depositSet.Contains(store.Id);

            // Composite: completionRate*0.5 + (1-corrRate)*0.3 + depositCompliance*0.2
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

        return new RegionDashboardDto(result);
    }
}
