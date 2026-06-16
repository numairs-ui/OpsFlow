using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OpsFlow.Domain.Entities;
using System.Security.Claims;

namespace OpsFlow.Infrastructure;

public sealed class TenantDbContextFactory(
    MasterDbContext masterDb,
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration)
{
    public async Task<TenantDbContext> CreateAsync(CancellationToken ct = default)
    {
        var tenantId = httpContextAccessor.HttpContext?.User.FindFirstValue("tenantId")
            // Dev header fallback for local testing without a JWT
            ?? httpContextAccessor.HttpContext?.Request.Headers["X-Tenant-Id"].FirstOrDefault()
            ?? throw new UnauthorizedAccessException("tenantId claim is missing from the request.");

        return await CreateForTenantAsync(tenantId, ct);
    }

    // Used during login — before a JWT exists to extract tenantId from
    public async Task<TenantDbContext> CreateForTenantAsync(string tenantId, CancellationToken ct = default)
    {
        var tenant = await masterDb.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId && t.IsActive, ct)
            ?? throw new UnauthorizedAccessException($"Tenant '{tenantId}' not found or inactive.");

        var provider = configuration["DATABASE_PROVIDER"] ?? "supabase";
        var opts = new DbContextOptionsBuilder<TenantDbContext>();

        if (provider == "azure")
            opts.UseSqlServer(tenant.ConnectionString);
        else
            opts.UseNpgsql(tenant.ConnectionString);

        return new TenantDbContext(opts.Options);
    }
}
