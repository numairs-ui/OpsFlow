using MediatR;
using OpsFlow.Api.Features.Me.GetMyCompletions;
using System.Security.Claims;

namespace OpsFlow.Api.Features.Me;

internal static class MeEndpoints
{
    public static IEndpointRouteBuilder MapMeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/users/me").RequireAuthorization();

        group.MapGet("/completions", async (int days, HttpContext ctx, IMediator mediator, CancellationToken ct) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? ctx.User.FindFirstValue("sub");
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            var result = await mediator.Send(new GetMyCompletionsQuery(userId, days < 1 ? 7 : days), ct);
            return Results.Ok(result);
        });

        return app;
    }
}
