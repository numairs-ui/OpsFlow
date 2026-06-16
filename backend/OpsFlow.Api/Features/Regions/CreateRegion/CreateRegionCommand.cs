using MediatR;

namespace OpsFlow.Api.Features.Regions.CreateRegion;

internal sealed record CreateRegionCommand(string Name, string? Description) : IRequest<Guid>;
