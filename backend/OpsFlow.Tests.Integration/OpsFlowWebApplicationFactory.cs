using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpsFlow.Domain.Interfaces;
using OpsFlow.Infrastructure;

namespace OpsFlow.Tests.Integration;

/// <summary>
/// Shared WebApplicationFactory for integration tests.
/// Environment variables are set in the static constructor so WebApplication.CreateBuilder
/// picks them up via AddEnvironmentVariables before service registration reads them.
/// </summary>
public sealed class OpsFlowWebApplicationFactory : WebApplicationFactory<Program>
{
    static OpsFlowWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("INFRASTRUCTURE_PROVIDER", "azure");
        Environment.SetEnvironmentVariable("DATABASE_PROVIDER", "azure");
        Environment.SetEnvironmentVariable("MASTER_DB_CONNECTION_STRING", "Server=.;Database=test-master;");
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
            // Replace real DbContexts with in-memory so no SQL Server or Azurite is required
            services.RemoveAll<DbContextOptions<MasterDbContext>>();
            services.AddDbContext<MasterDbContext>(opts =>
                opts.UseInMemoryDatabase("test-master-db"));

            services.RemoveAll<DbContextOptions<TenantDbContext>>();
            services.AddDbContext<TenantDbContext>(opts =>
                opts.UseInMemoryDatabase("test-tenant-db"));

            // Replace storage/realtime/auth adapters with no-op stubs
            services.RemoveAll<IStorageProvider>();
            services.AddSingleton<IStorageProvider, NullStorageProvider>();

            services.RemoveAll<IRealtimeService>();
            services.AddSingleton<IRealtimeService, NullRealtimeService>();

            services.RemoveAll<IAuthProvider>();
            services.AddSingleton<IAuthProvider, NullAuthProvider>();
        });
    }

    // Null implementations — used in tests only
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
            => Task.FromResult("test-user-id");

        public Task ResetPasswordAsync(string userId, string newPassword, CancellationToken ct = default) => Task.CompletedTask;
    }
}
