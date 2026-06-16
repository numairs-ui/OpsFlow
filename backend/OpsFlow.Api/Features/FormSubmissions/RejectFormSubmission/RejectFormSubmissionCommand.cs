using FluentValidation;
using MediatR;

namespace OpsFlow.Api.Features.FormSubmissions.RejectFormSubmission;

internal sealed record RejectFormSubmissionCommand(Guid Id, string Reason) : IRequest;

internal sealed class RejectFormSubmissionValidator : AbstractValidator<RejectFormSubmissionCommand>
{
    public RejectFormSubmissionValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().WithMessage("A rejection reason is required.");
    }
}
