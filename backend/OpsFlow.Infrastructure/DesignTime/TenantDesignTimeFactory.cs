using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OpsFlow.Infrastructure.DesignTime;

/// <summary>
/// Allows `dotnet ef migrations add` to create/apply migrations for TenantDbContext (Identity + tenant data).
/// Usage: dotnet ef migrations add <Name> --context TenantDbContext --project backend/OpsFlow.Infrastructure
/// Set TENANT_DB_CONNECTION_STRING env var to target the right database.
/// </summary>
public sealed class TenantDesignTimeFactory : IDesignTimeDbContextFactory<TenantDbContext>
{
    public TenantDbContext CreateDbContext(string[] args)
    {
        var connStr = Environment.GetEnvironmentVariable("TENANT_DB_CONNECTION_STRING")
            ?? "Host=localhost;Database=opsflow_dev_tenant;Username=postgres;Password=postgres";

        var provider = Environment.GetEnvironmentVariable("DATABASE_PROVIDER") ?? "supabase";
        var opts = new DbContextOptionsBuilder<TenantDbContext>();

        if (provider == "azure")
            opts.UseSqlServer(connStr);
        else
            opts.UseNpgsql(connStr);

        return new TenantDbContext(opts.Options);
    }
}
