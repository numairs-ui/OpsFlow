using MediatR;

namespace OpsFlow.Api.Features.Templates.GetTemplates;

internal sealed record GetTemplatesQuery(
    string? Scope = null,
    string? Category = null,
    bool? IsActive = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 20) : IRequest<TemplateListResult>;

internal sealed record TemplateDto(
    Guid Id, string Name, string? Description, string Category, string Scope,
    Guid? RegionId, string? RegionName, Guid? StoreId, string? StoreName,
    int FieldCount, bool IsActive, DateTimeOffset CreatedAt);

internal sealed record TemplateListResult(List<TemplateDto> Items, int TotalCount);
