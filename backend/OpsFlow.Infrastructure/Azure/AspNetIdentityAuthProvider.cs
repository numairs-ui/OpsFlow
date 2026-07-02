using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using OpsFlow.Domain.Interfaces;

namespace OpsFlow.Infrastructure.Azure;

internal sealed class AspNetIdentityAuthProvider(UserManager<IdentityUser> userManager) : IAuthProvider
{
    public async Task<AuthResult?> AuthenticateAsync(string email, string password, string tenantId, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null || !await userManager.CheckPasswordAsync(user, password))
            return null;

        // Claims (role, storeId, regionId) are stored as Identity claims and read here
        var claims = await userManager.GetClaimsAsync(user);
        var role = claims.FirstOrDefault(c => c.Type == "role")?.Value ?? string.Empty;
        var storeId = claims.FirstOrDefault(c => c.Type == "storeId")?.Value;
        var regionIds = claims.Where(c => c.Type == "regionId").Select(c => c.Value).ToList();

        return new AuthResult(user.Id, tenantId, role, storeId, regionIds);
    }

    public async Task<string> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var user = new IdentityUser { UserName = request.Email, Email = request.Email };
        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        return user.Id;
    }

    public async Task UpdateUserAsync(UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(request.UserId)
            ?? throw new InvalidOperationException($"User {request.UserId} not found.");

        // Replace the authorization claims wholesale so removals (e.g. a dropped store) take effect.
        var existing = await userManager.GetClaimsAsync(user);
        var stale = existing.Where(c => c.Type is "role" or "storeId" or "regionId").ToList();
        if (stale.Count > 0) await userManager.RemoveClaimsAsync(user, stale);

        var fresh = new List<Claim> { new("role", request.Role) };
        if (request.StoreId is not null) fresh.Add(new Claim("storeId", request.StoreId));
        fresh.AddRange(request.RegionIds.Select(r => new Claim("regionId", r)));
        await userManager.AddClaimsAsync(user, fresh);
    }

    public async Task ResetPasswordAsync(string userId, string newPassword, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new InvalidOperationException($"User {userId} not found.");
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        await userManager.ResetPasswordAsync(user, token, newPassword);
    }
}
