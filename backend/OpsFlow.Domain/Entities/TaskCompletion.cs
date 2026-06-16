namespace OpsFlow.Domain.Entities;

public sealed class TaskCompletion
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string TenantId { get; init; } = default!;
    public Guid TaskInstanceId { get; init; }
    public string? CompletedByUserId { get; set; }
    public string? CompletedByVolunteerName { get; set; }
    public string FieldValuesJson { get; set; } = "[]";
    public string CorrectiveActionsJson { get; set; } = "[]";
    public DateTimeOffset CompletedAt { get; init; } = DateTimeOffset.UtcNow;
    public TaskInstance? TaskInstance { get; init; }
}
