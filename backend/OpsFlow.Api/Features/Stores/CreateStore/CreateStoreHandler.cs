using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Domain.Entities;
using OpsFlow.Infrastructure;
using System.Security.Claims;

namespace OpsFlow.Api.Features.Stores.CreateStore;

internal sealed class CreateStoreHandler(
    TenantDbContextFactory factory,
    MasterDbContext masterDb,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<CreateStoreCommand, Guid>
{
    private const string FallbackTimezone = "America/New_York";
    private const int FallbackGraceMinutes = 30;
    // Matches GetStoreSettingsHandler.DefaultTargets (24/48 per size) so a seeded store shows the
    // same dough targets the lazy path used to show.
    private const string FallbackDoughJson =
        "{\"dough_10in\":{\"Day2Need\":24,\"Day3Need\":48},\"dough_12in\":{\"Day2Need\":24,\"Day3Need\":48}," +
        "\"dough_14in\":{\"Day2Need\":24,\"Day3Need\":48},\"dough_16in\":{\"Day2Need\":24,\"Day3Need\":48}}";

    public async Task<Guid> Handle(CreateStoreCommand cmd, CancellationToken ct)
    {
        var tenantId = httpContextAccessor.HttpContext!.User.FindFirstValue("tenantId")!;
        await using var db = await factory.CreateAsync(ct);

        var regionExists = await db.Regions.AnyAsync(r => r.Id == cmd.RegionId && r.IsActive, ct);
        if (!regionExists) throw new KeyNotFoundException($"Region {cmd.RegionId} not found or inactive.");

        var store = new Store { TenantId = tenantId, Name = cmd.Name, Address = cmd.Address, RegionId = cmd.RegionId };
        db.Stores.Add(store);
        await db.SaveChangesAsync(ct);

        // Seed the new store's settings from org-wide defaults (falling back to the code literals when
        // a default is unset), so a new store starts configured instead of relying on lazy defaults.
        var tenant = await masterDb.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        db.StoreSettings.Add(new OpsFlow.Domain.Entities.StoreSettings
        {
            StoreId = store.Id,
            TenantId = tenantId,
            TimezoneId = tenant?.DefaultTimezoneId ?? FallbackTimezone,
            OverdueGraceMinutes = tenant?.DefaultOverdueGraceMinutes ?? FallbackGraceMinutes,
            DepositDeadlineLocalTime = tenant?.DefaultDepositDeadlineLocalTime,
            TillABase = tenant?.DefaultTillABase,
            TillBBase = tenant?.DefaultTillBBase,
            DoughNeedTargetsJson = string.IsNullOrWhiteSpace(tenant?.DefaultDoughNeedTargetsJson)
                ? FallbackDoughJson
                : tenant!.DefaultDoughNeedTargetsJson,
        });
        await db.SaveChangesAsync(ct);

        return store.Id;
    }
}
