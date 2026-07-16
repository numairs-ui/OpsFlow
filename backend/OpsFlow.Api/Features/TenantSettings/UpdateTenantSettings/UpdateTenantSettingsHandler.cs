using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Domain.Authorization;
using OpsFlow.Infrastructure;
using System.Security.Claims;
using System.Text.Json;

namespace OpsFlow.Api.Features.TenantSettings.UpdateTenantSettings;

internal sealed class UpdateTenantSettingsHandler(
    MasterDbContext masterDb,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<UpdateTenantSettingsCommand>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task Handle(UpdateTenantSettingsCommand cmd, CancellationToken ct)
    {
        var tenantId = httpContextAccessor.HttpContext!.User.FindFirstValue("tenantId")
            ?? httpContextAccessor.HttpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault()
            ?? throw new UnauthorizedAccessException("Tenant not identified.");

        var role = httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.Role)
            ?? httpContextAccessor.HttpContext.User.FindFirstValue("role");
        if (!Roles.IsSuperAdmin(role)) throw new UnauthorizedAccessException("Super admin role required.");

        var tenant = await masterDb.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct)
            ?? throw new KeyNotFoundException($"Tenant '{tenantId}' not found.");

        if (!string.IsNullOrWhiteSpace(cmd.Name))
            tenant.Name = cmd.Name.Trim();

        tenant.LogoUrl = cmd.LogoUrl;
        tenant.PrimaryContactEmail = cmd.PrimaryContactEmail?.Trim();

        // Org-wide new-store defaults
        tenant.DefaultTimezoneId = string.IsNullOrWhiteSpace(cmd.DefaultTimezoneId) ? null : cmd.DefaultTimezoneId.Trim();
        tenant.DefaultOverdueGraceMinutes = cmd.DefaultOverdueGraceMinutes;
        tenant.DefaultDepositDeadlineLocalTime = cmd.DefaultDepositDeadlineLocalTime;
        tenant.DefaultTillABase = cmd.DefaultTillABase;
        tenant.DefaultTillBBase = cmd.DefaultTillBBase;
        tenant.DefaultDoughNeedTargetsJson = cmd.DefaultDoughNeedTargets is { Count: > 0 }
            ? JsonSerializer.Serialize(cmd.DefaultDoughNeedTargets, JsonOptions)
            : null;

        // Display conventions
        tenant.LocaleCode = string.IsNullOrWhiteSpace(cmd.LocaleCode) ? null : cmd.LocaleCode.Trim();
        tenant.CurrencyCode = string.IsNullOrWhiteSpace(cmd.CurrencyCode) ? null : cmd.CurrencyCode.Trim();

        await masterDb.SaveChangesAsync(ct);
    }
}
