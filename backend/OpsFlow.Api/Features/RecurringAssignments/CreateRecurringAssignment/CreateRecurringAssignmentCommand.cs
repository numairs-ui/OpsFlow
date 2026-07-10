using FluentValidation;
using MediatR;

namespace OpsFlow.Api.Features.RecurringAssignments.CreateRecurringAssignment;

internal sealed record CreateRecurringAssignmentCommand(
    string Name,
    Guid ChecklistId,
    IReadOnlyList<Guid> TargetStoreIds,
    string CronExpression,
    DateTimeOffset StartsAt,
    DateTimeOffset? EndsAt,
    string? AssignedToUserId
) : IRequest<Guid>;

internal sealed class CreateRecurringAssignmentValidator : AbstractValidator<CreateRecurringAssignmentCommand>
{
    public CreateRecurringAssignmentValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ChecklistId).NotEmpty();
        RuleFor(x => x.TargetStoreIds).NotEmpty().WithMessage("At least one target store is required.");
        RuleFor(x => x.TargetStoreIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .When(x => x.TargetStoreIds is { Count: > 0 })
            .WithMessage("Target stores must be distinct.");
        RuleFor(x => x.CronExpression).NotEmpty().MaximumLength(100);
        RuleFor(x => x.StartsAt).NotEmpty();
        RuleFor(x => x.EndsAt).GreaterThan(x => x.StartsAt).When(x => x.EndsAt.HasValue);

        // A specific employee assignee only makes sense for a single store — reject it when broadcasting.
        RuleFor(x => x.AssignedToUserId)
            .Empty()
            .When(x => x.TargetStoreIds is { Count: > 1 })
            .WithMessage("A specific assignee cannot be set when broadcasting to more than one store.");
    }
}
