using MediatR;
using Microsoft.AspNetCore.SignalR;
using OpsFlow.Api.Hubs;
using OpsFlow.Infrastructure;
using System.Security.Claims;

namespace OpsFlow.Api.Features.Tasks.AssignTask;

internal sealed class AssignTaskHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor,
    IHubContext<TaskBoardHub> hub) : IRequestHandler<AssignTaskCommand>
{
    public async Task Handle(AssignTaskCommand cmd, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var role = user.FindFirstValue("role") ?? "";

        if (!new[] { "store_manager", "supervisor", "admin" }.Contains(role))
            throw new UnauthorizedAccessException("Only managers can assign tasks.");

        await using var db = await factory.CreateAsync(ct);
        var task = await db.TaskInstances.FindAsync([cmd.TaskId], ct)
            ?? throw new KeyNotFoundException($"Task {cmd.TaskId} not found.");

        task.AssignedToUserId = cmd.AssignedToUserId;
        await db.SaveChangesAsync(ct);

        await hub.Clients.Group($"store-{task.StoreId}")
            .SendAsync("TaskUpdated", new { task.Id, task.Status, task.AssignedToUserId }, ct);
    }
}
