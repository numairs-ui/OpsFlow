using MediatR;
using Microsoft.AspNetCore.SignalR;
using OpsFlow.Api.Hubs;
using OpsFlow.Infrastructure;
using System.Security.Claims;

namespace OpsFlow.Api.Features.Tasks.ClaimTask;

internal sealed class ClaimTaskHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor,
    IHubContext<TaskBoardHub> hub) : IRequestHandler<ClaimTaskCommand>
{
    public async Task Handle(ClaimTaskCommand cmd, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");

        await using var db = await factory.CreateAsync(ct);
        var task = await db.TaskInstances.FindAsync([cmd.TaskId], ct)
            ?? throw new KeyNotFoundException($"Task {cmd.TaskId} not found.");

        if (!string.IsNullOrEmpty(task.AssignedToUserId))
            throw new InvalidOperationException("Task is already claimed.");

        task.AssignedToUserId = cmd.VolunteerName != null
            ? $"volunteer:{cmd.VolunteerName}"
            : userId;

        await db.SaveChangesAsync(ct);

        await hub.Clients.Group($"store-{task.StoreId}")
            .SendAsync("TaskUpdated", new { task.Id, task.Status, task.AssignedToUserId }, ct);
    }
}
