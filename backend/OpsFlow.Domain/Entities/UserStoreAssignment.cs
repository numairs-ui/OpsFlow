namespace OpsFlow.Domain.Entities;

public sealed class UserStoreAssignment
{
    public string UserId { get; init; } = default!;
    public Guid StoreId { get; init; }
    public string AssignedByAdminId { get; init; } = default!;
    public DateTimeOffset AssignedAt { get; init; } = DateTimeOffset.UtcNow;

    public Store Store { get; init; } = default!;
    public UserProfile User { get; init; } = default!;
}
