using MediatR;

namespace OpsFlow.Api.Features.Users.GetUsers;

internal sealed record GetUsersQuery(string? Role = null, Guid? StoreId = null, bool ActiveOnly = true)
    : IRequest<List<UserDto>>;

internal sealed record UserDto(
    string UserId, string Email, string DisplayName, string Role,
    Guid? StoreId, string? StoreName, Guid? RegionId, string? RegionName,
    bool IsActive, bool MustChangePassword, DateTimeOffset CreatedAt,
    IReadOnlyList<string> RegionIds);
