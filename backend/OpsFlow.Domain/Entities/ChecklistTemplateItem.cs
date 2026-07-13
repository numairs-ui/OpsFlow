namespace OpsFlow.Domain.Entities;

public sealed class ChecklistTemplateItem
{
    public Guid ChecklistId { get; init; }
    public Guid TemplateId { get; init; }
    public int Order { get; set; }

    // Scoring (A2). All nullable/defaulted so pre-existing flat checklists stay valid with no scoring.
    // ScoringType: null (unscored) | "PassFail" | "Scale1To5".
    public string? ScoringType { get; set; }
    public decimal Weight { get; set; } = 1.0m;
    public bool PhotoRequired { get; set; }
    public string? FailCorrectiveActionText { get; set; }
    // Only meaningful for Scale1To5: a score at or below this counts as a failure.
    public int? FailScoreThreshold { get; set; }

    public Checklist Checklist { get; init; } = default!;
    public TaskTemplate Template { get; init; } = default!;
}
