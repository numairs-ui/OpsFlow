using MediatR;

namespace OpsFlow.Api.Features.Users.GetUserActivity;

internal sealed record GetUserActivityQuery(string UserId) : IRequest<List<UserActivityDto>>;

internal sealed record UserActivityDto(
    string Type,
    string Title,
    string Status,
    DateTimeOffset Date);
