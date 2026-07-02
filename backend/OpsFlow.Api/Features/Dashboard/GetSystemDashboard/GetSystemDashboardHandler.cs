using MediatR;
using Microsoft.EntityFrameworkCore;
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

        var now        = DateTimeOffset.UtcNow;
        var todayStart = new DateTimeOffset(now.Date, TimeSpan.Zero);
        var todayEnd   = todayStart.AddDays(1);

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

        var taskStats = await db.TaskInstances
            .Where(t => storeIds.Contains(t.StoreId) && t.DueAt >= todayStart && t.DueAt < todayEnd)
            .GroupBy(t => t.StoreId)
            .Select(g => new
            {
                StoreId = g.Key,
                Total = g.Count(t => t.Status != "Cancelled" && t.Status != "Deferred"),
                Completed = g.Count(t => t.Status == "Completed" || t.Status == "Verified"),
                Open = g.Count(t => t.Status == "Pending" || t.Status == "InProgress"),
                Overdue = g.Count(t => t.Status == "Overdue" || t.Status == "CorrectiveActionRaised"),
                Corrective = g.Count(t => t.Status == "CorrectiveActionRaised")
            })
            .ToListAsync(ct);

        var deposits = await db.DepositLogs
            .Where(d => storeIds.Contains(d.StoreId) && d.SubmittedAt >= todayStart && d.SubmittedAt < todayEnd)
            .Select(d => d.StoreId)
            .ToListAsync(ct);

        var depositSet = deposits.ToHashSet();

        var allTotal = taskStats.Sum(s => s.Total);
        var allCompleted = taskStats.Sum(s => s.Completed);
        var systemRate = allTotal > 0 ? (double)allCompleted / allTotal : 0;
        var totalOpen = taskStats.Sum(s => s.Open);
        var totalOverdue = taskStats.Sum(s => s.Overdue);

        var missedDeposits = allStores
            .Where(s => !depositSet.Contains(s.Id))
            .Select(s => new MissedDepositStore(s.Id, s.Name))
            .ToList();

        var regional = allRegions.Select(r =>
        {
            var regionStores = allStores.Where(s => s.RegionId == r.Id).ToList();
            var regionStoreIds = regionStores.Select(s => s.Id).ToHashSet();
            var regionStats = taskStats.Where(s => regionStoreIds.Contains(s.StoreId)).ToList();

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
