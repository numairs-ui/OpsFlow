using Microsoft.EntityFrameworkCore;
using OpsFlow.Domain.Entities;
using OpsFlow.Infrastructure;
using Quartz;

namespace OpsFlow.Api.Jobs;

[DisallowConcurrentExecution]
internal sealed class GenerateTaskInstancesJob(IServiceScopeFactory scopeFactory, ILogger<GenerateTaskInstancesJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;
        var now = DateTimeOffset.UtcNow;
        var windowEnd = now.AddHours(1);

        await using var scope = scopeFactory.CreateAsyncScope();
        var masterDb = scope.ServiceProvider.GetRequiredService<MasterDbContext>();
        var factory = scope.ServiceProvider.GetRequiredService<TenantDbContextFactory>();

        var tenants = await masterDb.Tenants
            .Where(t => t.IsActive)
            .ToListAsync(ct);

        foreach (var tenant in tenants)
        {
            try
            {
                await GenerateForTenantAsync(tenant.Id, now, windowEnd, factory, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to generate task instances for tenant {TenantId}", tenant.Id);
            }
        }
    }

    private static async Task GenerateForTenantAsync(
        string tenantId,
        DateTimeOffset now,
        DateTimeOffset windowEnd,
        TenantDbContextFactory factory,
        CancellationToken ct)
    {
        await using var db = await factory.CreateForTenantAsync(tenantId, ct);

        var assignments = await db.RecurringAssignments
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

        await db.SaveChangesAsync(ct);
    }
}
