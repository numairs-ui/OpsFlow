using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Security;
using OpsFlow.Domain.Authorization;
using OpsFlow.Infrastructure;
using Quartz;

namespace OpsFlow.Api.Features.RecurringAssignments.GetRecurringHealth;

internal sealed class GetRecurringHealthHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<GetRecurringHealthQuery, RecurringHealthDto>
{
    public async Task<RecurringHealthDto> Handle(GetRecurringHealthQuery query, CancellationToken ct)
    {
        var spec = httpContextAccessor.HttpContext!.User.ToCaller().Scope();

        await using var db = await factory.CreateAsync(ct);

        var assignments = await db.RecurringAssignments
            .Include(r => r.Checklist)
            .Include(r => r.Store)
            .WhereStoreInScope(spec, r => r.StoreId, r => r.Store!.RegionId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        if (assignments.Count == 0)
            return new RecurringHealthDto(0, 0, 0, 0, []);

        var now = DateTimeOffset.UtcNow;
        var weekStart = now.AddDays(-7);
        var ids = assignments.Select(a => a.Id).ToList();

        var instanceStats = await db.TaskInstances
            .Where(t => t.RecurringAssignmentId != null && ids.Contains(t.RecurringAssignmentId!.Value))
            .GroupBy(t => t.RecurringAssignmentId!.Value)
            .Select(g => new
            {
                AssignmentId = g.Key,
                ThisWeek = g.Count(t => t.DueAt >= weekStart && t.DueAt <= now),
                LastGeneratedAt = g.Max(t => (DateTimeOffset?)t.DueAt),
            })
            .ToListAsync(ct);
        var statsById = instanceStats.ToDictionary(s => s.AssignmentId);

        var rows = assignments.Select(a =>
        {
            statsById.TryGetValue(a.Id, out var stats);
            var lastGeneratedAt = stats?.LastGeneratedAt;
            var nextFireAt = TryGetNextFireAt(a.CronExpression, now);
            var isStale = !a.IsPaused && HasMissedFire(a.CronExpression, lastGeneratedAt ?? a.StartsAt, now);

            return new RecurringAssignmentHealthDto(
                a.Id, a.Name, a.StoreId, a.Store?.Name ?? "", a.Checklist?.Name ?? "",
                a.IsPaused, a.CronExpression, nextFireAt, lastGeneratedAt,
                stats?.ThisWeek ?? 0, isStale);
        })
        .OrderByDescending(a => a.IsStale)
        .ThenBy(a => a.Name)
        .ToList();

        return new RecurringHealthDto(
            rows.Count(a => !a.IsPaused),
            rows.Count(a => a.IsPaused),
            rows.Sum(a => a.InstancesThisWeek),
            rows.Count(a => a.IsStale),
            rows);
    }

    private static DateTimeOffset? TryGetNextFireAt(string cronExpression, DateTimeOffset now)
    {
        try { return new CronExpression(cronExpression).GetNextValidTimeAfter(now); }
        catch { return null; }
    }

    // Reuses GenerateTaskInstancesJob's own contract: that job creates an instance once its next
    // fire falls within a rolling 1-hour lookahead, checked every 15 minutes. So a "stale" assignment
    // is one where the cron, walked forward from its last known fire, expected a fire more than an
    // hour ago that never produced an instance — the job's own lookahead should already have caught it.
    private static bool HasMissedFire(string cronExpression, DateTimeOffset from, DateTimeOffset now)
    {
        CronExpression cron;
        try { cron = new CronExpression(cronExpression); }
        catch { return true; } // malformed cron — surface it as a problem, not a false "all clear"

        var cursor = from;
        for (var i = 0; i < 1000; i++)
        {
            var next = cron.GetNextValidTimeAfter(cursor);
            if (next is null || next > now) return false; // nothing expected yet — healthy
            if (next < now.AddHours(-1)) return true;     // job's own lookahead should've caught this
            cursor = next.Value;
        }
        return false; // pathological cron (e.g. fires every second) — don't false-positive
    }
}
