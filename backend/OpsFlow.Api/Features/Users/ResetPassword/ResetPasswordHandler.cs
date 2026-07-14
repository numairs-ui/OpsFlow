using System.Security.Cryptography;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Security;
using OpsFlow.Domain.Authorization;
using OpsFlow.Domain.Interfaces;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Users.ResetPassword;

internal sealed class ResetPasswordHandler(
    IAuthProvider authProvider,
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor)
    : IRequestHandler<ResetPasswordCommand, ResetPasswordResponse>
{
    public async Task<ResetPasswordResponse> Handle(ResetPasswordCommand cmd, CancellationToken ct)
    {
        var caller = httpContextAccessor.HttpContext!.User;
        var callerRole = caller.GetRole();
        var callerRegionIds = caller.GetRegionIds();

        await using var db = await factory.CreateAsync(ct);
        var profile = await db.UserProfiles.FindAsync([cmd.UserId], ct)
            ?? throw new KeyNotFoundException($"User {cmd.UserId} not found.");

        // Authorization mirrors UpdateUserHandler: only super_admin / admin can manage users, and a
        // region-scoped admin is confined to users whose current placement falls within its regions.
        if (!Roles.IsSuperAdmin(callerRole))
        {
            if (callerRole != Roles.Admin)
                throw new UnauthorizedAccessException("Only super_admin or admin can reset passwords.");

            var currentRegions = UserRegionScope.Decode(profile.RegionIdsCsv, profile.RegionId?.ToString());
            await AssertWithinCallerRegionsAsync(db, callerRegionIds, profile.Role, profile.StoreId, currentRegions, ct);
        }

        var tempPassword = cmd.NewPassword ?? GenerateTempPassword();

        // Invalidate outstanding refresh tokens so old sessions can't survive the credential change.
        var tokens = await db.RefreshTokens
            .Where(t => t.UserId == cmd.UserId && !t.IsUsed)
            .ToListAsync(ct);
        foreach (var t in tokens) t.IsUsed = true;

        await authProvider.ResetPasswordAsync(cmd.UserId, tempPassword, ct);
        await db.SaveChangesAsync(ct);

        return new ResetPasswordResponse(tempPassword);
    }

    /// <summary>
    /// Asserts every region implied by a (role, store, regions) placement is in the caller's region set.
    /// Store-scoped placements resolve through the store's region; region placements use their own set.
    /// (Same containment rule UpdateUserHandler applies.)
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

    // Character classes chosen so the result satisfies both ASP.NET Identity's default policy
    // (upper, lower, digit, non-alphanumeric) and the 8-char minimum used elsewhere. Ambiguous
    // glyphs (0/O, 1/l/I) are omitted so a hand-typed temp password is less error-prone.
    private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lower = "abcdefghijkmnpqrstuvwxyz";
    private const string Digits = "23456789";
    private const string Symbols = "!@#$%^&*?";

    private static string GenerateTempPassword()
    {
        var all = Upper + Lower + Digits + Symbols;
        // Guarantee one of each required class, then fill to length 14.
        var chars = new List<char>
        {
            Pick(Upper), Pick(Lower), Pick(Digits), Pick(Symbols),
        };
        while (chars.Count < 14) chars.Add(Pick(all));

        // Fisher–Yates shuffle so the guaranteed classes aren't always in the first four positions.
        for (var i = chars.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
        return new string(chars.ToArray());
    }

    private static char Pick(string set) => set[RandomNumberGenerator.GetInt32(set.Length)];
}
