using MediatR;
using OpsFlow.Api.Features.StoreSettings.GetStoreSettings;

namespace OpsFlow.Api.Features.TenantSettings.GetTenantSettings;

internal sealed record GetTenantSettingsQuery : IRequest<TenantSettingsDto>;

internal sealed record TenantSettingsDto(
    string Id,
    string Name,
    string? LogoUrl,
    string? PrimaryContactEmail,
    bool IsActive,
    // Org-wide defaults new stores inherit (null → server falls back to the code literal).
    string? DefaultTimezoneId,
    int? DefaultOverdueGraceMinutes,
    TimeOnly? DefaultDepositDeadlineLocalTime,
    decimal? DefaultTillABase,
    decimal? DefaultTillBBase,
    Dictionary<string, DoughNeedTargetDto>? DefaultDoughNeedTargets,
    // Org display conventions honored app-wide (null → app default en-US / USD).
    string? LocaleCode,
    string? CurrencyCode);
