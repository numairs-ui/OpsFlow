using MediatR;

namespace OpsFlow.Api.Features.StoreSettings.GetStoreSettings;

internal sealed record GetStoreSettingsQuery(Guid StoreId) : IRequest<StoreSettingsDto>;

internal sealed record DoughNeedTargetDto(double Day2Need, double Day3Need);

internal sealed record StoreSettingsDto(
    Guid StoreId,
    decimal? TillABase,
    decimal? TillBBase,
    Dictionary<string, DoughNeedTargetDto> DoughNeedTargets,
    string TimezoneId,
    int OverdueGraceMinutes,
    TimeOnly? DepositDeadlineLocalTime
);
