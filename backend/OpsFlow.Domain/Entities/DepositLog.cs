namespace OpsFlow.Domain.Entities;

public sealed class DepositLog
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string TenantId { get; init; } = default!;
    public Guid StoreId { get; init; }
    public decimal Amount { get; init; }
    public string SubmittedByManagerId { get; init; } = default!;
    public DateTimeOffset SubmittedAt { get; init; } = DateTimeOffset.UtcNow;

    public Store? Store { get; init; }
}
