using Microsoft.EntityFrameworkCore;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Dashboard.Shared;

/// <summary>
/// Queries shared by the Store/Region/System dashboard rollups: per-store task stats for a window,
/// and which of those stores logged a deposit in that window.
/// </summary>
internal static class DashboardMetrics
{
    private static readonly string[] DoneStatuses = ["Completed", "Verified"];
    private static readonly string[] OpenStatuses = ["Pending", "InProgress"];
    private static readonly string[] OverdueStatuses = ["Overdue", "CorrectiveActionRaised"];
    private static readonly string[] ExcludedFromTotal = ["Cancelled", "Deferred"];

    public static async Task<Dictionary<Guid, StoreTaskStats>> GetStoreTaskStatsAsync(
        TenantDbContext db, IReadOnlyCollection<Guid> storeIds, DashboardWindow window, CancellationToken ct)
    {
        var stats = await db.TaskInstances
            .Where(t => storeIds.Contains(t.StoreId) && t.DueAt >= window.Start && t.DueAt < window.End)
            .GroupBy(t => t.StoreId)
            .Select(g => new StoreTaskStats(
                g.Key,
                g.Count(t => !ExcludedFromTotal.Contains(t.Status)),
                g.Count(t => DoneStatuses.Contains(t.Status)),
                g.Count(t => OpenStatuses.Contains(t.Status)),
                g.Count(t => OverdueStatuses.Contains(t.Status)),
                g.Count(t => t.Status == "CorrectiveActionRaised")))
            .ToListAsync(ct);

        return stats.ToDictionary(s => s.StoreId);
    }

    public static async Task<HashSet<Guid>> GetStoresWithDepositLoggedAsync(
        TenantDbContext db, IReadOnlyCollection<Guid> storeIds, DashboardWindow window, CancellationToken ct)
    {
        var deposited = await db.DepositLogs
            .Where(d => storeIds.Contains(d.StoreId) && d.SubmittedAt >= window.Start && d.SubmittedAt < window.End)
            .Select(d => d.StoreId)
            .ToListAsync(ct);

        return deposited.ToHashSet();
    }
}
