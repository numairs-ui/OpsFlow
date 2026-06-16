namespace OpsFlow.Domain.Entities;

public sealed class RefreshToken
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string UserId { get; init; } = default!;
    public string TokenHash { get; init; } = default!;
    public string UserRole { get; init; } = default!;
    public string? StoreId { get; init; }
    public string? RegionId { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public bool IsUsed { get; set; }
}
