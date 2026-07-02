using System.Security.Claims;
using OpsFlow.Domain.Authorization;

namespace OpsFlow.Api.Security;

/// <summary>
/// Uniform accessors for the OpsFlow auth claims carried on the access token.
/// Region scope is a SET: supervisors carry a single "regionId" claim, region-scoped
/// admins carry several. Always read it via <see cref="GetRegionIds"/>.
/// </summary>
internal static class ClaimsPrincipalExtensions
{
    public static string GetRole(this ClaimsPrincipal user) =>
        user.FindFirstValue("role") ?? user.FindFirstValue(ClaimTypes.Role) ?? "";

    public static string GetTenantId(this ClaimsPrincipal user) =>
        user.FindFirstValue("tenantId")!;

    public static string GetUserId(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub")!;

    public static string? GetStoreId(this ClaimsPrincipal user) =>
        user.FindFirstValue("storeId");

    /// <summary>All region ids the user is scoped to (empty for global/store roles).</summary>
    public static IReadOnlyList<string> GetRegionIds(this ClaimsPrincipal user) =>
        user.FindAll("regionId").Select(c => c.Value).ToList();

    /// <summary>
    /// Edge adapter: parse the claims into the pure <see cref="Caller"/> the scope module consumes.
    /// Non-GUID store/region claims are dropped rather than throwing.
    /// </summary>
    public static Caller ToCaller(this ClaimsPrincipal user)
    {
        var storeId = Guid.TryParse(user.GetStoreId(), out var s) ? s : (Guid?)null;
        var regionIds = user.GetRegionIds()
            .Select(r => Guid.TryParse(r, out var g) ? g : (Guid?)null)
            .Where(g => g is not null)
            .Select(g => g!.Value)
            .ToList();
        return new Caller(user.GetRole(), storeId, regionIds);
    }
}
