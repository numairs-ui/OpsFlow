namespace OpsFlow.Domain.Entities;

public sealed class FormSubmission
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string TenantId { get; init; } = default!;
    public Guid? FormTemplateId { get; set; }
    public Guid StoreId { get; set; }
    public string SubmittedByUserId { get; set; } = default!;

    // Draft | Submitted | PendingApproval | Returned | Rejected | Approved | Recorded
    public string Status { get; set; } = "Draft";
    public int? CurrentStepOrder { get; set; }
    public string FieldValuesJson { get; set; } = "{}";

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SubmittedAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }

    public FormTemplate? FormTemplate { get; init; }
    public Store? Store { get; init; }
    public ICollection<FormSubmissionApprovalStep> ApprovalSteps { get; init; } = [];
}
