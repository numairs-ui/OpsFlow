using MediatR;
using OpsFlow.Api.Features.Regions.CreateRegion;
using OpsFlow.Api.Features.Regions.DeactivateRegion;
using OpsFlow.Api.Features.Regions.GetRegions;
using OpsFlow.Api.Features.Regions.UpdateRegion;

namespace OpsFlow.Api.Features.Regions;

internal static class RegionsEndpoints
{
    internal static IEndpointRouteBuilder MapRegionsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/regions").RequireAuthorization().WithTags("Regions");

        group.MapGet("/", async (IMediator m, bool activeOnly = true) =>
            Results.Ok(await m.Send(new GetRegionsQuery(activeOnly))));

        group.MapPost("/", async (CreateRegionCommand cmd, IMediator m) =>
        {
            var id = await m.Send(cmd);
            return Results.Created($"/regions/{id}", new { id });
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateRegionBody body, IMediator m) =>
        {
            await m.Send(new UpdateRegionCommand(id, body.Name, body.Description));
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/deactivate", async (Guid id, IMediator m) =>
        {
            await m.Send(new DeactivateRegionCommand(id));
            return Results.NoContent();
        });

        return app;
    }
}

internal sealed record UpdateRegionBody(string Name, string? Description);
