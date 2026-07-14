using MediatR;

namespace OpsFlow.Api.Features.Templates.ImportTemplates;

internal sealed record ImportTemplateItem(
    string Type,
    string Name,
    string? Description,
    string Category,
    string Scope,
    Guid? RegionId,
    Guid? StoreId,
    string? FieldsJson,
    // For Type == "Checklist": each sub-item becomes a TaskTemplate + a scored ChecklistTemplateItem.
    List<ImportChecklistItem>? Items = null);

internal sealed record ImportChecklistItem(
    string Name,
    string? Description,
    string? Category,
    string? FieldsJson,
    int Order = 0,
    string? ScoringType = null,
    decimal Weight = 1.0m,
    bool PhotoRequired = false,
    string? FailCorrectiveActionText = null,
    int? FailScoreThreshold = null);

internal sealed record ImportTemplatesCommand(List<ImportTemplateItem> Templates) : IRequest<ImportTemplatesResult>;

internal sealed record ImportTemplatesResult(
    int Created,
    List<ImportFailure> Failed);

internal sealed record ImportFailure(int Index, List<string> Errors);
