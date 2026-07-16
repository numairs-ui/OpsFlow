using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Interfaces;
using OpsFlow.Infrastructure;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace OpsFlow.Tests.Integration;

/// <summary>
/// Extended factory for tests that need to read/write tenant-scoped data.
/// Sets INFRASTRUCTURE_PROVIDER=inmemory so AddDbContexts uses an in-memory MasterDbContext
/// and TenantDbContextFactory creates in-memory TenantDbContexts.
/// </summary>
public sealed class TenantAwareWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TenantId = "test-tenant";
    public const string SuperAdminUserId = "super-admin-user-id";
    public const string AdminUserId = "admin-user-id";
    public const string EmployeeUserId = "employee-user-id";
    public const string SupervisorUserId = "supervisor-user-id";
    public const string StoreManagerUserId = "store-manager-user-id";
    public const string KioskUserId = "kiosk-user-id";

    // Fixed GUIDs for shared reference entities seeded by SeedCommonDataAsync
    public static readonly Guid RegionId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid StoreId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    public static readonly Guid AltStoreId = Guid.Parse("10000000-0000-0000-0000-000000000003");

    private const string JwtSecret = "test-integration-secret-long-enough-for-hmac256";
    private const string JwtIssuer = "opsflow-test";
    private const string JwtAudience = "opsflow-test";

    static TenantAwareWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("INFRASTRUCTURE_PROVIDER", "inmemory");
        Environment.SetEnvironmentVariable("DATABASE_PROVIDER", "inmemory");
        Environment.SetEnvironmentVariable("MASTER_DB_CONNECTION_STRING", "n/a");
        Environment.SetEnvironmentVariable("AZURE_BLOB_CONNECTION_STRING", "UseDevelopmentStorage=true");
        Environment.SetEnvironmentVariable("AZURE_BLOB_CONTAINER_NAME", "test-photos");
        Environment.SetEnvironmentVariable("JWT_SECRET", JwtSecret);
        Environment.SetEnvironmentVariable("JWT_ISSUER", JwtIssuer);
        Environment.SetEnvironmentVariable("JWT_AUDIENCE", JwtAudience);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IStorageProvider, NullStorageProvider>();
            services.AddSingleton<IRealtimeService, NullRealtimeService>();
            services.AddSingleton<IAuthProvider, NullAuthProvider>();
        });
    }

    public async Task SeedMasterDbAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MasterDbContext>();
        await db.Database.EnsureCreatedAsync();

        if (!db.Tenants.Any(t => t.Id == TenantId))
        {
            try
            {
                db.Tenants.Add(new Tenant
                {
                    Id = TenantId,
                    Name = "Test Tenant",
                    ConnectionString = "n/a-inmemory",
                    IsActive = true,
                });
                await db.SaveChangesAsync();
            }
            catch (ArgumentException)
            {
                // Another thread already inserted the tenant between our Any() check and Add(); safe to ignore.
            }
        }
    }

    public async Task<TenantDbContext> GetTenantDbAsync()
    {
        await SeedMasterDbAsync();
        using var scope = Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<TenantDbContextFactory>();
        return await factory.CreateForTenantAsync(TenantId);
    }

    /// <summary>
    /// Seeds the Region, both Stores, and UserProfiles for all four roles so role-aware handlers
    /// (GetTodayTasks, CreateRecurringAssignment, etc.) can resolve the requesting user's store/region.
    /// Idempotent — safe to call from multiple test classes.
    /// </summary>
    public async Task SeedCommonDataAsync()
    {
        var db = await GetTenantDbAsync();

        // Region
        if (!db.Regions.Any(r => r.Id == RegionId))
        {
            try
            {
                db.Regions.Add(new Region { Id = RegionId, TenantId = TenantId, Name = "Test Region" });
                await db.SaveChangesAsync();
            }
            catch (Exception ex) when (ex is ArgumentException || ex.GetType().Name.Contains("DbUpdate")) { }
        }

        // Primary Store
        if (!db.Stores.Any(s => s.Id == StoreId))
        {
            try
            {
                db.Stores.Add(new Store { Id = StoreId, TenantId = TenantId, RegionId = RegionId, Name = "Test Store" });
                await db.SaveChangesAsync();
            }
            catch (Exception ex) when (ex is ArgumentException || ex.GetType().Name.Contains("DbUpdate")) { }
        }

        // Alternate Store (for cross-store access-control tests)
        if (!db.Stores.Any(s => s.Id == AltStoreId))
        {
            try
            {
                db.Stores.Add(new Store { Id = AltStoreId, TenantId = TenantId, RegionId = RegionId, Name = "Alt Store" });
                await db.SaveChangesAsync();
            }
            catch (Exception ex) when (ex is ArgumentException || ex.GetType().Name.Contains("DbUpdate")) { }
        }

        // UserProfiles needed for role-scoped handler checks
        var profiles = new[]
        {
            new UserProfile { UserId = SuperAdminUserId,  Email = "superadmin@test.com",DisplayName = "Test Super Admin", Role = "super_admin" },
            new UserProfile { UserId = AdminUserId,       Email = "admin@test.com",    DisplayName = "Test Admin",    Role = "admin", RegionId = RegionId, RegionIdsCsv = RegionId.ToString() },
            new UserProfile { UserId = EmployeeUserId,    Email = "employee@test.com",  DisplayName = "Test Employee", Role = "store_employee", StoreId = StoreId },
            new UserProfile { UserId = SupervisorUserId,  Email = "supervisor@test.com",DisplayName = "Test Supervisor", Role = "supervisor", RegionId = RegionId },
            new UserProfile { UserId = StoreManagerUserId,Email = "manager@test.com",   DisplayName = "Test Manager",  Role = "store_manager", StoreId = StoreId },
            new UserProfile { UserId = KioskUserId,       Email = "kiosk@test.com",     DisplayName = "Test Kiosk",    Role = "store_kiosk", StoreId = StoreId },
        };

        foreach (var profile in profiles)
        {
            if (!db.UserProfiles.Any(u => u.UserId == profile.UserId))
            {
                try
                {
                    db.UserProfiles.Add(profile);
                    await db.SaveChangesAsync();
                }
                catch (Exception ex) when (ex is ArgumentException || ex.GetType().Name.Contains("DbUpdate")) { }
            }
        }
    }

    public string MintToken(string userId, string role, string? storeId = null, string? regionId = null)
        => MintMultiRegionToken(userId, role, storeId, regionId is null ? [] : [regionId]);

    /// <summary>Mints a token with a region SET — one "regionId" claim per region (admin: many, supervisor: one).</summary>
    public string MintMultiRegionToken(string userId, string role, string? storeId, params string[] regionIds)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new("tenantId", TenantId),
            new("role", role),
        };
        if (storeId is not null) claims.Add(new("storeId", storeId));
        foreach (var regionId in regionIds) claims.Add(new("regionId", regionId));

        var token = new JwtSecurityToken(
            JwtIssuer, JwtAudience, claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
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
        // Unique id per call — tenant in-memory DBs are shared across the whole test run,
        // so a constant id would collide on the UserProfiles primary key across tests.
        public Task<string> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default)
            => Task.FromResult(Guid.NewGuid().ToString());
        public Task UpdateUserAsync(UpdateUserRequest request, CancellationToken ct = default) => Task.CompletedTask;
        public Task ResetPasswordAsync(string userId, string newPassword, CancellationToken ct = default) => Task.CompletedTask;
        public Task<string?> GetEmailAsync(string userId, CancellationToken ct = default) => Task.FromResult<string?>(null);
    }
}

