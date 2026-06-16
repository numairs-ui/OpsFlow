using MediatR;
using OpsFlow.Api.Features.Inventory.GetInventoryHistory;
using OpsFlow.Api.Features.Inventory.GetLatestInventory;

namespace OpsFlow.Api.Features.Inventory;

internal static class InventoryEndpoints
{
    internal static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/stores/{storeId:guid}/inventory").RequireAuthorization().WithTags("Inventory");

        group.MapGet("/latest", async (Guid storeId, IMediator m) =>
            Results.Ok(await m.Send(new GetLatestInventoryQuery(storeId))));

        group.MapGet("/history", async (Guid storeId, IMediator m, int days = 7) =>
            Results.Ok(await m.Send(new GetInventoryHistoryQuery(storeId, Math.Clamp(days, 1, 90)))));

        return app;
    }
}
