namespace OpsFlow.Domain.Entities;

public sealed class ChecklistTemplateItem
{
    public Guid ChecklistId { get; init; }
    public Guid TemplateId { get; init; }
    public int Order { get; set; }

    public Checklist Checklist { get; init; } = default!;
    public TaskTemplate Template { get; init; } = default!;
}
