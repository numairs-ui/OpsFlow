namespace OpsFlow.Domain.Authorization;

/// <summary>
/// The authenticated principal's authorization-relevant facts, parsed once at the edge from claims.
/// Built via the API's ClaimsPrincipal.ToCaller() adapter; the scope rules never touch HTTP or JWT.
/// </summary>
public sealed record Caller(string Role, Guid? StoreId, IReadOnlyList<Guid> RegionIds)
{
    /// <summary>The caller's reach, used by the scope filters and assertions.</summary>
    public ScopeSpec Scope() => ScopeSpec.From(this);
}
