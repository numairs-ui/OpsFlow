using MediatR;
using OpsFlow.Api.Security;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Stores.ReactivateStore;

internal sealed class ReactivateStoreHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<ReactivateStoreCommand>
{
    public async Task Handle(ReactivateStoreCommand cmd, CancellationToken ct)
    {
        var spec = httpContextAccessor.HttpContext!.User.ToCaller().Scope();

        await using var db = await factory.CreateAsync(ct);
        var store = await db.Stores.FindAsync([cmd.Id], ct)
            ?? throw new KeyNotFoundException($"Store {cmd.Id} not found.");

        spec.AssertCanManageStore(store.RegionId, store.Id);

        store.IsActive = true;
        await db.SaveChangesAsync(ct);
    }
}
