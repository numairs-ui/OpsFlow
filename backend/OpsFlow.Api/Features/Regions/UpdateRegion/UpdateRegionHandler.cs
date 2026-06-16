using MediatR;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Regions.UpdateRegion;

internal sealed class UpdateRegionHandler(TenantDbContextFactory factory)
    : IRequestHandler<UpdateRegionCommand>
{
    public async Task Handle(UpdateRegionCommand cmd, CancellationToken ct)
    {
        await using var db = await factory.CreateAsync(ct);
        var region = await db.Regions.FindAsync([cmd.Id], ct)
            ?? throw new KeyNotFoundException($"Region {cmd.Id} not found.");
        region.Name = cmd.Name;
        region.Description = cmd.Description;
        await db.SaveChangesAsync(ct);
    }
}
