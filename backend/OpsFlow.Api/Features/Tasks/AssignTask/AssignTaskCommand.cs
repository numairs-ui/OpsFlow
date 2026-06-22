using MediatR;
namespace OpsFlow.Api.Features.Tasks.AssignTask;

internal sealed record AssignTaskCommand(Guid TaskId, string? AssignedToUserId) : IRequest;
