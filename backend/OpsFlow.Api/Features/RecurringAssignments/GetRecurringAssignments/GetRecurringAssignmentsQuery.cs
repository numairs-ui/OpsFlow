using MediatR;

namespace OpsFlow.Api.Features.RecurringAssignments.GetRecurringAssignments;

internal sealed record GetRecurringAssignmentsQuery(
    Guid? StoreId,
    bool? IsPaused
) : IRequest<List<RecurringAssignmentDto>>;

internal sealed record RecurringAssignmentDto(
    Guid Id,
    string Name,
    Guid ChecklistId,
    string ChecklistName,
    Guid StoreId,
    string StoreName,
    string CronExpression,
    DateTimeOffset StartsAt,
    DateTimeOffset? EndsAt,
    bool IsPaused,
    int TaskInstanceCount,
    DateTimeOffset CreatedAt
);
