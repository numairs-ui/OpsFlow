using MediatR;

namespace OpsFlow.Api.Features.Tasks.ClaimTask;

internal sealed record ClaimTaskCommand(Guid TaskId, string? VolunteerName = null) : IRequest;
