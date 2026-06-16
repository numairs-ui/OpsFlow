using MediatR;

namespace OpsFlow.Api.Features.Regions.DeactivateRegion;

internal sealed record DeactivateRegionCommand(Guid Id) : IRequest;
