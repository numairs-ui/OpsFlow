using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Features.Dashboard.Shared;
using OpsFlow.Domain.Entities;
using OpsFlow.Infrastructure;
using Quartz;

namespace OpsFlow.Api.Jobs;

/// <summary>
/// Daily sweep: flags every active store that had not logged a deposit by its local deadline for the
/// current (UTC) business day. The flag is surfaced on the existing region/system dashboards; there is
/// deliberately no push/SignalR delivery (dashboard-flag-only escalation).
/// </summary>
[DisallowConcurrentExecution]
internal sealed class DepositEscalationJob(
    IServiceScopeFactory scopeFactory,
    ILogger<DepositEscalationJob> logger) : TenantIteratingJob(scopeFactory, logger)
{
    private static readonly TimeOnly DefaultDeadline = new(21, 0);

    protected override string JobName => "Deposit escalation";

    protected override async Task RunForTenantAsync(string tenantId, IServiceProvider services, CancellationToken ct)
    {
        var factory = services.GetRequiredService<TenantDbContextFactory>();
        await using var db = await factory.CreateForTenantAsync(tenantId, ct);

        // Org-wide default deposit deadline (used when a store has none of its own).
        var masterDb = services.GetRequiredService<MasterDbContext>();
        var tenantDefaultDeadline = await masterDb.Tenants
            .Where(t => t.Id == tenantId)
            .Select(t => t.DefaultDepositDeadlineLocalTime)
            .FirstOrDefaultAsync(ct);

        var flaggedCount = await FlagMissedDepositsAsync(db, tenantId, DateTimeOffset.UtcNow, ct, tenantDefaultDeadline);
        if (flaggedCount > 0)
            logger.LogInformation("Deposit escalation flagged {Count} store(s) for tenant {TenantId}.",
                flaggedCount, tenantId);
    }

    /// <summary>
    /// Core, side-effect-scoped logic (saves the flags it adds): flag every active store that hasn't
    /// logged a deposit in the current window and whose local deposit deadline has passed. Idempotent —
    /// stores already flagged for the business day are skipped. Returns how many new flags were written.
    /// </summary>
    internal static async Task<int> FlagMissedDepositsAsync(
        TenantDbContext db, string tenantId, DateTimeOffset utcNow, CancellationToken ct,
        TimeOnly? tenantDefaultDeadline = null)
    {
        // Same "today" the dashboards use, so the flag's business day lines up with what they read.
        var window = DashboardWindow.Today();
        var businessDate = DateOnly.FromDateTime(window.Start.UtcDateTime);

        var stores = await db.Stores.Where(s => s.IsActive).Select(s => new { s.Id }).ToListAsync(ct);
        if (stores.Count == 0) return 0;

        var storeIds = stores.Select(s => s.Id).ToList();

        var depositedSet = await DashboardMetrics.GetStoresWithDepositLoggedAsync(db, storeIds, window, ct);

        var settings = await db.StoreSettings
            .Where(s => storeIds.Contains(s.StoreId))
            .ToDictionaryAsync(s => s.StoreId, ct);

        var alreadyFlagged = (await db.MissedDepositFlags
                .Where(f => storeIds.Contains(f.StoreId) && f.BusinessDate == businessDate)
                .Select(f => f.StoreId)
                .ToListAsync(ct))
            .ToHashSet();

        var flaggedCount = 0;
        foreach (var store in stores)
        {
            if (depositedSet.Contains(store.Id) || alreadyFlagged.Contains(store.Id)) continue;

            settings.TryGetValue(store.Id, out var s);
            var deadline = s?.DepositDeadlineLocalTime ?? tenantDefaultDeadline ?? DefaultDeadline;
            var timezoneId = s?.TimezoneId ?? "America/New_York";

            if (!DeadlinePassed(utcNow, timezoneId, deadline)) continue;

            db.MissedDepositFlags.Add(new MissedDepositFlag
            {
                TenantId = tenantId,
                StoreId = store.Id,
                BusinessDate = businessDate,
                FlaggedAt = utcNow,
            });
            flaggedCount++;
        }

        if (flaggedCount > 0)
            await db.SaveChangesAsync(ct);

        return flaggedCount;
    }

    /// <summary>True when the store's local wall-clock time is at or past its deposit deadline.</summary>
    private static bool DeadlinePassed(DateTimeOffset utcNow, string timezoneId, TimeOnly deadline)
    {
        TimeZoneInfo tz;
        try { tz = TimeZoneInfo.FindSystemTimeZoneById(timezoneId); }
        catch { tz = TimeZoneInfo.Utc; }

        var local = TimeZoneInfo.ConvertTime(utcNow, tz);
        return TimeOnly.FromDateTime(local.DateTime) >= deadline;
    }
}
