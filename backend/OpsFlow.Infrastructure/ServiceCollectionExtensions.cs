using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpsFlow.Domain.Interfaces;
using OpsFlow.Infrastructure.Azure;
using OpsFlow.Infrastructure.Supabase;

namespace OpsFlow.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDbContexts(
        this IServiceCollection services,
        IConfiguration configuration,
        string provider)
    {
        var masterConn = configuration["MASTER_DB_CONNECTION_STRING"]
            ?? throw new InvalidOperationException("MASTER_DB_CONNECTION_STRING not configured.");

        services.AddDbContext<MasterDbContext>(opts =>
        {
            if (provider == "azure")
                opts.UseSqlServer(masterConn);
            else
                opts.UseNpgsql(masterConn);
        });

        // TenantDbContext is resolved per-request via TenantDbContextFactory
        services.AddScoped<TenantDbContextFactory>();

        return services;
    }

    public static IServiceCollection AddAdapterInterfaces(
        this IServiceCollection services,
        IConfiguration configuration,
        string provider)
    {
        if (provider == "azure")
        {
            var blobConn = configuration["AZURE_BLOB_CONNECTION_STRING"]
                ?? throw new InvalidOperationException("AZURE_BLOB_CONNECTION_STRING not configured.");

            services.AddSingleton(_ => new BlobServiceClient(blobConn));
            services.AddScoped<IStorageProvider, AzureBlobStorageProvider>();
            services.AddScoped<IRealtimeService, AzureSignalRService>();

            services.AddIdentity<IdentityUser, IdentityRole>()
                    .AddEntityFrameworkStores<TenantDbContext>()
                    .AddDefaultTokenProviders();
            services.AddScoped<IAuthProvider, AspNetIdentityAuthProvider>();
        }
        else
        {
            // Supabase.Client — singleton initialized once at startup with the service role key
            services.AddSingleton(_ =>
            {
                var url = configuration["SUPABASE_URL"]
                    ?? throw new InvalidOperationException("SUPABASE_URL not configured.");
                var key = configuration["SUPABASE_SERVICE_ROLE_KEY"]
                    ?? throw new InvalidOperationException("SUPABASE_SERVICE_ROLE_KEY not configured.");

                var client = new global::Supabase.Client(url, key, new global::Supabase.SupabaseOptions
                {
                    AutoConnectRealtime = false,
                    AutoRefreshToken = false,
                });
                client.InitializeAsync().GetAwaiter().GetResult();
                return client;
            });

            services.AddHttpClient(); // IHttpClientFactory for SupabaseRealtimeService
            services.AddScoped<IAuthProvider, SupabaseAuthProvider>();
            services.AddScoped<IStorageProvider, SupabaseStorageProvider>();
            services.AddScoped<IRealtimeService, SupabaseRealtimeService>();
        }

        return services;
    }
}
