using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OpsFlow.Infrastructure.DesignTime;

/// <summary>
/// Allows `dotnet ef migrations add` to create/apply migrations for MasterDbContext.
/// Usage: dotnet ef migrations add <Name> --context MasterDbContext --project backend/OpsFlow.Infrastructure
/// </summary>
public sealed class MasterDesignTimeFactory : IDesignTimeDbContextFactory<MasterDbContext>
{
    public MasterDbContext CreateDbContext(string[] args)
    {
        var connStr = Environment.GetEnvironmentVariable("MASTER_DB_CONNECTION_STRING")
            ?? "Host=localhost;Database=opsflow_master;Username=postgres;Password=postgres";

        var provider = Environment.GetEnvironmentVariable("DATABASE_PROVIDER") ?? "supabase";
        var opts = new DbContextOptionsBuilder<MasterDbContext>();

        if (provider == "azure")
            opts.UseSqlServer(connStr);
        else
            opts.UseNpgsql(connStr);

        return new MasterDbContext(opts.Options);
    }
}
