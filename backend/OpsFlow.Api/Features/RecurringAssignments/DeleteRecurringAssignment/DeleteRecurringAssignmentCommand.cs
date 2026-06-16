using MediatR;

namespace OpsFlow.Api.Features.RecurringAssignments.DeleteRecurringAssignment;

internal sealed record DeleteRecurringAssignmentCommand(Guid Id) : IRequest;
