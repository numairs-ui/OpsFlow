using MediatR;
using OpsFlow.Api.Features.Dashboard.Shared;

namespace OpsFlow.Api.Features.Dashboard.GetSystemDashboard;

internal sealed record GetSystemDashboardQuery : IRequest<SystemDashboardDto>;

internal sealed record SystemDashboardDto(
    double SystemCompletionRate,
    int TotalOpenCount,
    int TotalOverdueCount,
    List<MissedDepositStore> StoresWithMissedDeposits,
    List<RegionalSummaryDto> RegionalSummary);

internal sealed record MissedDepositStore(Guid StoreId, string StoreName);

internal sealed record RegionalSummaryDto(
    Guid RegionId,
    string RegionName,
    int StoreCount,
    double AverageCompletionRate,
    int CriticalAlertCount,
    List<StoreScoreDto> Stores);
