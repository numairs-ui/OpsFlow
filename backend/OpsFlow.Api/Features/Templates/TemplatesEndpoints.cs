using MediatR;
using OpsFlow.Api.Features.Templates.CreateTemplate;
using OpsFlow.Api.Features.Templates.DeactivateTemplate;
using OpsFlow.Api.Features.Templates.GetTemplate;
using OpsFlow.Api.Features.Templates.GetTemplates;
using OpsFlow.Api.Features.Templates.ImportTemplates;
using OpsFlow.Api.Features.Templates.UpdateTemplate;

namespace OpsFlow.Api.Features.Templates;

internal static class TemplatesEndpoints
{
    internal static IEndpointRouteBuilder MapTemplatesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/templates").RequireAuthorization().WithTags("Templates");

        group.MapGet("/", async (IMediator m,
            string? scope, string? category, bool? isActive, string? search,
            int page = 1, int pageSize = 20) =>
            Results.Ok(await m.Send(new GetTemplatesQuery(scope, category, isActive, search, page, pageSize))));

        group.MapGet("/{id:guid}", async (Guid id, IMediator m) =>
            Results.Ok(await m.Send(new GetTemplateQuery(id))));

        group.MapPost("/", async (CreateTemplateCommand cmd, IMediator m) =>
        {
            var id = await m.Send(cmd);
            return Results.Created($"/templates/{id}", new { id });
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateTemplateBody body, IMediator m) =>
        {
            await m.Send(new UpdateTemplateCommand(id, body.Name, body.Description, body.Category, body.FieldsJson));
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/deactivate", async (Guid id, IMediator m) =>
        {
            await m.Send(new DeactivateTemplateCommand(id));
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/activate", async (Guid id, IMediator m) =>
        {
            await m.Send(new DeactivateTemplateCommand(id, Activate: true));
            return Results.NoContent();
        });

        group.MapPost("/import", async (ImportTemplatesCommand cmd, IMediator m) =>
            Results.Ok(await m.Send(cmd)));

        return app;
    }
}

internal sealed record UpdateTemplateBody(string Name, string? Description, string Category, string? FieldsJson);
