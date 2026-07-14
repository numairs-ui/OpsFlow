using MediatR;

namespace OpsFlow.Api.Features.Tasks.GetTask;

internal sealed record GetTaskQuery(Guid TaskId) : IRequest<TaskDetailDto>;

internal sealed record TaskDetailDto(
    Guid Id,
    Guid? RecurringAssignmentId,
    string? RecurringAssignmentName,
    Guid? ChecklistId,
    string ChecklistName,
    string? ChecklistDescription,
    Guid StoreId,
    string StoreName,
    DateTimeOffset DueAt,
    string Status,
    string? AssignedToUserId,
    string? Notes,
    bool IsAdHoc,
    List<TaskTemplateItemDto> Templates,
    DateTimeOffset CreatedAt,
    bool IsMdog,
    Dictionary<string, double> PreviousValues
);

internal sealed record TaskTemplateItemDto(
    Guid TemplateId,
    string TemplateName,
    int Order,
    string FieldsJson,
    // Scoring config (A3) — null ScoringType means this item is not scored.
    string? ScoringType = null,
    bool PhotoRequired = false,
    int? FailScoreThreshold = null
);
