using MediatR;

namespace OpsFlow.Api.Features.FormSubmissions.CreateFormSubmission;

internal sealed record CreateFormSubmissionCommand(
    Guid? FormTemplateId,
    Guid StoreId,
    Dictionary<string, string> FieldValues) : IRequest<Guid>;
