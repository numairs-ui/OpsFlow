using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Domain.Entities;
using OpsFlow.Infrastructure;
using System.Security.Claims;

namespace OpsFlow.Api.Features.Stores.CreateStore;

internal sealed class CreateStoreHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<CreateStoreCommand, Guid>
{
    public async Task<Guid> Handle(CreateStoreCommand cmd, CancellationToken ct)
    {
        var tenantId = httpContextAccessor.HttpContext!.User.FindFirstValue("tenantId")!;
        await using var db = await factory.CreateAsync(ct);

        var regionExists = await db.Regions.AnyAsync(r => r.Id == cmd.RegionId && r.IsActive, ct);
        if (!regionExists) throw new KeyNotFoundException($"Region {cmd.RegionId} not found or inactive.");

        var store = new Store { TenantId = tenantId, Name = cmd.Name, Address = cmd.Address, RegionId = cmd.RegionId };
        db.Stores.Add(store);
        await db.SaveChangesAsync(ct);
        return store.Id;
    }
}
