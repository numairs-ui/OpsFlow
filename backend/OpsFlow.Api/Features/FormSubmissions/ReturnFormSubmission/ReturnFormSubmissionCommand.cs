using FluentValidation;
using MediatR;

namespace OpsFlow.Api.Features.FormSubmissions.ReturnFormSubmission;

internal sealed record ReturnFormSubmissionCommand(Guid Id, string Comments) : IRequest;

internal sealed class ReturnFormSubmissionValidator : AbstractValidator<ReturnFormSubmissionCommand>
{
    public ReturnFormSubmissionValidator()
    {
        RuleFor(x => x.Comments).NotEmpty().WithMessage("Comments are required when returning a submission.");
    }
}
