using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Inventory.GetLatestInventory;

internal sealed class GetLatestInventoryHandler(TenantDbContextFactory factory)
    : IRequestHandler<GetLatestInventoryQuery, List<InventorySnapshotDto>>
{
    public async Task<List<InventorySnapshotDto>> Handle(GetLatestInventoryQuery query, CancellationToken ct)
    {
        await using var db = await factory.CreateAsync(ct);

        // Latest snapshot per item key for this store
        var latest = await db.InventorySnapshots
            .Where(s => s.StoreId == query.StoreId)
            .GroupBy(s => s.ItemKey)
            .Select(g => g.OrderByDescending(s => s.Date).First())
            .ToListAsync(ct);

        return latest.Select(s => new InventorySnapshotDto(s.ItemKey, s.OnHandCount, s.Date, s.SubmittedByUserId, s.UpdatedAt)).ToList();
    }
}
