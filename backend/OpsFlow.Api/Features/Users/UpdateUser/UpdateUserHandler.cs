using MediatR;
using OpsFlow.Api.Security;
using OpsFlow.Domain.Authorization;
using OpsFlow.Domain.Interfaces;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Users.UpdateUser;

internal sealed class UpdateUserHandler(
    IAuthProvider authProvider,
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor)
    : IRequestHandler<UpdateUserCommand>
{
    public async Task Handle(UpdateUserCommand cmd, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var tenantId = user.GetTenantId();

        // Only super_admin can grant the super_admin or admin roles.
        if (!Roles.IsSuperAdmin(user.GetRole()) && cmd.Role is Roles.SuperAdmin or Roles.Admin)
            throw new UnauthorizedAccessException("Only super_admin can assign the super_admin or admin role.");

        await using var db = await factory.CreateAsync(ct);
        var profile = await db.UserProfiles.FindAsync([cmd.UserId], ct)
            ?? throw new KeyNotFoundException($"User {cmd.UserId} not found.");

        var regionIds = cmd.RegionIds ?? [];

        // Sync the auth provider first — it is what login reads — then mirror onto the profile.
        await authProvider.UpdateUserAsync(new UpdateUserRequest(
            cmd.UserId, tenantId, cmd.Role,
            cmd.StoreId?.ToString(),
            regionIds.Select(r => r.ToString()).ToList()), ct);

        var (fkRegion, regionCsv) = UserRegionScope.Encode(regionIds);
        profile.DisplayName = cmd.DisplayName;
        profile.Role = cmd.Role;
        profile.StoreId = cmd.StoreId;
        profile.RegionId = fkRegion;
        profile.RegionIdsCsv = regionCsv;
        await db.SaveChangesAsync(ct);
    }
}
