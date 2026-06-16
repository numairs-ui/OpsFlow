namespace OpsFlow.Domain.Entities;

public sealed class FormSubmissionApprovalStep
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid SubmissionId { get; init; }
    public int StepOrder { get; set; }
    public string Role { get; set; } = default!;
    public string? ActionByUserId { get; set; }

    // Pending | Approved | Rejected | Returned | Recorded | AutoClosed
    public string Action { get; set; } = "Pending";
    public string? Comments { get; set; }
    public DateTimeOffset? ActionAt { get; set; }

    public FormSubmission? FormSubmission { get; init; }
}
