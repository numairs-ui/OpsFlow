using Microsoft.EntityFrameworkCore;
using OpsFlow.Domain.Entities;
using OpsFlow.Infrastructure;
using Quartz;

namespace OpsFlow.Api.Jobs;

[DisallowConcurrentExecution]
internal sealed class GenerateTaskInstancesJob(IServiceScopeFactory scopeFactory, ILogger<GenerateTaskInstancesJob> logger)
    : TenantIteratingJob(scopeFactory, logger)
{
    protected override string JobName => "Generate task instances";

    protected override async Task RunForTenantAsync(string tenantId, IServiceProvider services, CancellationToken ct)
    {
        var factory = services.GetRequiredService<TenantDbContextFactory>();
        await using var db = await factory.CreateForTenantAsync(tenantId, ct);
        await GenerateAsync(db, tenantId, DateTimeOffset.UtcNow, ct);
    }

    /// <summary>
    /// Core generation logic (saves what it adds): for each active assignment whose next cron firing
    /// lands in the [now, now+1h) window, create one task instance per target store, skipping any that
    /// already exist for that (assignment, dueAt, store). Returns how many instances were created.
    /// </summary>
    internal static async Task<int> GenerateAsync(
        TenantDbContext db, string tenantId, DateTimeOffset now, CancellationToken ct)
    {
        var windowEnd = now.AddHours(1);
        var created = 0;

        var assignments = await db.RecurringAssignments
            .Include(a => a.TargetStores)
            .Where(a => !a.IsPaused
                && a.StartsAt <= now
                && (a.EndsAt == null || a.EndsAt > now))
            .ToListAsync(ct);

        foreach (var assignment in assignments)
        {
            var cron = new CronExpression(assignment.CronExpression);
            var nextFire = cron.GetNextValidTimeAfter(now);
            if (nextFire == null || nextFire > windowEnd) continue;

            var dueAt = nextFire.Value;

            // Fan out one instance per target store per firing. The dedup check MUST include StoreId —
            // without it a multi-store assignment would generate only the first store's instance, since
            // AnyAsync(RecurringAssignmentId + DueAt) would match after the first store's insert.
            foreach (var target in assignment.TargetStores)
            {
                var exists = await db.TaskInstances.AnyAsync(
                    t => t.RecurringAssignmentId == assignment.Id
                        && t.DueAt == dueAt
                        && t.StoreId == target.StoreId, ct);

                if (!exists)
                {
                    db.TaskInstances.Add(new TaskInstance
                    {
                        TenantId = tenantId,
                        RecurringAssignmentId = assignment.Id,
                        ChecklistId = assignment.ChecklistId,
                        StoreId = target.StoreId,
                        DueAt = dueAt,
                        CreatedByUserId = "system",
                    });
                    created++;
                }
            }
        }

        await db.SaveChangesAsync(ct);
        return created;
    }
}
