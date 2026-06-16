using MediatR;

namespace OpsFlow.Api.Features.Tasks.CancelTask;

internal sealed record CancelTaskCommand(Guid TaskId, string Reason) : IRequest;
