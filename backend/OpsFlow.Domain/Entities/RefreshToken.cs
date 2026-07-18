namespace OpsFlow.Domain.Entities;

public sealed class RefreshToken
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string UserId { get; init; } = default!;
    public string TokenHash { get; init; } = default!;
    public string UserRole { get; init; } = default!;
    public string? StoreId { get; init; }
    /// <summary>Comma-separated region ids for region-scoped roles (admin: many, supervisor: one). Null for global/store roles.</summary>
    public string? RegionIdsCsv { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public bool IsUsed { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
    /// <summary>
    /// The token this one was rotated into on use. Lets a refresh call that loses a rotation race
    /// (its token got used by a concurrent request a moment earlier — e.g. a hard-reload right
    /// after login racing another reload/tab) recover the current session within a short grace
    /// window, instead of being hard-logged-out for a benign, non-malicious replay.
    /// </summary>
    public Guid? ReplacedByTokenId { get; set; }
}
