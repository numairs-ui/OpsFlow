using MediatR;
using OpsFlow.Api.Features.StoreSettings.GetStoreSettings;

namespace OpsFlow.Api.Features.TenantSettings.UpdateTenantSettings;

internal sealed record UpdateTenantSettingsCommand(
    string Name,
    string? LogoUrl,
    string? PrimaryContactEmail,
    string? DefaultTimezoneId,
    int? DefaultOverdueGraceMinutes,
    TimeOnly? DefaultDepositDeadlineLocalTime,
    decimal? DefaultTillABase,
    decimal? DefaultTillBBase,
    Dictionary<string, DoughNeedTargetDto>? DefaultDoughNeedTargets,
    string? LocaleCode,
    string? CurrencyCode) : IRequest;
