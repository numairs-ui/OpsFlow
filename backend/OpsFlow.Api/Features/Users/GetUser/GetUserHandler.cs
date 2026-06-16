using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Features.Users.GetUsers;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Users.GetUser;

internal sealed class GetUserHandler(TenantDbContextFactory factory)
    : IRequestHandler<GetUserQuery, UserDto>
{
    public async Task<UserDto> Handle(GetUserQuery query, CancellationToken ct)
    {
        await using var db = await factory.CreateAsync(ct);
        var u = await db.UserProfiles
            .Include(u => u.Store).Include(u => u.Region)
            .FirstOrDefaultAsync(u => u.UserId == query.UserId, ct)
            ?? throw new KeyNotFoundException($"User {query.UserId} not found.");

        return new UserDto(
            u.UserId, u.Email, u.DisplayName, u.Role,
            u.StoreId, u.Store?.Name, u.RegionId, u.Region?.Name,
            u.IsActive, u.MustChangePassword, u.CreatedAt);
    }
}
