using MediatR;

namespace OpsFlow.Api.Features.FormSubmissions.GetFormSubmissions;

internal sealed record GetFormSubmissionsQuery(
    Guid? StoreId = null,
    Guid? RegionId = null,
    string? Status = null) : IRequest<List<FormSubmissionSummaryDto>>;

internal sealed record FormSubmissionSummaryDto(
    Guid Id,
    Guid? FormTemplateId,
    string? FormTemplateName,
    Guid StoreId,
    string? StoreName,
    string SubmittedByUserId,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? ResolvedAt);
