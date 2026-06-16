using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Domain.Entities;
using OpsFlow.Infrastructure;
using System.Security.Claims;

namespace OpsFlow.Api.Features.Regions.CreateRegion;

internal sealed class CreateRegionHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<CreateRegionCommand, Guid>
{
    public async Task<Guid> Handle(CreateRegionCommand cmd, CancellationToken ct)
    {
        var tenantId = httpContextAccessor.HttpContext!.User.FindFirstValue("tenantId")!;
        await using var db = await factory.CreateAsync(ct);

        var exists = await db.Regions.AnyAsync(r => r.TenantId == tenantId && r.Name == cmd.Name, ct);
        if (exists) throw new InvalidOperationException($"Region '{cmd.Name}' already exists.");

        var region = new Region { TenantId = tenantId, Name = cmd.Name, Description = cmd.Description };
        db.Regions.Add(region);
        await db.SaveChangesAsync(ct);
        return region.Id;
    }
}
