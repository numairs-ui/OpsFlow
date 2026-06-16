namespace OpsFlow.Domain.Entities;

public sealed class Store
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string TenantId { get; init; } = default!;
    public Guid RegionId { get; set; }
    public string Name { get; set; } = default!;
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public Region Region { get; init; } = default!;
    public ICollection<UserStoreAssignment> UserStoreAssignments { get; init; } = [];
}
