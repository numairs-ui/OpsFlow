namespace OpsFlow.Domain.Entities;

/// <summary>
/// Persisted escalation flag: a store had not logged its deposit by its deadline for a given
/// business day. Written by the deposit-escalation job and surfaced (unchanged) on the existing
/// region/system dashboards. One row per store per business date.
/// </summary>
public sealed class MissedDepositFlag
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string TenantId { get; init; } = default!;
    public Guid StoreId { get; init; }
    /// <summary>The UTC "today" date the flag applies to (matches the dashboards' DashboardWindow).</summary>
    public DateOnly BusinessDate { get; init; }
    public DateTimeOffset FlaggedAt { get; init; } = DateTimeOffset.UtcNow;

    public Store? Store { get; init; }
}
