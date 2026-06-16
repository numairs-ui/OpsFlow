using MediatR;

namespace OpsFlow.Api.Features.Auth.Refresh;

internal static class RefreshEndpoint
{
    internal static IEndpointRouteBuilder MapRefreshEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/refresh", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new RefreshCommand());
            return Results.Ok(new { result.AccessToken, result.ExpiresIn });
        })
        .AllowAnonymous()
        .WithName("RefreshToken")
        .WithTags("Auth");

        return app;
    }
}
