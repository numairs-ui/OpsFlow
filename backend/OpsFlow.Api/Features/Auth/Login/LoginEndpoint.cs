using MediatR;
using OpsFlow.Api.Services;

namespace OpsFlow.Api.Features.Auth.Login;

internal static class LoginEndpoint
{
    internal static IEndpointRouteBuilder MapLoginEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", async (
            LoginCommand cmd,
            IMediator mediator,
            TokenService tokenService,
            HttpContext ctx) =>
        {
            var result = await mediator.Send(cmd);
            tokenService.SetRefreshCookie(ctx.Response, result.TenantId, result.RawRefreshToken, result.RefreshExpiresAt);
            return Results.Ok(new { result.AccessToken, result.ExpiresIn });
        })
        .AllowAnonymous()
        .WithName("Login")
        .WithTags("Auth");

        return app;
    }
}
