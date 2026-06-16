using MediatR;

namespace OpsFlow.Api.Features.Users.ReactivateUser;

internal sealed record ReactivateUserCommand(string UserId) : IRequest;
