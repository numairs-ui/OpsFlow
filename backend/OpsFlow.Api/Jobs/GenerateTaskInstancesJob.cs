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
        var now = DateTimeOffset.UtcNow;
        var windowEnd = now.AddHours(1);
        var factory = services.GetRequiredService<TenantDbContextFactory>();

        await using var db = await factory.CreateForTenantAsync(tenantId, ct);

        var assignments = await db.RecurringAssignments
            .Where(a => !a.IsPaused
                && a.StartsAt <= now
                && (a.EndsAt == null || a.EndsAt > now))
            .ToListAsync(ct);

        foreach (var assignment in assignments)
        {
            try
            {
                var cron = new CronExpression(assignment.CronExpression);
                var nextFire = cron.GetNextValidTimeAfter(now);
                if (nextFire == null || nextFire > windowEnd) continue;

                var dueAt = nextFire.Value;
                var exists = await db.TaskInstances.AnyAsync(
                    t => t.RecurringAssignmentId == assignment.Id && t.DueAt == dueAt, ct);

                if (!exists)
                {
                    db.TaskInstances.Add(new TaskInstance
                    {
                        TenantId = tenantId,
                        RecurringAssignmentId = assignment.Id,
                        ChecklistId = assignment.ChecklistId,
                        StoreId = assignment.StoreId,
                        DueAt = dueAt,
                        CreatedByUserId = "system",
                    });
                }
            }
            catch (Exception ex)
            {
                // A single malformed CronExpression must not abort generation for every other
                // assignment in the tenant — it previously did, because this loop had no
                // per-iteration guard (see incident where one bad 5-field cron silently stalled
                // task generation tenant-wide for ~4 weeks).
                logger.LogError(ex,
                    "Failed to process recurring assignment {AssignmentId} ({Name}) for tenant {TenantId}; skipping it this tick.",
                    assignment.Id, assignment.Name, tenantId);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
