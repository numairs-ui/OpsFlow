using MediatR;

namespace OpsFlow.Api.Features.Tasks.GetTodayTasks;

internal sealed record GetTodayTasksQuery(Guid StoreId) : IRequest<TodayTasksDto>;

internal sealed record TodayTasksDto(
    string Date,
    Guid StoreId,
    string StoreName,
    int TotalCount,
    int CompletedCount,
    List<TaskGroupDto> TaskGroups
);

internal sealed record TaskGroupDto(
    // null for the standalone bucket (tasks with no checklist).
    Guid? ChecklistId,
    string ChecklistName,
    int TotalCount,
    int CompletedCount,
    List<TaskBoardItemDto> Tasks
);

internal sealed record TaskBoardItemDto(
    Guid Id,
    DateTimeOffset DueAt,
    string Status,
    string? AssignedToUserId,
    bool IsAdHoc,
    string? RecurringAssignmentName,
    DateTimeOffset CreatedAt
);
