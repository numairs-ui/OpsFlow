namespace OpsFlow.Domain.Entities;

public sealed class Tenant
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public required string Name { get; set; }
    public required string ConnectionString { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    // Phase 2 additions (TB-71)
    public string? LogoUrl { get; set; }
    public string? PrimaryContactEmail { get; set; }

    // Org-wide defaults applied to newly created stores (null → fall back to the code literal).
    public string? DefaultTimezoneId { get; set; }
    public int? DefaultOverdueGraceMinutes { get; set; }
    public TimeOnly? DefaultDepositDeadlineLocalTime { get; set; }
    public decimal? DefaultTillABase { get; set; }
    public decimal? DefaultTillBBase { get; set; }
    public string? DefaultDoughNeedTargetsJson { get; set; }

    // Org display conventions honored app-wide (null → app default en-US / USD).
    public string? LocaleCode { get; set; }
    public string? CurrencyCode { get; set; }
}
