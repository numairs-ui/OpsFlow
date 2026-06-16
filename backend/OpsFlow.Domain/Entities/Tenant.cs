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
}
