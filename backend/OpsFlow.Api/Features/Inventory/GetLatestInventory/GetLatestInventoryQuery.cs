using MediatR;

namespace OpsFlow.Api.Features.Inventory.GetLatestInventory;

internal sealed record GetLatestInventoryQuery(Guid StoreId) : IRequest<List<InventorySnapshotDto>>;

internal sealed record InventorySnapshotDto(
    string ItemKey,
    double OnHandCount,
    DateOnly Date,
    string? SubmittedByUserId,
    DateTimeOffset UpdatedAt
);
