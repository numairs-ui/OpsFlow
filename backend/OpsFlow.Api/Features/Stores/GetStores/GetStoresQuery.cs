using MediatR;

namespace OpsFlow.Api.Features.Stores.GetStores;

internal sealed record GetStoresQuery(Guid? RegionId = null, bool ActiveOnly = true) : IRequest<List<StoreDto>>;

internal sealed record StoreDto(Guid Id, string Name, string? Address, Guid RegionId, string RegionName, bool IsActive, DateTimeOffset CreatedAt);
