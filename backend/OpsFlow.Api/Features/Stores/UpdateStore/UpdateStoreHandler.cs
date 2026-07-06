using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Security;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Stores.UpdateStore;

internal sealed class UpdateStoreHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<UpdateStoreCommand>
{
    public async Task Handle(UpdateStoreCommand cmd, CancellationToken ct)
    {
        var spec = httpContextAccessor.HttpContext!.User.ToCaller().Scope();

        await using var db = await factory.CreateAsync(ct);
        var store = await db.Stores.FindAsync([cmd.Id], ct)
            ?? throw new KeyNotFoundException($"Store {cmd.Id} not found.");

        // Moving a store between regions requires managing both ends: the store's current
        // region (so a region-scoped admin can't reach into a store outside its set) and the
        // destination region (so it can't move a store into a region it doesn't manage either).
        spec.AssertCanManageStore(store.RegionId, store.Id);
        if (cmd.RegionId != store.RegionId && !spec.CanActOnRegion(cmd.RegionId))
            throw new UnauthorizedAccessException("You do not have access to the destination region.");

        var regionExists = await db.Regions.AnyAsync(r => r.Id == cmd.RegionId && r.IsActive, ct);
        if (!regionExists) throw new KeyNotFoundException($"Region {cmd.RegionId} not found or inactive.");

        store.Name = cmd.Name;
        store.Address = cmd.Address;
        store.RegionId = cmd.RegionId;
        await db.SaveChangesAsync(ct);
    }
}
