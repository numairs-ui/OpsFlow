using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Hubs;
using OpsFlow.Domain.Entities;
using OpsFlow.Infrastructure;
using System.Text.Json;

namespace OpsFlow.Api.Features.FormSubmissions.SubmitFormSubmission;

internal sealed class SubmitFormSubmissionHandler(
    TenantDbContextFactory factory,
    IHubContext<TaskBoardHub> hub) : IRequestHandler<SubmitFormSubmissionCommand>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task Handle(SubmitFormSubmissionCommand cmd, CancellationToken ct)
    {
        await using var db = await factory.CreateAsync(ct);

        var submission = await db.FormSubmissions
            .Include(s => s.ApprovalSteps)
            .Include(s => s.FormTemplate)
            .FirstOrDefaultAsync(s => s.Id == cmd.Id, ct)
            ?? throw new KeyNotFoundException($"Form submission {cmd.Id} not found.");

        if (submission.Status is not ("Draft" or "Returned"))
            throw new InvalidOperationException($"Cannot submit a submission in status '{submission.Status}'.");

        if (cmd.FieldValues != null)
            submission.FieldValuesJson = JsonSerializer.Serialize(cmd.FieldValues);

        var template = submission.FormTemplate
            ?? throw new InvalidOperationException("Submission has no associated form template.");

        // Validate required fields
        var fieldValues = JsonSerializer.Deserialize<Dictionary<string, string>>(submission.FieldValuesJson, JsonOptions) ?? [];
        var fieldSpecs = JsonSerializer.Deserialize<List<TemplateFieldSpec>>(template.FieldsJson, JsonOptions) ?? [];
        var errors = new List<ValidationFailure>();
        foreach (var spec in fieldSpecs.Where(s => s.Required))
        {
            if (!fieldValues.TryGetValue(spec.Id, out var val) || string.IsNullOrWhiteSpace(val))
                errors.Add(new ValidationFailure($"FieldValues[{spec.Id}]", $"Field '{spec.Label}' is required."));
        }
        if (errors.Count > 0) throw new ValidationException(errors);

        var steps = JsonSerializer.Deserialize<List<ApprovalStepSpec>>(template.ApprovalStepsJson, JsonOptions) ?? [];
        var now = DateTimeOffset.UtcNow;
        submission.SubmittedAt = now;

        if (template.PropagationType == "NotificationOnly")
        {
            foreach (var step in steps.OrderBy(s => s.Order))
            {
                db.FormSubmissionApprovalSteps.Add(new FormSubmissionApprovalStep
                {
                    SubmissionId = submission.Id,
                    StepOrder = step.Order,
                    Role = step.Role,
                    Action = "Recorded",
                    ActionAt = now,
                });
            }
            submission.Status = "Recorded";
            submission.ResolvedAt = now;
            submission.CurrentStepOrder = null;
        }
        else if (submission.Status == "Returned")
        {
            if (template.PropagationType == "Sequential")
            {
                var returningStep = submission.ApprovalSteps
                    .Where(s => s.Action == "Returned")
                    .OrderByDescending(s => s.ActionAt)
                    .First();

                db.FormSubmissionApprovalSteps.Add(new FormSubmissionApprovalStep
                {
                    SubmissionId = submission.Id,
                    StepOrder = returningStep.StepOrder,
                    Role = returningStep.Role,
                    Action = "Pending",
                });
                submission.CurrentStepOrder = returningStep.StepOrder;
            }
            else // Parallel
            {
                foreach (var step in steps.OrderBy(s => s.Order))
                {
                    db.FormSubmissionApprovalSteps.Add(new FormSubmissionApprovalStep
                    {
                        SubmissionId = submission.Id,
                        StepOrder = step.Order,
                        Role = step.Role,
                        Action = "Pending",
                    });
                }
                submission.CurrentStepOrder = steps.Count > 0 ? steps.Min(s => s.Order) : 1;
            }
            submission.Status = "PendingApproval";
        }
        else // Draft -> first submission, Sequential or Parallel
        {
            foreach (var step in steps.OrderBy(s => s.Order))
            {
                db.FormSubmissionApprovalSteps.Add(new FormSubmissionApprovalStep
                {
                    SubmissionId = submission.Id,
                    StepOrder = step.Order,
                    Role = step.Role,
                    Action = "Pending",
                });
            }
            submission.CurrentStepOrder = steps.Count > 0 ? steps.Min(s => s.Order) : 1;
            submission.Status = "PendingApproval";
        }

        await db.SaveChangesAsync(ct);

        // FCM push deferred — blocked on TB-06 (Angular PWA + FCM infra). SignalR broadcast only.
        await hub.Clients.Group($"store-{submission.StoreId}").SendAsync(
            "FormSubmissionUpdated",
            new { submission.Id, submission.Status, Event = submission.Status == "Recorded" ? "FormRecorded" : "FormSubmitted" },
            ct);
    }

    private sealed class TemplateFieldSpec
    {
        public string Id { get; init; } = "";
        public string Label { get; init; } = "";
        public bool Required { get; init; }
    }

    private sealed class ApprovalStepSpec
    {
        public string Role { get; init; } = "";
        public int Order { get; init; }
    }
}
