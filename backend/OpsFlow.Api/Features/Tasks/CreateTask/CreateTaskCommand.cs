using FluentValidation;
using MediatR;

namespace OpsFlow.Api.Features.Tasks.CreateTask;

internal sealed record CreateTaskCommand(
    Guid ChecklistId,
    Guid StoreId,
    DateTimeOffset DueAt,
    string? Notes
) : IRequest<Guid>;

internal sealed class CreateTaskValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskValidator()
    {
        RuleFor(x => x.ChecklistId).NotEmpty();
        RuleFor(x => x.StoreId).NotEmpty();
        RuleFor(x => x.DueAt).NotEmpty();
    }
}
