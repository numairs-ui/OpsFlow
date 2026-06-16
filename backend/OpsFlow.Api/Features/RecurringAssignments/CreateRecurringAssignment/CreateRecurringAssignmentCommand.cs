using FluentValidation;
using MediatR;

namespace OpsFlow.Api.Features.RecurringAssignments.CreateRecurringAssignment;

internal sealed record CreateRecurringAssignmentCommand(
    string Name,
    Guid ChecklistId,
    Guid StoreId,
    string CronExpression,
    DateTimeOffset StartsAt,
    DateTimeOffset? EndsAt
) : IRequest<Guid>;

internal sealed class CreateRecurringAssignmentValidator : AbstractValidator<CreateRecurringAssignmentCommand>
{
    public CreateRecurringAssignmentValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ChecklistId).NotEmpty();
        RuleFor(x => x.StoreId).NotEmpty();
        RuleFor(x => x.CronExpression).NotEmpty().MaximumLength(100);
        RuleFor(x => x.StartsAt).NotEmpty();
        RuleFor(x => x.EndsAt).GreaterThan(x => x.StartsAt).When(x => x.EndsAt.HasValue);
    }
}
