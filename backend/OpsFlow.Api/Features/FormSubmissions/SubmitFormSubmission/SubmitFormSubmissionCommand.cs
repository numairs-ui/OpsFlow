using MediatR;

namespace OpsFlow.Api.Features.FormSubmissions.SubmitFormSubmission;

internal sealed record SubmitFormSubmissionCommand(
    Guid Id,
    Dictionary<string, string>? FieldValues) : IRequest;
