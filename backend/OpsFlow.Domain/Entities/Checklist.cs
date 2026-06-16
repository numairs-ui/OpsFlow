namespace OpsFlow.Domain.Entities;

public sealed class Checklist
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string TenantId { get; init; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string Scope { get; set; } = default!; // System | Regional | Store
    public Guid? RegionId { get; set; }
    public Guid? StoreId { get; set; }
    public bool IsActive { get; set; } = true;
    public string CreatedByUserId { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public Region? Region { get; init; }
    public Store? Store { get; init; }
    public ICollection<ChecklistTemplateItem> Items { get; init; } = [];
}
