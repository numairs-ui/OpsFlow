using MediatR;
using Microsoft.EntityFrameworkCore;
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

        var now        = DateTimeOffset.UtcNow;
        var todayStart = new DateTimeOffset(now.Date, TimeSpan.Zero);
        var todayEnd   = todayStart.AddDays(1);

        var stores = await db.Stores
            .Where(s => s.RegionId == query.RegionId && s.IsActive)
            .Select(s => new { s.Id, s.Name })
            .ToListAsync(ct);

        if (stores.Count == 0)
            return new RegionDashboardDto([]);

        var storeIds = stores.Select(s => s.Id).ToList();

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

        var result = stores.Select(store =>
        {
            var stats = taskStats.FirstOrDefault(s => s.StoreId == store.Id);
            var completionRate = stats is { Total: > 0 } ? (double)stats.Completed / stats.Total : 0;
            var corrRate = stats is { Total: > 0 } ? (double)(stats.Corrective) / stats.Total : 0;
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
