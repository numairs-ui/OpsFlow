using MediatR;

namespace OpsFlow.Api.Features.Tasks.CompleteTask;

internal sealed record FieldSubmission(Guid TemplateId, string FieldId, string Value);

/// <summary>A per-item score for a checklist session. TemplateId identifies the checklist item;
/// PhotoUrl (optional) is a blob URL from the B5 photo-upload plumbing.</summary>
internal sealed record ItemScoreSubmission(Guid TemplateId, int Score, string? PhotoUrl = null);

internal sealed record CompleteTaskCommand(
    Guid TaskId,
    string? CompletedByVolunteerName,
    List<FieldSubmission> FieldValues,
    List<ItemScoreSubmission>? ItemScores = null
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
    List<CorrectiveActionDto> TriggeredCorrectiveActions,
    decimal? CompositeScorePercent = null,
    List<Guid>? SpawnedCorrectiveTaskIds = null
);
