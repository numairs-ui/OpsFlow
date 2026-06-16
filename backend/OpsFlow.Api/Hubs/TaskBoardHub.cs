using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace OpsFlow.Api.Hubs;

[Authorize]
public sealed class TaskBoardHub : Hub
{
    public async Task JoinStoreGroup(string storeId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"store-{storeId}");
    }

    public async Task LeaveStoreGroup(string storeId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"store-{storeId}");
    }
}
