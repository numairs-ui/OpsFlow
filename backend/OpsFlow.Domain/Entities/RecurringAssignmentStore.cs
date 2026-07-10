namespace OpsFlow.Domain.Entities;

/// <summary>
/// Join row targeting a recurring assignment at one store. A single assignment can broadcast to many
/// stores; the generator fans out one task instance per target store per firing. Modeled on
/// <see cref="UserStoreAssignment"/>.
/// </summary>
public sealed class RecurringAssignmentStore
{
    public Guid RecurringAssignmentId { get; init; }
    public Guid StoreId { get; init; }

    public RecurringAssignment RecurringAssignment { get; init; } = default!;
    public Store Store { get; init; } = default!;
}
