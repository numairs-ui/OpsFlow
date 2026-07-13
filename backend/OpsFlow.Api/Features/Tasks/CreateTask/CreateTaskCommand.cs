using FluentValidation;
using MediatR;

namespace OpsFlow.Api.Features.Tasks.CreateTask;

internal sealed record CreateTaskCommand(
    // Three creation modes, distinguished by which of these is set:
    //  - ChecklistId set    → checklist-backed task (the original behavior / a scored session)
    //  - TaskTemplateId set → standalone task against a single template's fields
    //  - both null          → notes-only task (bare notes + optional photo)
    Guid? ChecklistId,
    Guid StoreId,
    DateTimeOffset DueAt,
    string? Notes,
    Guid? TaskTemplateId = null,
    string? AssignedToUserId = null
) : IRequest<Guid>;

internal sealed class CreateTaskValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskValidator()
    {
        RuleFor(x => x.StoreId).NotEmpty();
        RuleFor(x => x.DueAt).NotEmpty();

        // At most one structured source may be supplied; both unset is a valid notes-only task.
        RuleFor(x => x)
            .Must(x => !(x.ChecklistId.HasValue && x.TaskTemplateId.HasValue))
            .WithMessage("A task may reference either a checklist or a single template, not both.")
            .WithName(nameof(CreateTaskCommand.ChecklistId));
    }
}
