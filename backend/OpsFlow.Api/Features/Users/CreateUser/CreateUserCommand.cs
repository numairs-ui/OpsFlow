using MediatR;

namespace OpsFlow.Api.Features.Users.CreateUser;

internal sealed record CreateUserCommand(
    string Email,
    string Password,
    string DisplayName,
    string Role,
    Guid? StoreId,
    Guid? RegionId) : IRequest<string>;
