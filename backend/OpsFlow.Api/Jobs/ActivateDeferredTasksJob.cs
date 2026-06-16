using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Hubs;
using OpsFlow.Infrastructure;
using Quartz;

namespace OpsFlow.Api.Jobs;

[DisallowConcurrentExecution]
internal sealed class ActivateDeferredTasksJob(
    IServiceScopeFactory scopeFactory,
    ILogger<ActivateDeferredTasksJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;
        var today = DateTimeOffset.UtcNow.Date;

        await using var scope = scopeFactory.CreateAsyncScope();
        var masterDb = scope.ServiceProvider.GetRequiredService<MasterDbContext>();
        var factory = scope.ServiceProvider.GetRequiredService<TenantDbContextFactory>();
        var hub = scope.ServiceProvider.GetRequiredService<IHubContext<TaskBoardHub>>();

        var tenants = await masterDb.Tenants.Where(t => t.IsActive).ToListAsync(ct);

        foreach (var tenant in tenants)
        {
            try { await ActivateForTenantAsync(tenant.Id, today, factory, hub, ct); }
            catch (Exception ex) { logger.LogError(ex, "Activate deferred tasks failed for tenant {TenantId}", tenant.Id); }
        }
    }

    private static async Task ActivateForTenantAsync(
        string tenantId, DateTimeOffset today,
        TenantDbContextFactory factory,
        IHubContext<TaskBoardHub> hub,
        CancellationToken ct)
    {
        await using var db = await factory.CreateForTenantAsync(tenantId, ct);

        var deferred = await db.TaskInstances
            .Where(t => t.Status == "Deferred" && t.DeferredTo <= today)
            .ToListAsync(ct);

        foreach (var task in deferred)
        {
            task.Status = "Pending";
            task.DeferredTo = null;
            task.DeferReason = null;
            task.DeferredByUserId = null;

            await hub.Clients.Group($"store-{task.StoreId}")
                .SendAsync("TaskUpdated", new { task.Id, task.Status }, ct);
        }

        await db.SaveChangesAsync(ct);
    }
}
