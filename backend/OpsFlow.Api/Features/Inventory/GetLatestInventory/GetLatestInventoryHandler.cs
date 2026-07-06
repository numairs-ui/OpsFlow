using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Security;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Inventory.GetLatestInventory;

internal sealed class GetLatestInventoryHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<GetLatestInventoryQuery, List<InventorySnapshotDto>>
{
    public async Task<List<InventorySnapshotDto>> Handle(GetLatestInventoryQuery query, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var spec = user.ToCaller().Scope();

        await using var db = await factory.CreateAsync(ct);

        var store = await db.Stores.FindAsync([query.StoreId], ct)
            ?? throw new KeyNotFoundException($"Store {query.StoreId} not found.");
        var assigned = spec.IsStoreScoped
            && await db.UserStoreAssignments.AnyAsync(a => a.UserId == user.GetUserId() && a.StoreId == query.StoreId, ct);
        spec.AssertCanViewStore(store.RegionId, store.Id, assigned);

        // Latest snapshot per item key for this store
        var latest = await db.InventorySnapshots
            .Where(s => s.StoreId == query.StoreId)
            .GroupBy(s => s.ItemKey)
            .Select(g => g.OrderByDescending(s => s.Date).First())
            .ToListAsync(ct);

        return latest.Select(s => new InventorySnapshotDto(s.ItemKey, s.OnHandCount, s.Date, s.SubmittedByUserId, s.UpdatedAt)).ToList();
    }
}
