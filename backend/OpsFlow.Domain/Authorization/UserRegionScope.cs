namespace OpsFlow.Domain.Authorization;

/// <summary>
/// The single place that converts between a user's region set and its stored representations.
/// A region-scoped user keeps its regions as a comma-separated <c>RegionIdsCsv</c> (and, when there
/// is exactly one, mirrors it into the single <c>RegionId</c> FK column for supervisors); the same
/// CSV is what Supabase metadata holds under <c>region_ids</c>. Both directions live here so the
/// encoding never drifts between CreateUser, UpdateUser, and the auth provider.
/// </summary>
public static class UserRegionScope
{
    /// <summary>Region set → the (single-FK, CSV) pair stored on a UserProfile.</summary>
    public static (Guid? RegionId, string? Csv) Encode(IReadOnlyList<Guid> regionIds) =>
    (
        regionIds.Count == 1 ? regionIds[0] : null,
        regionIds.Count > 0 ? string.Join(',', regionIds) : null
    );

    /// <summary>Region set → the comma-separated string stored in Supabase metadata / a token column.</summary>
    public static string? ToCsv(IReadOnlyList<string> regionIds) =>
        regionIds.Count > 0 ? string.Join(',', regionIds) : null;

    /// <summary>
    /// Stored form → region set. Prefers the CSV; falls back to a single legacy value (the
    /// <c>RegionId</c> FK, or the pre-multi-region <c>region_id</c> metadata key).
    /// </summary>
    public static IReadOnlyList<string> Decode(string? csv, string? fallbackSingle)
    {
        if (!string.IsNullOrWhiteSpace(csv))
            return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.IsNullOrWhiteSpace(fallbackSingle) ? [] : [fallbackSingle];
    }
}
