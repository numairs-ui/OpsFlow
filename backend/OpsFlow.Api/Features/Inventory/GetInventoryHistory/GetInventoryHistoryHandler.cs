using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Security;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Inventory.GetInventoryHistory;

internal sealed class GetInventoryHistoryHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<GetInventoryHistoryQuery, List<InventoryHistoryDto>>
{
    public async Task<List<InventoryHistoryDto>> Handle(GetInventoryHistoryQuery query, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var spec = user.ToCaller().Scope();

        await using var db = await factory.CreateAsync(ct);

        var store = await db.Stores.FindAsync([query.StoreId], ct)
            ?? throw new KeyNotFoundException($"Store {query.StoreId} not found.");
        var assigned = spec.IsStoreScoped
            && await db.UserStoreAssignments.AnyAsync(a => a.UserId == user.GetUserId() && a.StoreId == query.StoreId, ct);
        spec.AssertCanViewStore(store.RegionId, store.Id, assigned);

        var since = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-query.Days));

        var snapshots = await db.InventorySnapshots
            .Where(s => s.StoreId == query.StoreId && s.Date >= since)
            .OrderByDescending(s => s.Date)
            .ThenBy(s => s.ItemKey)
            .ToListAsync(ct);

        return snapshots.Select(s => new InventoryHistoryDto(s.Date, s.ItemKey, s.OnHandCount, s.SubmittedByUserId)).ToList();
    }
}
