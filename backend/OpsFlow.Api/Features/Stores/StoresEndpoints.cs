using MediatR;
using OpsFlow.Api.Features.Stores.CreateStore;
using OpsFlow.Api.Features.Stores.DeactivateStore;
using OpsFlow.Api.Features.Stores.GetStoreEmployees;
using OpsFlow.Api.Features.Stores.GetStores;
using OpsFlow.Api.Features.Stores.UpdateStore;

namespace OpsFlow.Api.Features.Stores;

internal static class StoresEndpoints
{
    internal static IEndpointRouteBuilder MapStoresEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/stores").RequireAuthorization().WithTags("Stores");

        group.MapGet("/", async (IMediator m, Guid? regionId, bool activeOnly = true) =>
            Results.Ok(await m.Send(new GetStoresQuery(regionId, activeOnly))));

        group.MapPost("/", async (CreateStoreCommand cmd, IMediator m) =>
        {
            var id = await m.Send(cmd);
            return Results.Created($"/stores/{id}", new { id });
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateStoreBody body, IMediator m) =>
        {
            await m.Send(new UpdateStoreCommand(id, body.Name, body.Address, body.RegionId));
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/deactivate", async (Guid id, IMediator m) =>
        {
            await m.Send(new DeactivateStoreCommand(id));
            return Results.NoContent();
        });

        group.MapGet("/{id:guid}/employees", async (Guid id, IMediator m) =>
            Results.Ok(await m.Send(new GetStoreEmployeesQuery(id))));

        // Convenience: stores by region
        app.MapGet("/regions/{regionId:guid}/stores", async (Guid regionId, IMediator m) =>
            Results.Ok(await m.Send(new GetStoresQuery(regionId, true))))
           .RequireAuthorization().WithTags("Stores");

        return app;
    }
}

internal sealed record UpdateStoreBody(string Name, string? Address, Guid RegionId);
