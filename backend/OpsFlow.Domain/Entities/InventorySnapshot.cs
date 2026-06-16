namespace OpsFlow.Domain.Entities;

public sealed class InventorySnapshot
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string TenantId { get; init; } = default!;
    public Guid StoreId { get; init; }
    public DateOnly Date { get; init; }
    public string ItemKey { get; init; } = default!;   // matches field id in template
    public double OnHandCount { get; set; }
    public string? SubmittedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Store? Store { get; init; }
}
