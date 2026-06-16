using FluentValidation;

namespace OpsFlow.Api.Features.Tasks.CancelTask;

internal sealed class CancelTaskValidator : AbstractValidator<CancelTaskCommand>
{
    public CancelTaskValidator()
    {
        RuleFor(c => c.TaskId).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty().WithMessage("A cancellation reason is required.");
    }
}
