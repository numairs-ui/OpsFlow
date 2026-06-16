using Microsoft.AspNetCore.SignalR;
using OpsFlow.Domain.Interfaces;

namespace OpsFlow.Infrastructure.Azure;

internal sealed class AzureSignalRService(IHubContext<OpsFlowHub> hubContext) : IRealtimeService
{
    public async Task BroadcastAsync(string group, string eventName, object payload, CancellationToken ct = default)
        => await hubContext.Clients.Group(group).SendAsync(eventName, payload, ct);

    public async Task JoinGroupAsync(string connectionId, string group, CancellationToken ct = default)
        => await hubContext.Groups.AddToGroupAsync(connectionId, group, ct);
}

// Hub class — connection management delegated to Azure SignalR Service in production
public sealed class OpsFlowHub : Hub;
