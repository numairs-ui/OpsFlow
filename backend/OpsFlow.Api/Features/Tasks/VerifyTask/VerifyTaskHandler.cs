using MediatR;
using Microsoft.AspNetCore.SignalR;
using OpsFlow.Api.Hubs;
using OpsFlow.Infrastructure;
using System.Security.Claims;

namespace OpsFlow.Api.Features.Tasks.VerifyTask;

internal sealed class VerifyTaskHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor,
    IHubContext<TaskBoardHub> hub) : IRequestHandler<VerifyTaskCommand>
{
    public async Task Handle(VerifyTaskCommand cmd, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");

        await using var db = await factory.CreateAsync(ct);

        var task = await db.TaskInstances.FindAsync([cmd.TaskId], ct)
            ?? throw new KeyNotFoundException($"Task {cmd.TaskId} not found.");

        if (task.Status != "Completed")
            throw new InvalidOperationException("Only completed tasks can be verified.");

        task.Status = "Verified";
        task.VerifiedByUserId = userId;
        task.VerifiedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);

        await hub.Clients.Group($"store-{task.StoreId}")
            .SendAsync("TaskUpdated", new { task.Id, task.Status }, ct);
    }
}
