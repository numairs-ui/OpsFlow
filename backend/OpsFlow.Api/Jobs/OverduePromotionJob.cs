using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Hubs;
using OpsFlow.Infrastructure;
using Quartz;

namespace OpsFlow.Api.Jobs;

[DisallowConcurrentExecution]
internal sealed class OverduePromotionJob(
    IServiceScopeFactory scopeFactory,
    ILogger<OverduePromotionJob> logger) : TenantIteratingJob(scopeFactory, logger)
{
    private const int GraceMinutes = 30;

    protected override string JobName => "Overdue promotion";

    protected override async Task RunForTenantAsync(string tenantId, IServiceProvider services, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var factory = services.GetRequiredService<TenantDbContextFactory>();
        var hub = services.GetRequiredService<IHubContext<TaskBoardHub>>();

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
