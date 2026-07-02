using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Hubs;
using OpsFlow.Infrastructure;
using Quartz;

namespace OpsFlow.Api.Jobs;

[DisallowConcurrentExecution]
internal sealed class ActivateDeferredTasksJob(
    IServiceScopeFactory scopeFactory,
    ILogger<ActivateDeferredTasksJob> logger) : TenantIteratingJob(scopeFactory, logger)
{
    protected override string JobName => "Activate deferred tasks";

    protected override async Task RunForTenantAsync(string tenantId, IServiceProvider services, CancellationToken ct)
    {
        var today = DateTimeOffset.UtcNow.Date;
        var factory = services.GetRequiredService<TenantDbContextFactory>();
        var hub = services.GetRequiredService<IHubContext<TaskBoardHub>>();

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
