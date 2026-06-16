using MediatR;

namespace OpsFlow.Api.Features.FormSubmissions.GetPendingReview;

internal sealed record GetPendingReviewQuery : IRequest<List<PendingReviewDto>>;

internal sealed record PendingReviewDto(
    Guid Id,
    Guid? FormTemplateId,
    string? FormTemplateName,
    Guid StoreId,
    string? StoreName,
    string SubmittedByUserId,
    string Status,
    int StepOrder,
    DateTimeOffset? SubmittedAt);
