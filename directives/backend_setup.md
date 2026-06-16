# OpsFlow Backend Setup SOP

## Overview

.NET 9 Vertical Slice Architecture backend. All commands assume the repo root as working directory.

---

## Prerequisites

- .NET 9 SDK installed at `$HOME/.dotnet`
- Always prefix dotnet commands with `export PATH="$HOME/.dotnet:$PATH"` or add to shell profile

```bash
# Install .NET 9 SDK (if not present)
curl -fsSL https://dot.net/v1/dotnet-install.sh | bash --channel 9.0 --install-dir $HOME/.dotnet
export PATH="$HOME/.dotnet:$PATH"
dotnet --version  # expect 9.0.x
```

---

## Build & Test

```bash
export PATH="$HOME/.dotnet:$PATH"

# Restore + build all 6 projects
dotnet build backend/OpsFlow.sln

# Run all tests
dotnet test backend/OpsFlow.sln

# Unit tests only
dotnet test backend/OpsFlow.Tests.Unit

# Integration tests only
dotnet test backend/OpsFlow.Tests.Integration
```

---

## Database Migrations (TB-03)

### CLI Tool

```bash
export PATH="$HOME/.dotnet:$PATH"
export MASTER_DB_CONNECTION_STRING="Host=localhost;Database=opsflow_master;Username=postgres;Password=postgres"
export DATABASE_PROVIDER=supabase   # or 'azure' for SQL Server

# Show help
dotnet run --project backend/OpsFlow.Migrations -- --help

# Apply master DB migrations only
dotnet run --project backend/OpsFlow.Migrations -- --migrate-master

# Apply migrations to a specific tenant DB
dotnet run --project backend/OpsFlow.Migrations -- --migrate-tenant bajco-dev

# Apply master + all active tenant migrations
dotnet run --project backend/OpsFlow.Migrations -- --migrate-all

# Seed the Bajco dev tenant (uses EnsureCreated — safe to re-run)
dotnet run --project backend/OpsFlow.Migrations -- --seed-dev
```

### Generating New Migrations (when schema changes)

```bash
# Master DB schema changed
dotnet ef migrations add <MigrationName> \
  --context MasterDbContext \
  --project backend/OpsFlow.Infrastructure \
  --startup-project backend/OpsFlow.Migrations

# Tenant DB schema changed (Identity + tenant data tables)
dotnet ef migrations add <MigrationName> \
  --context TenantDbContext \
  --project backend/OpsFlow.Infrastructure \
  --startup-project backend/OpsFlow.Migrations
```

---

## Environment Variables

| Variable | Description | Default |
|---|---|---|
| `MASTER_DB_CONNECTION_STRING` | Connection string for master (tenant registry) DB | required |
| `TENANT_DB_CONNECTION_STRING` | Override for dev tenant DB (seed-dev only) | `opsflow_bajco_dev` on localhost |
| `DATABASE_PROVIDER` | `supabase` (PostgreSQL) or `azure` (SQL Server) | `supabase` |
| `INFRASTRUCTURE_PROVIDER` | `supabase` or `azure` adapter set | `supabase` |
| `JWT_SECRET` | HMAC-SHA256 signing key (min 32 chars) | required |
| `JWT_ISSUER` | JWT issuer claim | required |
| `JWT_AUDIENCE` | JWT audience claim | required |
| `AZURE_BLOB_CONNECTION_STRING` | Azure Blob Storage connection string | required for azure provider |
| `FCM_PROJECT_ID` | Firebase project ID for push notifications | required when FCM enabled |

---

## Known Issues / Edge Cases

### NuGet Version Constraints (discovered 2026-06-12)
- `Npgsql.EntityFrameworkCore.PostgreSQL 9.0.4` requires `Microsoft.EntityFrameworkCore >= 9.0.1`. Pin EF Core to `9.0.5+` to avoid NU1605 downgrade warnings (treated as errors).
- `supabase-csharp 1.1.3` does not exist on NuGet — latest is `0.16.2`. Do not reference until Supabase stubs are implemented.
- `Scalar.AspNetCore` and `Testcontainers.*` package versions move quickly — check NuGet for latest if NU1603 warnings appear.

### WebApplicationFactory + Minimal APIs (discovered 2026-06-12)
- `ConfigureAppConfiguration` in `WebApplicationFactory.ConfigureWebHost` is applied AFTER service registration when using `WebApplication.CreateBuilder`. Config values read during service registration (e.g., connection strings in `AddDbContexts`) will NOT see these overrides.
- **Fix**: Set environment variables in the static constructor of the factory class. `WebApplication.CreateBuilder` picks up environment variables via `AddEnvironmentVariables()` which runs before service registration.
- See `OpsFlowWebApplicationFactory` for the reference implementation.

### ASP.NET Core Types in Class Libraries (discovered 2026-06-12)
- `OpsFlow.Infrastructure` uses `Microsoft.NET.Sdk` (not `.Web`). Add `<FrameworkReference Include="Microsoft.AspNetCore.App" />` to get `IServiceCollection`, `IConfiguration`, `IHttpContextAccessor` etc. Also requires explicit `using Microsoft.Extensions.DependencyInjection;` etc. since `ImplicitUsings` doesn't include these for class library SDK.

### InternalsVisibleTo for Unit Tests (discovered 2026-06-12)
- VSA handlers are `internal sealed`. Add `<InternalsVisibleTo Include="OpsFlow.Tests.Unit" />` to the Api csproj so unit tests can instantiate them directly.
