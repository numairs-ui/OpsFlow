using MediatR;

namespace OpsFlow.Api.Features.Regions.GetRegions;

internal sealed record GetRegionsQuery(bool ActiveOnly = true) : IRequest<List<RegionDto>>;

internal sealed record RegionDto(Guid Id, string Name, string? Description, bool IsActive, DateTimeOffset CreatedAt);
