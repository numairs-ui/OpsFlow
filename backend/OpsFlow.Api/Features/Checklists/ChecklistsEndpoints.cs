using MediatR;
using OpsFlow.Api.Features.Checklists.CreateChecklist;
using OpsFlow.Api.Features.Checklists.DeactivateChecklist;
using OpsFlow.Api.Features.Checklists.GetChecklist;
using OpsFlow.Api.Features.Checklists.GetChecklists;
using OpsFlow.Api.Features.Checklists.UpdateItems;

namespace OpsFlow.Api.Features.Checklists;

internal static class ChecklistsEndpoints
{
    internal static IEndpointRouteBuilder MapChecklistsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/checklists").RequireAuthorization().WithTags("Checklists");

        group.MapGet("/", async (IMediator m, string? scope, bool? isActive, string? search) =>
            Results.Ok(await m.Send(new GetChecklistsQuery(scope, isActive, search))));

        group.MapGet("/{id:guid}", async (Guid id, IMediator m) =>
            Results.Ok(await m.Send(new GetChecklistQuery(id))));

        group.MapPost("/", async (CreateChecklistCommand cmd, IMediator m) =>
        {
            var id = await m.Send(cmd);
            return Results.Created($"/checklists/{id}", new { id });
        });

        group.MapPut("/{id:guid}/items", async (Guid id, List<ItemInput> items, IMediator m) =>
        {
            await m.Send(new UpdateItemsCommand(id, items));
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/deactivate", async (Guid id, IMediator m) =>
        {
            await m.Send(new DeactivateChecklistCommand(id));
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/activate", async (Guid id, IMediator m) =>
        {
            await m.Send(new DeactivateChecklistCommand(id, Activate: true));
            return Results.NoContent();
        });

        return app;
    }
}
