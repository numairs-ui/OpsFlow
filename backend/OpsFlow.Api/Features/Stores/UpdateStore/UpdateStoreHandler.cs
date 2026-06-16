using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Stores.UpdateStore;

internal sealed class UpdateStoreHandler(TenantDbContextFactory factory)
    : IRequestHandler<UpdateStoreCommand>
{
    public async Task Handle(UpdateStoreCommand cmd, CancellationToken ct)
    {
        await using var db = await factory.CreateAsync(ct);
        var store = await db.Stores.FindAsync([cmd.Id], ct)
            ?? throw new KeyNotFoundException($"Store {cmd.Id} not found.");

        var regionExists = await db.Regions.AnyAsync(r => r.Id == cmd.RegionId && r.IsActive, ct);
        if (!regionExists) throw new KeyNotFoundException($"Region {cmd.RegionId} not found or inactive.");

        store.Name = cmd.Name;
        store.Address = cmd.Address;
        store.RegionId = cmd.RegionId;
        await db.SaveChangesAsync(ct);
    }
}
