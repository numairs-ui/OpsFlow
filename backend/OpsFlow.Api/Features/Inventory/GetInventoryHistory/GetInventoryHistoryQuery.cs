using MediatR;

namespace OpsFlow.Api.Features.Inventory.GetInventoryHistory;

internal sealed record GetInventoryHistoryQuery(Guid StoreId, int Days) : IRequest<List<InventoryHistoryDto>>;

internal sealed record InventoryHistoryDto(
    DateOnly Date,
    string ItemKey,
    double OnHandCount,
    string? SubmittedByUserId
);
