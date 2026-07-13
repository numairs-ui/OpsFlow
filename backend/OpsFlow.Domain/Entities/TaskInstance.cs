namespace OpsFlow.Domain.Entities;

public sealed class TaskInstance
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string TenantId { get; init; } = default!;

    // null = ad-hoc task created by a manager
    public Guid? RecurringAssignmentId { get; init; }
    // null = standalone task (references a single TaskTemplate directly, or is notes-only)
    public Guid? ChecklistId { get; set; }
    // Standalone mode (a): the task validates/completes against this single template's fields.
    public Guid? AdHocTaskTemplateId { get; set; }
    // Links a corrective task back to the checklist session that spawned it (A4). SetNull on delete.
    public Guid? SourceTaskInstanceId { get; set; }
    public Guid StoreId { get; set; }
    public DateTimeOffset DueAt { get; set; }

    // Pending | InProgress | Completed | Overdue | Verified | Cancelled | Deferred | CorrectiveActionRaised
    public string Status { get; set; } = "Pending";
    public string? AssignedToUserId { get; set; }
    public string? CompletedByUserId { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? Notes { get; set; }

    // Cancellation
    public string? CancelledByUserId { get; set; }
    public string? CancelReason { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }

    // Deferral
    public DateTimeOffset? DeferredTo { get; set; }
    public string? DeferReason { get; set; }
    public string? DeferredByUserId { get; set; }

    // Verification
    public string? VerifiedByUserId { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }

    public string CreatedByUserId { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public RecurringAssignment? RecurringAssignment { get; init; }
    public Checklist? Checklist { get; init; }
    public TaskTemplate? AdHocTaskTemplate { get; init; }
    public Store? Store { get; init; }
    public ICollection<TaskCompletion> Completions { get; init; } = [];
}
