using MediatR;

namespace OpsFlow.Api.Features.FormSubmissions.GetMySubmissions;

internal sealed record GetMySubmissionsQuery(string? Status = null) : IRequest<List<MySubmissionDto>>;

internal sealed record MySubmissionDto(
    Guid Id,
    Guid? FormTemplateId,
    string? FormTemplateName,
    Guid StoreId,
    string Status,
    int? CurrentStepOrder,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? ResolvedAt);
