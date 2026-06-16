using MediatR;
using OpsFlow.Api.Features.FormTemplates.CreateFormTemplate;
using OpsFlow.Api.Features.FormTemplates.DeactivateFormTemplate;
using OpsFlow.Api.Features.FormTemplates.GetFormTemplate;
using OpsFlow.Api.Features.FormTemplates.GetFormTemplates;
using OpsFlow.Api.Features.FormTemplates.UpdateFormTemplate;

namespace OpsFlow.Api.Features.FormTemplates;

internal static class FormTemplatesEndpoints
{
    internal static IEndpointRouteBuilder MapFormTemplatesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/form-templates").RequireAuthorization().WithTags("FormTemplates");

        group.MapGet("/", async (IMediator m,
            string? scope, string? propagationType, bool? isActive, string? search,
            int page = 1, int pageSize = 20) =>
            Results.Ok(await m.Send(new GetFormTemplatesQuery(scope, propagationType, isActive, search, page, pageSize))));

        group.MapGet("/{id:guid}", async (Guid id, IMediator m) =>
            Results.Ok(await m.Send(new GetFormTemplateQuery(id))));

        group.MapPost("/", async (CreateFormTemplateCommand cmd, IMediator m) =>
        {
            var id = await m.Send(cmd);
            return Results.Created($"/form-templates/{id}", new { id });
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateFormTemplateBody body, IMediator m) =>
        {
            await m.Send(new UpdateFormTemplateCommand(id, body.Name, body.Description, body.PropagationType, body.ApprovalSteps, body.FieldsJson));
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/deactivate", async (Guid id, IMediator m) =>
        {
            await m.Send(new DeactivateFormTemplateCommand(id));
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/activate", async (Guid id, IMediator m) =>
        {
            await m.Send(new DeactivateFormTemplateCommand(id, Activate: true));
            return Results.NoContent();
        });

        return app;
    }
}

internal sealed record UpdateFormTemplateBody(
    string Name,
    string? Description,
    string PropagationType,
    List<ApprovalStepInput> ApprovalSteps,
    string? FieldsJson);
