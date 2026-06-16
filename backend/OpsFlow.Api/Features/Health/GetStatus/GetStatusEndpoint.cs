using MediatR;

namespace OpsFlow.Api.Features.Health.GetStatus;

internal static class GetStatusEndpoint
{
    internal static void MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", async (IMediator mediator, CancellationToken ct) =>
        {
            var response = await mediator.Send(new GetStatusQuery(), ct);
            return Results.Ok(response);
        })
        .AllowAnonymous()
        .WithName("GetStatus")
        .WithTags("Health")
        .Produces<GetStatusResponse>();
    }
}
