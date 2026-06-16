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
    string? FieldsJson);

internal sealed record ImportTemplatesCommand(List<ImportTemplateItem> Templates) : IRequest<ImportTemplatesResult>;

internal sealed record ImportTemplatesResult(
    int Created,
    List<ImportFailure> Failed);

internal sealed record ImportFailure(int Index, List<string> Errors);
