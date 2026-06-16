using MediatR;
using Microsoft.AspNetCore.SignalR;
using OpsFlow.Api.Hubs;
using OpsFlow.Infrastructure;
using System.Security.Claims;

namespace OpsFlow.Api.Features.Tasks.DeferTask;

internal sealed class DeferTaskHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor,
    IHubContext<TaskBoardHub> hub) : IRequestHandler<DeferTaskCommand>
{
    private static readonly HashSet<string> NonDeferrable = ["Completed", "Verified", "Cancelled", "Deferred"];

    public async Task Handle(DeferTaskCommand cmd, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");

        await using var db = await factory.CreateAsync(ct);

        var task = await db.TaskInstances.FindAsync([cmd.TaskId], ct)
            ?? throw new KeyNotFoundException($"Task {cmd.TaskId} not found.");

        if (NonDeferrable.Contains(task.Status))
            throw new InvalidOperationException($"Cannot defer a task with status '{task.Status}'.");

        task.Status = "Deferred";
        task.DeferredTo = cmd.DeferredTo.Date;
        task.DeferReason = cmd.Reason;
        task.DeferredByUserId = userId;

        await db.SaveChangesAsync(ct);

        await hub.Clients.Group($"store-{task.StoreId}")
            .SendAsync("TaskUpdated", new { task.Id, task.Status, task.DeferredTo }, ct);
    }
}
