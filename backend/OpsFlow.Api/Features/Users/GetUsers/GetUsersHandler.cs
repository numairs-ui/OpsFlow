using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Domain.Authorization;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Users.GetUsers;

internal sealed class GetUsersHandler(TenantDbContextFactory factory)
    : IRequestHandler<GetUsersQuery, List<UserDto>>
{
    public async Task<List<UserDto>> Handle(GetUsersQuery query, CancellationToken ct)
    {
        await using var db = await factory.CreateAsync(ct);
        var profiles = await db.UserProfiles
            .Include(u => u.Store)
            .Include(u => u.Region)
            .Where(u => (!query.ActiveOnly || u.IsActive)
                     && (query.Role == null || u.Role == query.Role)
                     && (query.StoreId == null || u.StoreId == query.StoreId))
            .OrderBy(u => u.DisplayName)
            .ToListAsync(ct);

        return profiles.Select(u => new UserDto(
            u.UserId, u.Email, u.DisplayName, u.Role,
            u.StoreId, u.Store?.Name,
            u.RegionId, u.Region?.Name,
            u.IsActive, u.MustChangePassword, u.CreatedAt,
            UserRegionScope.Decode(u.RegionIdsCsv, u.RegionId?.ToString()))).ToList();
    }
}
