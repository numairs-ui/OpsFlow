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
    IReadOnlyList<RecurringAssignmentTargetDto> TargetStores,
    string CronExpression,
    DateTimeOffset StartsAt,
    DateTimeOffset? EndsAt,
    bool IsPaused,
    int TaskInstanceCount,
    DateTimeOffset CreatedAt,
    string? AssignedToUserId,
    string? AssignedToUserName
);

internal sealed record RecurringAssignmentTargetDto(Guid StoreId, string StoreName);
