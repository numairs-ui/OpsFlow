using MediatR;
using OpsFlow.Api.Features.Dashboard.Shared;

namespace OpsFlow.Api.Features.Dashboard.GetRegionDashboard;

internal sealed record GetRegionDashboardQuery(Guid RegionId) : IRequest<RegionDashboardDto>;

internal sealed record RegionDashboardDto(List<StoreScoreDto> Stores);
