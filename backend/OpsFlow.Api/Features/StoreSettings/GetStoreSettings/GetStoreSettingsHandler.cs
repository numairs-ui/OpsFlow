using MediatR;
using OpsFlow.Infrastructure;
using System.Text.Json;

namespace OpsFlow.Api.Features.StoreSettings.GetStoreSettings;

internal sealed class GetStoreSettingsHandler(TenantDbContextFactory factory)
    : IRequestHandler<GetStoreSettingsQuery, StoreSettingsDto>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly Dictionary<string, DoughNeedTargetDto> DefaultTargets = new()
    {
        ["dough_10in"] = new(24, 48),
        ["dough_12in"] = new(24, 48),
        ["dough_14in"] = new(24, 48),
        ["dough_16in"] = new(24, 48)
    };

    public async Task<StoreSettingsDto> Handle(GetStoreSettingsQuery query, CancellationToken ct)
    {
        await using var db = await factory.CreateAsync(ct);

        var settings = await db.StoreSettings.FindAsync([query.StoreId], ct);
        if (settings == null)
            return new StoreSettingsDto(query.StoreId, null, null, DefaultTargets, "America/New_York", 30);

        Dictionary<string, DoughNeedTargetDto> targets;
        try
        {
            targets = JsonSerializer.Deserialize<Dictionary<string, DoughNeedTargetDto>>(
                settings.DoughNeedTargetsJson, JsonOptions) ?? DefaultTargets;
        }
        catch (JsonException)
        {
            targets = DefaultTargets;
        }

        return new StoreSettingsDto(settings.StoreId, settings.TillABase, settings.TillBBase, targets, settings.TimezoneId, settings.OverdueGraceMinutes);
    }
}
