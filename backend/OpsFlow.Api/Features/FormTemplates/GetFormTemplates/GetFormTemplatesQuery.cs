using MediatR;

namespace OpsFlow.Api.Features.FormTemplates.GetFormTemplates;

internal sealed record GetFormTemplatesQuery(
    string? Scope = null,
    string? PropagationType = null,
    bool? IsActive = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 20) : IRequest<FormTemplateListResult>;

internal sealed record FormTemplateDto(
    Guid Id, string Name, string? Description, string Scope,
    Guid? RegionId, string? RegionName, Guid? StoreId, string? StoreName,
    string PropagationType, int FieldCount, bool IsActive, DateTimeOffset CreatedAt);

internal sealed record FormTemplateListResult(List<FormTemplateDto> Items, int TotalCount);
