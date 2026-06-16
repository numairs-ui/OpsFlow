using MediatR;
using Microsoft.AspNetCore.SignalR;
using OpsFlow.Api.Hubs;
using OpsFlow.Infrastructure;
using System.Security.Claims;

namespace OpsFlow.Api.Features.Tasks.CancelTask;

internal sealed class CancelTaskHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor,
    IHubContext<TaskBoardHub> hub) : IRequestHandler<CancelTaskCommand>
{
    private static readonly HashSet<string> Terminal = ["Completed", "Verified", "Cancelled"];

    public async Task Handle(CancelTaskCommand cmd, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");

        await using var db = await factory.CreateAsync(ct);

        var task = await db.TaskInstances.FindAsync([cmd.TaskId], ct)
            ?? throw new KeyNotFoundException($"Task {cmd.TaskId} not found.");

        if (Terminal.Contains(task.Status))
            throw new InvalidOperationException($"Cannot cancel a task with status '{task.Status}'.");

        task.Status = "Cancelled";
        task.CancelledByUserId = userId;
        task.CancelReason = cmd.Reason;
        task.CancelledAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);

        await hub.Clients.Group($"store-{task.StoreId}")
            .SendAsync("TaskUpdated", new { task.Id, task.Status, task.CancelReason }, ct);
    }
}
