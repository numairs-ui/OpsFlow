using MediatR;
using OpsFlow.Api.Features.DepositLog.GetDepositByDate;
using OpsFlow.Api.Features.DepositLog.GetDepositLog;
using OpsFlow.Api.Features.DepositLog.RecordDeposit;

namespace OpsFlow.Api.Features.DepositLog;

internal static class DepositLogEndpoints
{
    public static IEndpointRouteBuilder MapDepositLogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/stores/{storeId:guid}/deposit-log")
            .RequireAuthorization();

        group.MapPost("/", async (Guid storeId, RecordDepositRequest req, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                var result = await mediator.Send(new RecordDepositCommand(storeId, req.Amount), ct);
                return Results.Created($"/stores/{storeId}/deposit-log/{result.SubmittedAt:yyyy-MM-dd}", result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { detail = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { detail = ex.Message });
            }
        });

        group.MapGet("/", async (
            Guid storeId,
            DateOnly? from,
            DateOnly? to,
            int page,
            int pageSize,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetDepositLogQuery(storeId, from, to, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize),
                ct);
            return Results.Ok(result);
        });

        group.MapGet("/{date}", async (Guid storeId, DateOnly date, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetDepositByDateQuery(storeId, date), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        return app;
    }
}

internal sealed record RecordDepositRequest(decimal Amount);
