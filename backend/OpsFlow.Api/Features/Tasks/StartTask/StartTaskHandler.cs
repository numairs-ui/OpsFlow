using MediatR;
using Microsoft.AspNetCore.SignalR;
using OpsFlow.Api.Hubs;
using OpsFlow.Infrastructure;
using System.Security.Claims;

namespace OpsFlow.Api.Features.Tasks.StartTask;

internal sealed class StartTaskHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor,
    IHubContext<TaskBoardHub> hub) : IRequestHandler<StartTaskCommand>
{
    public async Task Handle(StartTaskCommand cmd, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub")!;

        await using var db = await factory.CreateAsync(ct);
        var task = await db.TaskInstances.FindAsync([cmd.TaskId], ct)
            ?? throw new KeyNotFoundException($"Task {cmd.TaskId} not found.");

        if (task.Status != "Pending") return; // already started or terminal — no-op

        task.Status = "InProgress";
        if (string.IsNullOrEmpty(task.AssignedToUserId))
            task.AssignedToUserId = userId;

        await db.SaveChangesAsync(ct);

        await hub.Clients.Group($"store-{task.StoreId}")
            .SendAsync("TaskUpdated", new { task.Id, task.Status, task.AssignedToUserId }, ct);
    }
}
