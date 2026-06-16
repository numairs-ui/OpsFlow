using MediatR;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Regions.DeactivateRegion;

internal sealed class DeactivateRegionHandler(TenantDbContextFactory factory)
    : IRequestHandler<DeactivateRegionCommand>
{
    public async Task Handle(DeactivateRegionCommand cmd, CancellationToken ct)
    {
        await using var db = await factory.CreateAsync(ct);
        var region = await db.Regions.FindAsync([cmd.Id], ct)
            ?? throw new KeyNotFoundException($"Region {cmd.Id} not found.");
        region.IsActive = false;
        await db.SaveChangesAsync(ct);
    }
}
