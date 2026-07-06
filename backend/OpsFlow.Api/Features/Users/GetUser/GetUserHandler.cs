using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Features.Users.GetUsers;
using OpsFlow.Api.Security;
using OpsFlow.Domain.Authorization;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Users.GetUser;

internal sealed class GetUserHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<GetUserQuery, UserDto>
{
    public async Task<UserDto> Handle(GetUserQuery query, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var spec = user.ToCaller().Scope();
        var callerId = user.GetUserId();

        await using var db = await factory.CreateAsync(ct);
        var u = await db.UserProfiles
            .Include(u => u.Store).Include(u => u.Region)
            .FirstOrDefaultAsync(u => u.UserId == query.UserId, ct)
            ?? throw new KeyNotFoundException($"User {query.UserId} not found.");

        // Authorize: anyone may read their own profile. Reading another user's is a management
        // function — same rule as GetUserActivityHandler: store-scoped roles get only themselves,
        // a region role reads users within its region set, super_admin reads anyone.
        if (u.UserId != callerId)
        {
            if (spec.IsStoreScoped)
                throw new UnauthorizedAccessException("You can only view your own profile.");

            if (u.StoreId is { } storeId)
                spec.AssertCanViewStore(u.Store?.RegionId ?? Guid.Empty, storeId);
            else if (u.RegionId is { } regionId)
                spec.AssertCanViewRegion(regionId);
            else
                spec.AssertGlobal();
        }

        return new UserDto(
            u.UserId, u.Email, u.DisplayName, u.Role,
            u.StoreId, u.Store?.Name, u.RegionId, u.Region?.Name,
            u.IsActive, u.MustChangePassword, u.CreatedAt,
            UserRegionScope.Decode(u.RegionIdsCsv, u.RegionId?.ToString()));
    }
}
