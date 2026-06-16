using MediatR;

namespace OpsFlow.Api.Features.Stores.UpdateStore;

internal sealed record UpdateStoreCommand(Guid Id, string Name, string? Address, Guid RegionId) : IRequest;
