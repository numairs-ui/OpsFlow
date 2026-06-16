using MediatR;

namespace OpsFlow.Api.Features.Me.GetMyCompletions;

internal sealed record GetMyCompletionsQuery(string UserId, int Days = 7) : IRequest<List<MyCompletionDto>>;

internal sealed record MyCompletionDto(
    Guid TaskId,
    string TaskName,
    string Status,
    DateTimeOffset CompletedAt);
