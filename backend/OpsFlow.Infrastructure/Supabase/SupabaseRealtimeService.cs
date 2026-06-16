using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpsFlow.Domain.Interfaces;
using System.Net.Http.Json;

namespace OpsFlow.Infrastructure.Supabase;

/// <summary>
/// Server-side Supabase Realtime integration.
/// BroadcastAsync pushes to Supabase Realtime channels via the REST broadcast API.
/// JoinGroupAsync is a no-op — channel subscriptions are managed client-side in Angular.
/// </summary>
internal sealed class SupabaseRealtimeService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<SupabaseRealtimeService> logger) : IRealtimeService
{
    private readonly string _projectUrl = configuration["SUPABASE_URL"]!;
    private readonly string _serviceKey = configuration["SUPABASE_SERVICE_ROLE_KEY"]!;

    public async Task BroadcastAsync(string group, string eventName, object payload, CancellationToken ct = default)
    {
        try
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("apikey", _serviceKey);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_serviceKey}");

            var body = new
            {
                messages = new[]
                {
                    new
                    {
                        topic = $"realtime:opsflow:{group}",
                        @event = eventName,
                        payload,
                    },
                },
            };

            var response = await client.PostAsJsonAsync(
                $"{_projectUrl}/realtime/v1/api/broadcast", body, ct);

            if (!response.IsSuccessStatusCode)
                logger.LogWarning("Supabase broadcast to group '{Group}' returned {StatusCode}", group, response.StatusCode);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to broadcast event '{Event}' to group '{Group}'", eventName, group);
        }
    }

    // Supabase Realtime subscriptions are managed client-side — no server-side join needed
    public Task JoinGroupAsync(string connectionId, string group, CancellationToken ct = default)
        => Task.CompletedTask;
}
