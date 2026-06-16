using MediatR;

namespace OpsFlow.Api.Features.Stores.CreateStore;

internal sealed record CreateStoreCommand(string Name, string? Address, Guid RegionId) : IRequest<Guid>;
