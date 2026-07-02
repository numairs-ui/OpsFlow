using MediatR;

namespace OpsFlow.Api.Features.Users.UpdateUser;

internal sealed record UpdateUserCommand(
    string UserId, string DisplayName, string Role,
    Guid? StoreId, IReadOnlyList<Guid>? RegionIds) : IRequest;
