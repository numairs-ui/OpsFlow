namespace OpsFlow.Domain.Interfaces;

public interface IRealtimeService
{
    Task BroadcastAsync(string group, string eventName, object payload, CancellationToken ct = default);
    Task JoinGroupAsync(string connectionId, string group, CancellationToken ct = default);
}
