using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Security;
using OpsFlow.Domain.Authorization;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Users.GetUsers;

internal sealed class GetUsersHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<GetUsersQuery, List<UserDto>>
{
    public async Task<List<UserDto>> Handle(GetUsersQuery query, CancellationToken ct)
    {
        var spec = httpContextAccessor.HttpContext!.User.ToCaller().Scope();

        await using var db = await factory.CreateAsync(ct);
        var q = db.UserProfiles
            .Include(u => u.Store)
            .Include(u => u.Region)
            .Where(u => (!query.ActiveOnly || u.IsActive)
                     && (query.Role == null || u.Role == query.Role)
                     && (query.StoreId == null || u.StoreId == query.StoreId));

        // Region-scoped admin/supervisor see users at stores in their region set, or users whose
        // own region placement is in that set; a store-scoped caller sees only its own store's
        // roster. super_admin is unrestricted.
        if (!spec.IsGlobal)
        {
            q = q.Where(u =>
                (spec.IsStoreScoped && u.StoreId == spec.StoreId) ||
                (!spec.IsStoreScoped && u.StoreId != null && spec.RegionIds.Contains(u.Store!.RegionId)) ||
                (!spec.IsStoreScoped && u.StoreId == null && u.RegionId != null && spec.RegionIds.Contains(u.RegionId.Value)));
        }

        var profiles = await q.OrderBy(u => u.DisplayName).ToListAsync(ct);

        return profiles.Select(u => new UserDto(
            u.UserId, u.Email, u.DisplayName, u.Role,
            u.StoreId, u.Store?.Name,
            u.RegionId, u.Region?.Name,
            u.IsActive, u.MustChangePassword, u.CreatedAt,
            UserRegionScope.Decode(u.RegionIdsCsv, u.RegionId?.ToString()))).ToList();
    }
}
