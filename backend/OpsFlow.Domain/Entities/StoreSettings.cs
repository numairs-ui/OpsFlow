namespace OpsFlow.Domain.Entities;

public sealed class StoreSettings
{
    public Guid StoreId { get; init; }
    public string TenantId { get; init; } = default!;
    public decimal? TillABase { get; set; }
    public decimal? TillBBase { get; set; }
    public string DoughNeedTargetsJson { get; set; } = "{}";
    public string TimezoneId { get; set; } = "America/New_York";
    public int OverdueGraceMinutes { get; set; } = 30;
    /// <summary>
    /// Local time of day by which a store must have logged its deposit. Past this time with no
    /// deposit for the day, the deposit-escalation job flags the store. Null → the job's default (21:00).
    /// </summary>
    public TimeOnly? DepositDeadlineLocalTime { get; set; }
    public Store? Store { get; init; }
}
