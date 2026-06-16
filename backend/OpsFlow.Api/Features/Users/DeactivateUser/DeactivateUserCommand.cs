using MediatR;

namespace OpsFlow.Api.Features.Users.DeactivateUser;

internal sealed record DeactivateUserCommand(string UserId) : IRequest;
