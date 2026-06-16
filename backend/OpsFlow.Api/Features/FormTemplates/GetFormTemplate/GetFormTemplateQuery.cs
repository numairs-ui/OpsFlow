using MediatR;

namespace OpsFlow.Api.Features.FormTemplates.GetFormTemplate;

internal sealed record GetFormTemplateQuery(Guid Id) : IRequest<FormTemplateDetailDto>;

internal sealed record FormTemplateDetailDto(
    Guid Id, string Name, string? Description, string Scope,
    Guid? RegionId, string? RegionName, Guid? StoreId, string? StoreName,
    string PropagationType, string ApprovalStepsJson, string FieldsJson,
    bool IsActive, DateTimeOffset CreatedAt);
