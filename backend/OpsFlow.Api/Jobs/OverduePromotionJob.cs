using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Hubs;
using OpsFlow.Infrastructure;
using Quartz;

namespace OpsFlow.Api.Jobs;

[DisallowConcurrentExecution]
internal sealed class OverduePromotionJob(
    IServiceScopeFactory scopeFactory,
    ILogger<OverduePromotionJob> logger) : IJob
{
    private const int GraceMinutes = 30;

    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;
        var now = DateTimeOffset.UtcNow;

        await using var scope = scopeFactory.CreateAsyncScope();
        var masterDb = scope.ServiceProvider.GetRequiredService<MasterDbContext>();
        var factory = scope.ServiceProvider.GetRequiredService<TenantDbContextFactory>();
        var hub = scope.ServiceProvider.GetRequiredService<IHubContext<TaskBoardHub>>();

        var tenants = await masterDb.Tenants.Where(t => t.IsActive).ToListAsync(ct);

        foreach (var tenant in tenants)
        {
            try { await PromoteForTenantAsync(tenant.Id, now, factory, hub, ct); }
            catch (Exception ex) { logger.LogError(ex, "Overdue promotion failed for tenant {TenantId}", tenant.Id); }
        }
    }

    private static async Task PromoteForTenantAsync(
        string tenantId, DateTimeOffset now,
        TenantDbContextFactory factory,
        IHubContext<TaskBoardHub> hub,
        CancellationToken ct)
    {
        await using var db = await factory.CreateForTenantAsync(tenantId, ct);

        // Pass 1: Pending/InProgress past due → Overdue
        var overdueCandidates = await db.TaskInstances
            .Where(t => (t.Status == "Pending" || t.Status == "InProgress") && t.DueAt < now)
            .ToListAsync(ct);

        foreach (var task in overdueCandidates)
        {
            task.Status = "Overdue";
            await hub.Clients.Group($"store-{task.StoreId}")
                .SendAsync("TaskUpdated", new { task.Id, task.Status }, ct);
        }

        // Pass 2: Overdue past grace period → CorrectiveActionRaised
        var graceThreshold = now.AddMinutes(-GraceMinutes);
        var correctiveCandidates = await db.TaskInstances
            .Where(t => t.Status == "Overdue" && t.DueAt < graceThreshold)
            .ToListAsync(ct);

        foreach (var task in correctiveCandidates)
        {
            task.Status = "CorrectiveActionRaised";
            await hub.Clients.Group($"store-{task.StoreId}")
                .SendAsync("TaskUpdated", new { task.Id, task.Status }, ct);
        }

        await db.SaveChangesAsync(ct);
    }
}
