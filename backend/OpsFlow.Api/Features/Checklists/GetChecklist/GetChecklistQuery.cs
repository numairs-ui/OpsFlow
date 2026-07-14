using MediatR;

namespace OpsFlow.Api.Features.Checklists.GetChecklist;

internal sealed record GetChecklistQuery(Guid Id) : IRequest<ChecklistDetailDto>;

internal sealed record ChecklistItemDto(
    Guid TemplateId, string TemplateName, int Order, string FieldsJson,
    string? ScoringType, decimal Weight, bool PhotoRequired,
    string? FailCorrectiveActionText, int? FailScoreThreshold);

internal sealed record ChecklistDetailDto(
    Guid Id, string Name, string? Description, string Scope,
    Guid? RegionId, string? RegionName, Guid? StoreId, string? StoreName,
    List<ChecklistItemDto> Items, bool IsActive, DateTimeOffset CreatedAt);
