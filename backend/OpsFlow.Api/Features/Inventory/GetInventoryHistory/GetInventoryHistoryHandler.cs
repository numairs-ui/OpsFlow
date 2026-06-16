using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Inventory.GetInventoryHistory;

internal sealed class GetInventoryHistoryHandler(TenantDbContextFactory factory)
    : IRequestHandler<GetInventoryHistoryQuery, List<InventoryHistoryDto>>
{
    public async Task<List<InventoryHistoryDto>> Handle(GetInventoryHistoryQuery query, CancellationToken ct)
    {
        await using var db = await factory.CreateAsync(ct);

        var since = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-query.Days));

        var snapshots = await db.InventorySnapshots
            .Where(s => s.StoreId == query.StoreId && s.Date >= since)
            .OrderByDescending(s => s.Date)
            .ThenBy(s => s.ItemKey)
            .ToListAsync(ct);

        return snapshots.Select(s => new InventoryHistoryDto(s.Date, s.ItemKey, s.OnHandCount, s.SubmittedByUserId)).ToList();
    }
}
