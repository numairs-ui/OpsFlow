using MediatR;
using OpsFlow.Api.Features.TenantSettings.GetTenantSettings;
using OpsFlow.Api.Features.TenantSettings.UpdateTenantSettings;

namespace OpsFlow.Api.Features.TenantSettings;

internal static class TenantSettingsEndpoints
{
    internal static IEndpointRouteBuilder MapTenantSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/tenant/settings").RequireAuthorization().WithTags("TenantSettings");

        group.MapGet("/", async (IMediator m) =>
            Results.Ok(await m.Send(new GetTenantSettingsQuery())));

        group.MapPut("/", async (UpdateTenantSettingsBody body, IMediator m) =>
        {
            await m.Send(new UpdateTenantSettingsCommand(body.Name, body.LogoUrl, body.PrimaryContactEmail));
            return Results.NoContent();
        });

        return app;
    }
}

internal sealed record UpdateTenantSettingsBody(
    string Name,
    string? LogoUrl,
    string? PrimaryContactEmail);
