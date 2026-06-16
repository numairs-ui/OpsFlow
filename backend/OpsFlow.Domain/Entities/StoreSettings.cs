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
    public Store? Store { get; init; }
}
