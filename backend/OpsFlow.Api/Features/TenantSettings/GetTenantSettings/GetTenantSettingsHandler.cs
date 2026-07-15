using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Features.StoreSettings.GetStoreSettings;
using OpsFlow.Infrastructure;
using System.Security.Claims;
using System.Text.Json;

namespace OpsFlow.Api.Features.TenantSettings.GetTenantSettings;

internal sealed class GetTenantSettingsHandler(
    MasterDbContext masterDb,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<GetTenantSettingsQuery, TenantSettingsDto>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<TenantSettingsDto> Handle(GetTenantSettingsQuery _, CancellationToken ct)
    {
        var tenantId = httpContextAccessor.HttpContext!.User.FindFirstValue("tenantId")
            ?? httpContextAccessor.HttpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault()
            ?? throw new UnauthorizedAccessException("Tenant not identified.");

        var tenant = await masterDb.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct)
            ?? throw new KeyNotFoundException($"Tenant '{tenantId}' not found.");

        Dictionary<string, DoughNeedTargetDto>? doughDefaults = null;
        if (!string.IsNullOrWhiteSpace(tenant.DefaultDoughNeedTargetsJson))
        {
            try
            {
                doughDefaults = JsonSerializer.Deserialize<Dictionary<string, DoughNeedTargetDto>>(
                    tenant.DefaultDoughNeedTargetsJson, JsonOptions);
            }
            catch (JsonException) { doughDefaults = null; }
        }

        return new TenantSettingsDto(
            tenant.Id, tenant.Name, tenant.LogoUrl, tenant.PrimaryContactEmail, tenant.IsActive,
            tenant.DefaultTimezoneId, tenant.DefaultOverdueGraceMinutes, tenant.DefaultDepositDeadlineLocalTime,
            tenant.DefaultTillABase, tenant.DefaultTillBBase, doughDefaults,
            tenant.LocaleCode, tenant.CurrencyCode);
    }
}
