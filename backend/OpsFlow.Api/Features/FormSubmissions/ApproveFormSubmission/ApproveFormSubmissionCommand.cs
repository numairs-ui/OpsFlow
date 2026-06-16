using MediatR;

namespace OpsFlow.Api.Features.FormSubmissions.ApproveFormSubmission;

internal sealed record ApproveFormSubmissionCommand(Guid Id) : IRequest;
