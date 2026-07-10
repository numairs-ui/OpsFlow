namespace OpsFlow.Domain.Entities;

public sealed class RecurringAssignment
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string TenantId { get; init; } = default!;
    public string Name { get; set; } = default!;
    public Guid ChecklistId { get; set; }

    // Quartz 6-field cron expression (e.g. "0 0 9 ? * MON")
    public string CronExpression { get; set; } = default!;
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
    public bool IsPaused { get; set; } = false;
    public string CreatedByUserId { get; set; } = default!;
    public string? AssignedToUserId { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public Checklist? Checklist { get; init; }
    /// <summary>The stores this assignment broadcasts to (many-to-many via the join entity).</summary>
    public ICollection<RecurringAssignmentStore> TargetStores { get; init; } = [];
    public ICollection<TaskInstance> TaskInstances { get; init; } = [];
}
