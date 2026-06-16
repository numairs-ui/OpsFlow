using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Infrastructure;
using System.Security.Claims;

namespace OpsFlow.Api.Features.TenantSettings.GetTenantSettings;

internal sealed class GetTenantSettingsHandler(
    MasterDbContext masterDb,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<GetTenantSettingsQuery, TenantSettingsDto>
{
    public async Task<TenantSettingsDto> Handle(GetTenantSettingsQuery _, CancellationToken ct)
    {
        var tenantId = httpContextAccessor.HttpContext!.User.FindFirstValue("tenantId")
            ?? httpContextAccessor.HttpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault()
            ?? throw new UnauthorizedAccessException("Tenant not identified.");

        var tenant = await masterDb.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct)
            ?? throw new KeyNotFoundException($"Tenant '{tenantId}' not found.");

        return new TenantSettingsDto(tenant.Id, tenant.Name, tenant.LogoUrl, tenant.PrimaryContactEmail, tenant.IsActive);
    }
}
