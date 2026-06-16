using MediatR;

namespace OpsFlow.Api.Features.FormSubmissions.GetFormSubmission;

internal sealed record GetFormSubmissionQuery(Guid Id) : IRequest<FormSubmissionDetailDto>;

internal sealed record ApprovalStepDto(
    int StepOrder, string Role, string? ActionByUserId,
    string Action, string? Comments, DateTimeOffset? ActionAt);

internal sealed record FormSubmissionDetailDto(
    Guid Id,
    Guid? FormTemplateId,
    string? FormTemplateName,
    string? FormTemplateFieldsJson,
    Guid StoreId,
    string? StoreName,
    string SubmittedByUserId,
    string Status,
    int? CurrentStepOrder,
    string FieldValuesJson,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? ResolvedAt,
    List<ApprovalStepDto> ApprovalSteps);
