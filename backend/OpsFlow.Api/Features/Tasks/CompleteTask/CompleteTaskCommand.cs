using MediatR;

namespace OpsFlow.Api.Features.Tasks.CompleteTask;

internal sealed record FieldSubmission(Guid TemplateId, string FieldId, string Value);

internal sealed record CompleteTaskCommand(
    Guid TaskId,
    string? CompletedByVolunteerName,
    List<FieldSubmission> FieldValues
) : IRequest<CompleteTaskResponse>;

internal sealed record CorrectiveActionDto(string FieldLabel, string Text);

internal sealed record TaskCompletionResultDto(
    Guid Id,
    Guid TaskInstanceId,
    string? CompletedByUserId,
    string? CompletedByVolunteerName,
    DateTimeOffset CompletedAt
);

internal sealed record CompleteTaskResponse(
    TaskCompletionResultDto Completion,
    List<CorrectiveActionDto> TriggeredCorrectiveActions
);
