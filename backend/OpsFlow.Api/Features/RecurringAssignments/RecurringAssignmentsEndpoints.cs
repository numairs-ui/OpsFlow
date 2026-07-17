using MediatR;
using OpsFlow.Api.Features.RecurringAssignments.CreateRecurringAssignment;
using OpsFlow.Api.Features.RecurringAssignments.DeleteRecurringAssignment;
using OpsFlow.Api.Features.RecurringAssignments.GetRecurringAssignments;
using OpsFlow.Api.Features.RecurringAssignments.GetRecurringHealth;
using OpsFlow.Api.Features.RecurringAssignments.PauseRecurringAssignment;

namespace OpsFlow.Api.Features.RecurringAssignments;

internal static class RecurringAssignmentsEndpoints
{
    internal static IEndpointRouteBuilder MapRecurringAssignmentsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/recurring-assignments").RequireAuthorization().WithTags("RecurringAssignments");

        group.MapGet("/", async (IMediator m, Guid? storeId, bool? isPaused) =>
            Results.Ok(await m.Send(new GetRecurringAssignmentsQuery(storeId, isPaused))));

        group.MapGet("/health", async (IMediator m) =>
            Results.Ok(await m.Send(new GetRecurringHealthQuery())));

        group.MapPost("/", async (CreateRecurringAssignmentCommand cmd, IMediator m) =>
        {
            var id = await m.Send(cmd);
            return Results.Created($"/recurring-assignments/{id}", new { id });
        });

        group.MapPost("/{id:guid}/pause", async (Guid id, IMediator m) =>
        {
            await m.Send(new PauseRecurringAssignmentCommand(id));
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/resume", async (Guid id, IMediator m) =>
        {
            await m.Send(new PauseRecurringAssignmentCommand(id, Resume: true));
            return Results.NoContent();
        });

        group.MapDelete("/{id:guid}", async (Guid id, IMediator m) =>
        {
            await m.Send(new DeleteRecurringAssignmentCommand(id));
            return Results.NoContent();
        });

        return app;
    }
}
