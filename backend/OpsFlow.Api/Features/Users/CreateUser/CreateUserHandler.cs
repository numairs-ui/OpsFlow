using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Security;
using OpsFlow.Domain.Authorization;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Interfaces;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Users.CreateUser;

internal sealed class CreateUserHandler(
    IAuthProvider authProvider,
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<CreateUserCommand, string>
{
    public async Task<string> Handle(CreateUserCommand cmd, CancellationToken ct)
    {
        var caller = httpContextAccessor.HttpContext!.User;
        var tenantId = caller.GetTenantId();
        var callerRole = caller.GetRole();
        var callerRegionIds = caller.GetRegionIds();

        var regionIds = cmd.RegionIds ?? [];
        var regionIdStrings = regionIds.Select(r => r.ToString()).ToList();

        await using var db = await factory.CreateAsync(ct);

        // Only super_admin and admin may create users. A region-scoped admin cannot mint
        // super_admin/admin peers and is confined to its own region set.
        if (!Roles.IsSuperAdmin(callerRole))
        {
            if (callerRole != Roles.Admin)
                throw new UnauthorizedAccessException("Only super_admin or admin can create users.");
            if (cmd.Role is Roles.SuperAdmin or Roles.Admin)
                throw new UnauthorizedAccessException("Admins cannot create super_admin or admin accounts.");

            List<string> targetRegions;
            if (Roles.IsStoreScoped(cmd.Role))
            {
                var store = cmd.StoreId is { } sid ? await db.Stores.FindAsync([sid], ct) : null;
                targetRegions = store is not null ? [store.RegionId.ToString()] : [];
            }
            else
            {
                targetRegions = regionIdStrings;
            }

            if (targetRegions.Count == 0 || targetRegions.Any(r => !callerRegionIds.Contains(r)))
                throw new UnauthorizedAccessException("You can only create users within your assigned regions.");
        }

        var userId = await authProvider.CreateUserAsync(new CreateUserRequest(
            cmd.Email, cmd.Password, cmd.Role, tenantId,
            cmd.StoreId?.ToString(), regionIdStrings), ct);

        var (fkRegion, regionCsv) = UserRegionScope.Encode(regionIds);
        db.UserProfiles.Add(new UserProfile
        {
            UserId = userId,
            Email = cmd.Email,
            DisplayName = cmd.DisplayName,
            Role = cmd.Role,
            StoreId = cmd.StoreId,
            RegionId = fkRegion,
            RegionIdsCsv = regionCsv,
        });
        await db.SaveChangesAsync(ct);
        return userId;
    }
}
