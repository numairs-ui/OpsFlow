using MediatR;
using OpsFlow.Api.Features.Users.GetUsers;

namespace OpsFlow.Api.Features.Users.GetUser;

internal sealed record GetUserQuery(string UserId) : IRequest<UserDto>;
