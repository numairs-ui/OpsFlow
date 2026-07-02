using MediatR;
using Microsoft.EntityFrameworkCore;
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
        var callerRole = user.GetRole();
        var callerRegionIds = user.GetRegionIds();

        // Only super_admin can grant the super_admin or admin roles.
        if (!Roles.IsSuperAdmin(callerRole) && cmd.Role is Roles.SuperAdmin or Roles.Admin)
            throw new UnauthorizedAccessException("Only super_admin can assign the super_admin or admin role.");

        await using var db = await factory.CreateAsync(ct);
        var profile = await db.UserProfiles.FindAsync([cmd.UserId], ct)
            ?? throw new KeyNotFoundException($"User {cmd.UserId} not found.");

        var regionIds = cmd.RegionIds ?? [];

        // A region-scoped admin is confined to its own region set — for both the user's existing
        // placement and the requested one — so it cannot pull users in from, or push them out to,
        // a region it doesn't own. (CreateUser enforces the same containment for new users.)
        if (!Roles.IsSuperAdmin(callerRole))
        {
            if (callerRole != Roles.Admin)
                throw new UnauthorizedAccessException("Only super_admin or admin can edit users.");

            var currentRegions = UserRegionScope.Decode(profile.RegionIdsCsv, profile.RegionId?.ToString());
            var requestedRegions = regionIds.Select(r => r.ToString()).ToList();
            await AssertWithinCallerRegionsAsync(db, callerRegionIds, profile.Role, profile.StoreId, currentRegions, ct);
            await AssertWithinCallerRegionsAsync(db, callerRegionIds, cmd.Role, cmd.StoreId, requestedRegions, ct);
        }

        var (fkRegion, regionCsv) = UserRegionScope.Encode(regionIds);
        profile.DisplayName = cmd.DisplayName;
        profile.Role = cmd.Role;
        profile.StoreId = cmd.StoreId;
        profile.RegionId = fkRegion;
        profile.RegionIdsCsv = regionCsv;

        // A role/region/store change must take effect immediately: revoke outstanding refresh tokens
        // so the next refresh re-issues scope from the new placement instead of the login-time snapshot.
        var tokens = await db.RefreshTokens
            .Where(t => t.UserId == cmd.UserId && !t.IsUsed)
            .ToListAsync(ct);
        foreach (var t in tokens) t.IsUsed = true;

        // Keep the tenant-DB mirror and the auth store (what login reads) in step: stage the mirror in
        // a transaction, sync auth, then commit. If auth throws, the mirror rolls back; if the commit
        // throws, auth was never touched — neither side is left ahead of the other. (The in-memory
        // provider used in tests has no transaction support, so fall back to a plain save there.)
        var authRequest = new UpdateUserRequest(
            cmd.UserId, tenantId, cmd.Role,
            cmd.StoreId?.ToString(),
            regionIds.Select(r => r.ToString()).ToList());

        if (db.Database.IsRelational())
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            await db.SaveChangesAsync(ct);
            await authProvider.UpdateUserAsync(authRequest, ct);
            await tx.CommitAsync(ct);
        }
        else
        {
            await authProvider.UpdateUserAsync(authRequest, ct);
            await db.SaveChangesAsync(ct);
        }
    }

    /// <summary>
    /// Asserts every region implied by a (role, store, regions) placement is in the caller's region set.
    /// Store-scoped placements resolve through the store's region; region placements use their own set.
    /// </summary>
    private static async Task AssertWithinCallerRegionsAsync(
        TenantDbContext db, IReadOnlyList<string> callerRegionIds,
        string role, Guid? storeId, IReadOnlyList<string> regions, CancellationToken ct)
    {
        List<string> targetRegions;
        if (Roles.IsStoreScoped(role))
        {
            var store = storeId is { } sid ? await db.Stores.FindAsync([sid], ct) : null;
            targetRegions = store is not null ? [store.RegionId.ToString()] : [];
        }
        else
        {
            targetRegions = regions.ToList();
        }

        if (targetRegions.Count == 0 || targetRegions.Any(r => !callerRegionIds.Contains(r)))
            throw new UnauthorizedAccessException("You can only manage users within your assigned regions.");
    }
}
