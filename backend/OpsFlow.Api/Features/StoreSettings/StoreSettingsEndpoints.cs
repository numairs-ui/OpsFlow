using MediatR;
using OpsFlow.Api.Features.StoreSettings.GetStoreSettings;
using OpsFlow.Api.Features.StoreSettings.UpdateStoreSettings;

namespace OpsFlow.Api.Features.StoreSettings;

internal static class StoreSettingsEndpoints
{
    internal static IEndpointRouteBuilder MapStoreSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/stores/{storeId:guid}/settings").RequireAuthorization().WithTags("StoreSettings");

        group.MapGet("/", async (Guid storeId, IMediator m) =>
            Results.Ok(await m.Send(new GetStoreSettingsQuery(storeId))));

        group.MapPut("/", async (Guid storeId, UpdateStoreSettingsBody body, IMediator m) =>
        {
            await m.Send(new UpdateStoreSettingsCommand(
                storeId, body.TillABase, body.TillBBase,
                body.DoughNeedTargets, body.TimezoneId, body.OverdueGraceMinutes));
            return Results.NoContent();
        });

        return app;
    }
}

internal sealed record UpdateStoreSettingsBody(
    decimal? TillABase,
    decimal? TillBBase,
    Dictionary<string, DoughNeedTargetDto> DoughNeedTargets,
    string TimezoneId,
    int OverdueGraceMinutes
);
