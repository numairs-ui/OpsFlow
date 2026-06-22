using MediatR;
using OpsFlow.Api.Features.Tasks.AssignTask;
using OpsFlow.Api.Features.Tasks.CancelTask;
using OpsFlow.Api.Features.Tasks.ClaimTask;
using OpsFlow.Api.Features.Tasks.CompleteTask;
using OpsFlow.Api.Features.Tasks.CreateTask;
using OpsFlow.Api.Features.Tasks.DeferTask;
using OpsFlow.Api.Features.Tasks.GetTask;
using OpsFlow.Api.Features.Tasks.GetTasks;
using OpsFlow.Api.Features.Tasks.GetTodayTasks;
using OpsFlow.Api.Features.Tasks.StartTask;
using OpsFlow.Api.Features.Tasks.VerifyTask;

namespace OpsFlow.Api.Features.Tasks;

internal static class TasksEndpoints
{
    internal static IEndpointRouteBuilder MapTasksEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/tasks").RequireAuthorization().WithTags("Tasks");

        group.MapGet("/", async (IMediator m, Guid? storeId, string? status, DateTimeOffset? from, DateTimeOffset? to) =>
            Results.Ok(await m.Send(new GetTasksQuery(storeId, status, from, to))));

        group.MapGet("/{id:guid}", async (Guid id, IMediator m) =>
            Results.Ok(await m.Send(new GetTaskQuery(id))));

        group.MapPost("/", async (CreateTaskCommand cmd, IMediator m) =>
        {
            var id = await m.Send(cmd);
            return Results.Created($"/tasks/{id}", new { id });
        });

        group.MapPost("/{id:guid}/claim", async (Guid id, ClaimTaskRequest? body, IMediator m) =>
        {
            await m.Send(new ClaimTaskCommand(id, body?.VolunteerName));
            return Results.NoContent();
        });

        group.MapPatch("/{id:guid}/assign", async (Guid id, AssignTaskRequest body, IMediator m) =>
        {
            await m.Send(new AssignTaskCommand(id, body.AssignedToUserId));
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/start", async (Guid id, IMediator m) =>
        {
            await m.Send(new StartTaskCommand(id));
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/complete", async (Guid id, CompleteTaskRequest body, IMediator m) =>
        {
            var response = await m.Send(new CompleteTaskCommand(id, body.CompletedByVolunteerName, body.FieldValues));
            return Results.Ok(response);
        });

        group.MapPost("/{id:guid}/verify", async (Guid id, IMediator m) =>
        {
            await m.Send(new VerifyTaskCommand(id));
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/cancel", async (Guid id, CancelTaskRequest body, IMediator m) =>
        {
            await m.Send(new CancelTaskCommand(id, body.Reason));
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/defer", async (Guid id, DeferTaskRequest body, IMediator m) =>
        {
            await m.Send(new DeferTaskCommand(id, body.Reason, body.DeferredTo));
            return Results.NoContent();
        });

        // Today's tasks for a store — used by the Task Board
        app.MapGet("/stores/{storeId:guid}/tasks/today",
            async (Guid storeId, IMediator m) =>
                Results.Ok(await m.Send(new GetTodayTasksQuery(storeId))))
            .RequireAuthorization()
            .WithTags("Tasks");

        return app;
    }
}

internal sealed record ClaimTaskRequest(string? VolunteerName);
internal sealed record CompleteTaskRequest(string? CompletedByVolunteerName, List<FieldSubmission> FieldValues);
internal sealed record CancelTaskRequest(string Reason);
internal sealed record DeferTaskRequest(string Reason, DateTimeOffset DeferredTo);
internal sealed record AssignTaskRequest(string? AssignedToUserId);
