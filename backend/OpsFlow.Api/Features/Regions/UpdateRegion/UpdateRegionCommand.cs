using MediatR;

namespace OpsFlow.Api.Features.Regions.UpdateRegion;

internal sealed record UpdateRegionCommand(Guid Id, string Name, string? Description) : IRequest;
