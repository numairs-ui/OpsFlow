using MediatR;
using OpsFlow.Api.Features.Users.AddStoreAssignment;
using OpsFlow.Api.Features.Users.CreateUser;
using OpsFlow.Api.Features.Users.DeactivateUser;
using OpsFlow.Api.Features.Users.GetStoreAssignments;
using OpsFlow.Api.Features.Users.GetUser;
using OpsFlow.Api.Features.Users.GetUserActivity;
using OpsFlow.Api.Features.Users.GetUsers;
using OpsFlow.Api.Features.Users.ReactivateUser;
using OpsFlow.Api.Features.Users.RemoveStoreAssignment;
using OpsFlow.Api.Features.Users.UpdateUser;

namespace OpsFlow.Api.Features.Users;

internal static class UsersEndpoints
{
    internal static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/users").RequireAuthorization().WithTags("Users");

        group.MapGet("/", async (IMediator m, string? role, Guid? storeId, bool activeOnly = true) =>
            Results.Ok(await m.Send(new GetUsersQuery(role, storeId, activeOnly))));

        group.MapPost("/", async (CreateUserCommand cmd, IMediator m) =>
        {
            var userId = await m.Send(cmd);
            return Results.Created($"/users/{userId}", new { userId });
        });

        group.MapGet("/{userId}", async (string userId, IMediator m) =>
            Results.Ok(await m.Send(new GetUserQuery(userId))));

        group.MapPut("/{userId}", async (string userId, UpdateUserBody body, IMediator m) =>
        {
            await m.Send(new UpdateUserCommand(userId, body.DisplayName, body.Role, body.StoreId, body.RegionIds));
            return Results.NoContent();
        });

        group.MapPost("/{userId}/deactivate", async (string userId, IMediator m) =>
        {
            await m.Send(new DeactivateUserCommand(userId));
            return Results.NoContent();
        });

        group.MapPost("/{userId}/reactivate", async (string userId, IMediator m) =>
        {
            await m.Send(new ReactivateUserCommand(userId));
            return Results.NoContent();
        });

        group.MapGet("/{userId}/activity", async (string userId, IMediator m) =>
            Results.Ok(await m.Send(new GetUserActivityQuery(userId))));

        group.MapGet("/{userId}/store-assignments", async (string userId, IMediator m) =>
            Results.Ok(await m.Send(new GetStoreAssignmentsQuery(userId))));

        group.MapPost("/{userId}/store-assignments", async (string userId, StoreAssignmentBody body, IMediator m) =>
        {
            await m.Send(new AddStoreAssignmentCommand(userId, body.StoreId));
            return Results.NoContent();
        });

        group.MapDelete("/{userId}/store-assignments/{storeId:guid}", async (string userId, Guid storeId, IMediator m) =>
        {
            await m.Send(new RemoveStoreAssignmentCommand(userId, storeId));
            return Results.NoContent();
        });

        return app;
    }
}

internal sealed record UpdateUserBody(string DisplayName, string Role, Guid? StoreId, IReadOnlyList<Guid>? RegionIds);
internal sealed record StoreAssignmentBody(Guid StoreId);
