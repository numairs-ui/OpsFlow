namespace OpsFlow.Domain.Entities;

public sealed class UserProfile
{
    public string UserId { get; init; } = default!;
    public string Email { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string Role { get; set; } = default!;
    public Guid? StoreId { get; set; }
    public Guid? RegionId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; } = true;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public Store? Store { get; init; }
    public Region? Region { get; init; }
    public ICollection<UserStoreAssignment> StoreAssignments { get; init; } = [];
}
