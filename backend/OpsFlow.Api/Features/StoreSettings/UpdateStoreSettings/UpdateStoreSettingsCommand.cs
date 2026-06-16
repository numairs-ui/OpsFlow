using MediatR;
using OpsFlow.Api.Features.StoreSettings.GetStoreSettings;

namespace OpsFlow.Api.Features.StoreSettings.UpdateStoreSettings;

internal sealed record UpdateStoreSettingsCommand(
    Guid StoreId,
    decimal? TillABase,
    decimal? TillBBase,
    Dictionary<string, DoughNeedTargetDto> DoughNeedTargets,
    string TimezoneId,
    int OverdueGraceMinutes
) : IRequest;
