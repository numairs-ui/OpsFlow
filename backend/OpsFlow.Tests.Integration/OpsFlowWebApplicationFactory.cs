using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using OpsFlow.Domain.Interfaces;
using OpsFlow.Infrastructure;

namespace OpsFlow.Tests.Integration;

/// <summary>
/// Shared WebApplicationFactory for integration tests that don't need tenant DB access.
/// Uses INFRASTRUCTURE_PROVIDER=inmemory which skips real DB and adapter registrations.
/// The ConfigureWebHost callback then injects null stubs for the three adapter interfaces.
/// </summary>
public sealed class OpsFlowWebApplicationFactory : WebApplicationFactory<Program>
{
    static OpsFlowWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("INFRASTRUCTURE_PROVIDER", "inmemory");
        Environment.SetEnvironmentVariable("DATABASE_PROVIDER", "inmemory");
        Environment.SetEnvironmentVariable("MASTER_DB_CONNECTION_STRING", "n/a");
        Environment.SetEnvironmentVariable("AZURE_BLOB_CONNECTION_STRING", "UseDevelopmentStorage=true");
        Environment.SetEnvironmentVariable("AZURE_BLOB_CONTAINER_NAME", "test-photos");
        Environment.SetEnvironmentVariable("JWT_SECRET", "test-integration-secret-long-enough-for-hmac256");
        Environment.SetEnvironmentVariable("JWT_ISSUER", "opsflow-test");
        Environment.SetEnvironmentVariable("JWT_AUDIENCE", "opsflow-test");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Inject null stubs for the three adapter interfaces not registered by inmemory provider
            services.AddSingleton<IStorageProvider, NullStorageProvider>();
            services.AddSingleton<IRealtimeService, NullRealtimeService>();
            services.AddSingleton<IAuthProvider, NullAuthProvider>();
        });
    }

    private sealed class NullStorageProvider : IStorageProvider
    {
        public Task<UploadUrlResult> GetUploadUrlAsync(string blobPath, TimeSpan expiry, CancellationToken ct = default)
            => Task.FromResult(new UploadUrlResult("http://test/upload", "http://test/blob"));
        public Task DeleteAsync(string blobPath, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class NullRealtimeService : IRealtimeService
    {
        public Task BroadcastAsync(string group, string eventName, object payload, CancellationToken ct = default) => Task.CompletedTask;
        public Task JoinGroupAsync(string connectionId, string group, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class NullAuthProvider : IAuthProvider
    {
        public Task<AuthResult?> AuthenticateAsync(string email, string password, string tenantId, CancellationToken ct = default)
            => Task.FromResult<AuthResult?>(null);
        public Task<string> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default)
            => Task.FromResult(Guid.NewGuid().ToString());
        public Task UpdateUserAsync(UpdateUserRequest request, CancellationToken ct = default) => Task.CompletedTask;
        public Task ResetPasswordAsync(string userId, string newPassword, CancellationToken ct = default) => Task.CompletedTask;
        public Task<string?> GetEmailAsync(string userId, CancellationToken ct = default) => Task.FromResult<string?>(null);
    }
}

