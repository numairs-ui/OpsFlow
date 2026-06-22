using MediatR;
using OpsFlow.Api.Features.FormSubmissions.ApproveFormSubmission;
using OpsFlow.Api.Features.FormSubmissions.CreateFormSubmission;
using OpsFlow.Api.Features.FormSubmissions.GetFormSubmission;
using OpsFlow.Api.Features.FormSubmissions.GetFormSubmissions;
using OpsFlow.Api.Features.FormSubmissions.GetMySubmissions;
using OpsFlow.Api.Features.FormSubmissions.GetPendingReview;
using OpsFlow.Api.Features.FormSubmissions.RejectFormSubmission;
using OpsFlow.Api.Features.FormSubmissions.ReturnFormSubmission;
using OpsFlow.Api.Features.FormSubmissions.SubmitFormSubmission;
using OpsFlow.Api.Features.FormSubmissions.UpdateDraft;

namespace OpsFlow.Api.Features.FormSubmissions;

internal static class FormSubmissionsEndpoints
{
    internal static IEndpointRouteBuilder MapFormSubmissionsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/form-submissions").RequireAuthorization().WithTags("FormSubmissions");

        // Static segments must be mapped before the {id:guid} catch-all to avoid route conflicts
        group.MapGet("/my-submissions", async (string? status, IMediator m) =>
            Results.Ok(await m.Send(new GetMySubmissionsQuery(status))));

        group.MapGet("/pending-review", async (IMediator m) =>
            Results.Ok(await m.Send(new GetPendingReviewQuery())));

        group.MapGet("/", async (Guid? storeId, Guid? regionId, string? status, IMediator m) =>
            Results.Ok(await m.Send(new GetFormSubmissionsQuery(storeId, regionId, status))));

        group.MapGet("/{id:guid}", async (Guid id, IMediator m) =>
            Results.Ok(await m.Send(new GetFormSubmissionQuery(id))));

        group.MapPost("/", async (CreateFormSubmissionBody body, IMediator m) =>
        {
            var id = await m.Send(new CreateFormSubmissionCommand(body.FormTemplateId, body.StoreId, body.FieldValues));
            return Results.Created($"/form-submissions/{id}", new { id });
        });

        group.MapPost("/{id:guid}/submit", async (Guid id, SubmitFormSubmissionBody? body, IMediator m) =>
        {
            await m.Send(new SubmitFormSubmissionCommand(id, body?.FieldValues));
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/approve", async (Guid id, IMediator m) =>
        {
            await m.Send(new ApproveFormSubmissionCommand(id));
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/reject", async (Guid id, RejectFormSubmissionBody body, IMediator m) =>
        {
            await m.Send(new RejectFormSubmissionCommand(id, body.Reason));
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/return", async (Guid id, ReturnFormSubmissionBody body, IMediator m) =>
        {
            await m.Send(new ReturnFormSubmissionCommand(id, body.Comments));
            return Results.NoContent();
        });

        group.MapPatch("/{id:guid}/draft", async (Guid id, UpdateDraftBody body, IMediator m) =>
        {
            await m.Send(new UpdateDraftCommand(id, body.FieldValues));
            return Results.NoContent();
        });

        return app;
    }
}

internal sealed record CreateFormSubmissionBody(Guid? FormTemplateId, Guid StoreId, Dictionary<string, string> FieldValues);
internal sealed record SubmitFormSubmissionBody(Dictionary<string, string>? FieldValues);
internal sealed record RejectFormSubmissionBody(string Reason);
internal sealed record ReturnFormSubmissionBody(string Comments);
internal sealed record UpdateDraftBody(Dictionary<string, string> FieldValues);
