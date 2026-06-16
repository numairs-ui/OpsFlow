using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OpsFlow.Domain.Entities;
using OpsFlow.Infrastructure;

var config = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .Build();

var dbProvider = config["DATABASE_PROVIDER"] ?? "supabase";
var command = args.FirstOrDefault() ?? "--help";

// Validate required config only for commands that need a DB connection
var masterConnStr = command == "--help"
    ? string.Empty
    : config["MASTER_DB_CONNECTION_STRING"]
        ?? throw new InvalidOperationException("MASTER_DB_CONNECTION_STRING env var is required.");

switch (command)
{
    case "--migrate-master":
        await MigrateMasterAsync();
        break;

    case "--migrate-tenant":
        var tenantId = args.ElementAtOrDefault(1)
            ?? throw new ArgumentException("Usage: --migrate-tenant <tenantId>");
        await MigrateTenantAsync(tenantId);
        break;

    case "--migrate-all":
        await MigrateAllAsync();
        break;

    case "--seed-dev":
        await SeedDevAsync();
        break;

    default:
        PrintHelp();
        break;
}

return;

// ─────────────────────────────────────────────────────────────────────────────
// Commands
// ─────────────────────────────────────────────────────────────────────────────

async Task MigrateMasterAsync()
{
    Console.WriteLine("[master] Applying migrations...");
    await using var ctx = BuildMasterContext(masterConnStr);
    await ctx.Database.MigrateAsync();
    Console.WriteLine("[master] Done.");
}

async Task MigrateTenantAsync(string id)
{
    Console.WriteLine($"[tenant:{id}] Applying migrations...");
    await using var master = BuildMasterContext(masterConnStr);
    var tenant = await master.Tenants.FirstOrDefaultAsync(t => t.Id == id)
        ?? throw new InvalidOperationException($"Tenant '{id}' not found in master DB.");

    await using var ctx = BuildTenantContext(tenant.ConnectionString);
    await ctx.Database.MigrateAsync();
    Console.WriteLine($"[tenant:{id}] Done.");
}

async Task MigrateAllAsync()
{
    await MigrateMasterAsync();

    await using var master = BuildMasterContext(masterConnStr);
    var tenants = await master.Tenants.Where(t => t.IsActive).ToListAsync();

    Console.WriteLine($"[all] Migrating {tenants.Count} active tenant(s)...");
    foreach (var tenant in tenants)
        await MigrateTenantAsync(tenant.Id);

    Console.WriteLine("[all] Complete.");
}

async Task SeedDevAsync()
{
    Console.WriteLine("[seed-dev] Seeding development tenant...");

    await using var master = BuildMasterContext(masterConnStr);
    await master.Database.MigrateAsync();

    const string devTenantId = "bajco-dev";
    var tenantConnStr = config["TENANT_DB_CONNECTION_STRING"]
        ?? "Host=localhost;Database=opsflow_bajco_dev;Username=postgres;Password=postgres";

    var existing = await master.Tenants.FirstOrDefaultAsync(t => t.Id == devTenantId);
    if (existing is null)
    {
        master.Tenants.Add(new Tenant
        {
            Id = devTenantId,
            Name = "Bajco Group (Dev)",
            ConnectionString = tenantConnStr,
            IsActive = true,
            PrimaryContactEmail = "eusha@bajco.net",
        });
        await master.SaveChangesAsync();
        Console.WriteLine($"[seed-dev] Created tenant '{devTenantId}'.");
    }
    else
    {
        Console.WriteLine($"[seed-dev] Tenant '{devTenantId}' already exists, skipping.");
    }

    Console.WriteLine("[seed-dev] Applying tenant schema...");
    await using var tenantCtx = BuildTenantContext(tenantConnStr);
    await tenantCtx.Database.MigrateAsync();
    Console.WriteLine("[seed-dev] Done. Dev environment is ready.");
}

// ─────────────────────────────────────────────────────────────────────────────
// Helpers
// ─────────────────────────────────────────────────────────────────────────────

MasterDbContext BuildMasterContext(string connStr)
{
    var opts = new DbContextOptionsBuilder<MasterDbContext>();
    Apply(opts, connStr);
    return new MasterDbContext(opts.Options);
}

TenantDbContext BuildTenantContext(string connStr)
{
    var opts = new DbContextOptionsBuilder<TenantDbContext>();
    Apply(opts, connStr);
    return new TenantDbContext(opts.Options);
}

void Apply<T>(DbContextOptionsBuilder<T> opts, string connStr) where T : DbContext
{
    if (dbProvider == "azure")
        opts.UseSqlServer(connStr, o => o.MigrationsAssembly("OpsFlow.Infrastructure"));
    else
        opts.UseNpgsql(connStr, o => o.MigrationsAssembly("OpsFlow.Infrastructure"));
}

void PrintHelp()
{
    Console.WriteLine("""
        OpsFlow Migration CLI

        Commands:
          --migrate-master              Apply EF Core migrations to the master (tenant registry) database
          --migrate-tenant <tenantId>   Apply migrations to a specific tenant database
          --migrate-all                 Apply master migrations then all active tenant databases
          --seed-dev                    Create the 'bajco-dev' tenant and apply schema (no real DB required for initial setup)

        Environment variables:
          MASTER_DB_CONNECTION_STRING   Required. Connection string for the master database.
          DATABASE_PROVIDER             'supabase' (PostgreSQL, default) or 'azure' (SQL Server)
          TENANT_DB_CONNECTION_STRING   Used by --seed-dev to override the dev tenant connection string

        Examples:
          export MASTER_DB_CONNECTION_STRING="Host=localhost;Database=opsflow_master;Username=postgres;Password=postgres"
          dotnet run --project backend/OpsFlow.Migrations -- --seed-dev
          dotnet run --project backend/OpsFlow.Migrations -- --migrate-all
        """);
}
