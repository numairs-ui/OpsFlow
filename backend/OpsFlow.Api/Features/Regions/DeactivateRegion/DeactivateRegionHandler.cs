using MediatR;
using OpsFlow.Api.Security;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Regions.DeactivateRegion;

internal sealed class DeactivateRegionHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<DeactivateRegionCommand>
{
    public async Task Handle(DeactivateRegionCommand cmd, CancellationToken ct)
    {
        httpContextAccessor.HttpContext!.User.ToCaller().Scope().AssertGlobal();

        await using var db = await factory.CreateAsync(ct);
        var region = await db.Regions.FindAsync([cmd.Id], ct)
            ?? throw new KeyNotFoundException($"Region {cmd.Id} not found.");
        region.IsActive = false;
        await db.SaveChangesAsync(ct);
    }
}
