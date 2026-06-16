using MediatR;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Stores.DeactivateStore;

internal sealed class DeactivateStoreHandler(TenantDbContextFactory factory)
    : IRequestHandler<DeactivateStoreCommand>
{
    public async Task Handle(DeactivateStoreCommand cmd, CancellationToken ct)
    {
        await using var db = await factory.CreateAsync(ct);
        var store = await db.Stores.FindAsync([cmd.Id], ct)
            ?? throw new KeyNotFoundException($"Store {cmd.Id} not found.");
        store.IsActive = false;
        await db.SaveChangesAsync(ct);
    }
}
