using MediatR;

namespace OpsFlow.Api.Features.Tasks.GetTasks;

internal sealed record GetTasksQuery(
    Guid? StoreId,
    string? Status,
    DateTimeOffset? From,
    DateTimeOffset? To,
    IReadOnlyCollection<string>? Statuses = null
) : IRequest<List<TaskInstanceDto>>;

internal sealed record TaskInstanceDto(
    Guid Id,
    Guid? RecurringAssignmentId,
    string? RecurringAssignmentName,
    Guid? ChecklistId,
    string ChecklistName,
    Guid StoreId,
    string StoreName,
    DateTimeOffset DueAt,
    string Status,
    string? AssignedToUserId,
    string? CompletedByUserId,
    DateTimeOffset? CompletedAt,
    string? Notes,
    bool IsAdHoc,
    DateTimeOffset CreatedAt
);
