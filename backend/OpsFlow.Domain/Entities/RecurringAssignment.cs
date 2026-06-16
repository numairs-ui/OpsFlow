namespace OpsFlow.Domain.Entities;

public sealed class RecurringAssignment
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string TenantId { get; init; } = default!;
    public string Name { get; set; } = default!;
    public Guid ChecklistId { get; set; }
    public Guid StoreId { get; set; }

    // Quartz 6-field cron expression (e.g. "0 0 9 ? * MON")
    public string CronExpression { get; set; } = default!;
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
    public bool IsPaused { get; set; } = false;
    public string CreatedByUserId { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public Checklist? Checklist { get; init; }
    public Store? Store { get; init; }
    public ICollection<TaskInstance> TaskInstances { get; init; } = [];
}
