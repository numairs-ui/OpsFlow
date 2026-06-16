using FluentValidation;

namespace OpsFlow.Api.Features.Tasks.DeferTask;

internal sealed class DeferTaskValidator : AbstractValidator<DeferTaskCommand>
{
    public DeferTaskValidator()
    {
        RuleFor(c => c.TaskId).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty().WithMessage("A deferral reason is required.");
        RuleFor(c => c.DeferredTo)
            .GreaterThan(DateTimeOffset.UtcNow.Date.AddDays(1).AddTicks(-1))
            .WithMessage("DeferredTo must be after today.");
    }
}
