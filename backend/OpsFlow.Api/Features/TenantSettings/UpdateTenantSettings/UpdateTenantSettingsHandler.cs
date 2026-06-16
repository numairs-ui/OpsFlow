using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Infrastructure;
using System.Security.Claims;

namespace OpsFlow.Api.Features.TenantSettings.UpdateTenantSettings;

internal sealed class UpdateTenantSettingsHandler(
    MasterDbContext masterDb,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<UpdateTenantSettingsCommand>
{
    public async Task Handle(UpdateTenantSettingsCommand cmd, CancellationToken ct)
    {
        var tenantId = httpContextAccessor.HttpContext!.User.FindFirstValue("tenantId")
            ?? httpContextAccessor.HttpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault()
            ?? throw new UnauthorizedAccessException("Tenant not identified.");

        var role = httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.Role)
            ?? httpContextAccessor.HttpContext.User.FindFirstValue("role");
        if (role != "admin") throw new UnauthorizedAccessException("Admin role required.");

        var tenant = await masterDb.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct)
            ?? throw new KeyNotFoundException($"Tenant '{tenantId}' not found.");

        if (!string.IsNullOrWhiteSpace(cmd.Name))
            tenant.Name = cmd.Name.Trim();

        tenant.LogoUrl = cmd.LogoUrl;
        tenant.PrimaryContactEmail = cmd.PrimaryContactEmail?.Trim();

        await masterDb.SaveChangesAsync(ct);
    }
}
