using FluentValidation;

namespace OpsFlow.Api.Features.Tasks.CompleteTask;

internal sealed class CompleteTaskValidator : AbstractValidator<CompleteTaskCommand>
{
    public CompleteTaskValidator()
    {
        RuleFor(c => c.TaskId).NotEmpty();
        RuleFor(c => c.FieldValues).NotNull();
    }
}
