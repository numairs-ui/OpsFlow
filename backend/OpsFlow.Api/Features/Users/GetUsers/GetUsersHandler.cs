using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Users.GetUsers;

internal sealed class GetUsersHandler(TenantDbContextFactory factory)
    : IRequestHandler<GetUsersQuery, List<UserDto>>
{
    public async Task<List<UserDto>> Handle(GetUsersQuery query, CancellationToken ct)
    {
        await using var db = await factory.CreateAsync(ct);
        return await db.UserProfiles
            .Include(u => u.Store)
            .Include(u => u.Region)
            .Where(u => (!query.ActiveOnly || u.IsActive)
                     && (query.Role == null || u.Role == query.Role)
                     && (query.StoreId == null || u.StoreId == query.StoreId))
            .OrderBy(u => u.DisplayName)
            .Select(u => new UserDto(
                u.UserId, u.Email, u.DisplayName, u.Role,
                u.StoreId, u.Store != null ? u.Store.Name : null,
                u.RegionId, u.Region != null ? u.Region.Name : null,
                u.IsActive, u.MustChangePassword, u.CreatedAt))
            .ToListAsync(ct);
    }
}
