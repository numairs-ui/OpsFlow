using MediatR;

namespace OpsFlow.Api.Features.RecurringAssignments.GetRecurringHealth;

internal sealed record GetRecurringHealthQuery : IRequest<RecurringHealthDto>;

internal sealed record RecurringHealthDto(
    int ActiveCount,
    int PausedCount,
    int InstancesGeneratedThisWeek,
    int StaleCount,
    List<RecurringAssignmentHealthDto> Assignments);

internal sealed record RecurringAssignmentHealthDto(
    Guid Id,
    string Name,
    Guid StoreId,
    string StoreName,
    string ChecklistName,
    bool IsPaused,
    string CronExpression,
    DateTimeOffset? NextFireAt,
    DateTimeOffset? LastGeneratedAt,
    int InstancesThisWeek,
    bool IsStale);
