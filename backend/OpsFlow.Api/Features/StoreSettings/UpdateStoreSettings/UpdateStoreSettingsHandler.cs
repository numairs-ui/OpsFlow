using MediatR;
using OpsFlow.Domain.Entities;
using OpsFlow.Infrastructure;
using System.Text.Json;

namespace OpsFlow.Api.Features.StoreSettings.UpdateStoreSettings;

internal sealed class UpdateStoreSettingsHandler(TenantDbContextFactory factory)
    : IRequestHandler<UpdateStoreSettingsCommand>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task Handle(UpdateStoreSettingsCommand cmd, CancellationToken ct)
    {
        await using var db = await factory.CreateAsync(ct);

        var store = await db.Stores.FindAsync([cmd.StoreId], ct)
            ?? throw new KeyNotFoundException($"Store {cmd.StoreId} not found.");

        var settings = await db.StoreSettings.FindAsync([cmd.StoreId], ct);

        if (settings == null)
        {
            settings = new OpsFlow.Domain.Entities.StoreSettings { StoreId = cmd.StoreId, TenantId = store.TenantId };
            db.StoreSettings.Add(settings);
        }

        settings.TillABase = cmd.TillABase;
        settings.TillBBase = cmd.TillBBase;
        settings.DoughNeedTargetsJson = JsonSerializer.Serialize(cmd.DoughNeedTargets, JsonOptions);
        settings.TimezoneId = cmd.TimezoneId;
        settings.OverdueGraceMinutes = cmd.OverdueGraceMinutes;

        await db.SaveChangesAsync(ct);
    }
}
