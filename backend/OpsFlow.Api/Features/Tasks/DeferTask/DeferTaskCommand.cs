using MediatR;

namespace OpsFlow.Api.Features.Tasks.DeferTask;

internal sealed record DeferTaskCommand(Guid TaskId, string Reason, DateTimeOffset DeferredTo) : IRequest;
