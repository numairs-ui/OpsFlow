using MediatR;

namespace OpsFlow.Api.Features.Auth.Logout;

internal static class LogoutEndpoint
{
    internal static IEndpointRouteBuilder MapLogoutEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/logout", async (IMediator mediator) =>
        {
            await mediator.Send(new LogoutCommand());
            return Results.NoContent();
        })
        .AllowAnonymous()
        .WithName("Logout")
        .WithTags("Auth");

        return app;
    }
}
