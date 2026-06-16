using MediatR;

namespace OpsFlow.Api.Features.RecurringAssignments.PauseRecurringAssignment;

internal sealed record PauseRecurringAssignmentCommand(
    Guid Id,
    bool Resume = false
) : IRequest;
