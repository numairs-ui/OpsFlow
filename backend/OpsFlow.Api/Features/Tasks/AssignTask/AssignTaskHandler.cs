using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Hubs;
using OpsFlow.Api.Security;
using OpsFlow.Domain.Authorization;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Tasks.AssignTask;

internal sealed class AssignTaskHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor,
    IHubContext<TaskBoardHub> hub) : IRequestHandler<AssignTaskCommand>
{
    public async Task Handle(AssignTaskCommand cmd, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var spec = user.ToCaller().Scope();
        var userId = user.GetUserId();

        await using var db = await factory.CreateAsync(ct);
        var task = await db.TaskInstances.FindAsync([cmd.TaskId], ct)
            ?? throw new KeyNotFoundException($"Task {cmd.TaskId} not found.");

        // Manager (own or assigned store), region role (in-region), or super_admin may assign.
        var store = await db.Stores.FindAsync([task.StoreId], ct)
            ?? throw new KeyNotFoundException($"Store {task.StoreId} not found.");
        var assigned = spec.IsStoreScoped
            && await db.UserStoreAssignments.AnyAsync(a => a.UserId == userId && a.StoreId == task.StoreId, ct);
        spec.AssertCanManageStore(store.RegionId, store.Id, assigned);

        task.AssignedToUserId = cmd.AssignedToUserId;
        await db.SaveChangesAsync(ct);

        await hub.Clients.Group($"store-{task.StoreId}")
            .SendAsync("TaskUpdated", new { task.Id, task.Status, task.AssignedToUserId }, ct);
    }
}
