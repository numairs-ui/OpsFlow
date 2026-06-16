using MediatR;

namespace OpsFlow.Api.Features.Templates.GetTemplate;

internal sealed record GetTemplateQuery(Guid Id) : IRequest<TemplateDetailDto>;

internal sealed record TemplateDetailDto(
    Guid Id, string Name, string? Description, string Category, string Scope,
    Guid? RegionId, string? RegionName, Guid? StoreId, string? StoreName,
    string FieldsJson, bool IsActive, DateTimeOffset CreatedAt);
