using MediatR;

namespace OpsFlow.Api.Features.Tasks.VerifyTask;

internal sealed record VerifyTaskCommand(Guid TaskId) : IRequest;
