using MediatR;

namespace OpsFlow.Api.Features.Tasks.StartTask;

// Transitions a task from Pending → InProgress when a user opens the detail view
internal sealed record StartTaskCommand(Guid TaskId) : IRequest;
