using MediatR;
using OpsFlow.Api.Features.Dashboard.GetRegionDashboard;
using OpsFlow.Api.Features.Dashboard.GetStoreDashboard;
using OpsFlow.Api.Features.Dashboard.GetSystemDashboard;

namespace OpsFlow.Api.Features.Dashboard;

internal static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/dashboard").RequireAuthorization();

        group.MapGet("/store/{storeId:guid}", async (Guid storeId, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetStoreDashboardQuery(storeId), ct)));

        group.MapGet("/region/{regionId:guid}", async (Guid regionId, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetRegionDashboardQuery(regionId), ct)));

        group.MapGet("/system", async (IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetSystemDashboardQuery(), ct)));

        return app;
    }
}
