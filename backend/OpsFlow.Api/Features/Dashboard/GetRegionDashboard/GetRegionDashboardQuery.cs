using MediatR;

namespace OpsFlow.Api.Features.Dashboard.GetRegionDashboard;

internal sealed record GetRegionDashboardQuery(Guid RegionId) : IRequest<RegionDashboardDto>;

internal sealed record RegionDashboardDto(List<StoreScoreDto> Stores);

internal sealed record StoreScoreDto(
    Guid StoreId,
    string Name,
    double CompletionRate,
    int OpenCount,
    int OverdueCount,
    int CorrectiveActionCount,
    bool DepositLoggedToday,
    double CompositeScore);
