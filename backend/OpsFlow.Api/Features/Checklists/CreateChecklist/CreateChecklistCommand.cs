using FluentValidation;
using MediatR;

namespace OpsFlow.Api.Features.Checklists.CreateChecklist;

internal sealed record CreateChecklistCommand(
    string Name,
    string? Description,
    string Scope,
    Guid? RegionId,
    Guid? StoreId,
    List<ChecklistItemInput> Items) : IRequest<Guid>;

internal sealed record ChecklistItemInput(Guid TemplateId, int Order);

internal sealed class CreateChecklistValidator : AbstractValidator<CreateChecklistCommand>
{
    private static readonly string[] ValidScopes = ["System", "Regional", "Store"];

    public CreateChecklistValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Scope).Must(s => ValidScopes.Contains(s))
            .WithMessage("Scope must be System, Regional, or Store.");
        RuleFor(x => x.RegionId).NotNull().When(x => x.Scope == "Regional")
            .WithMessage("RegionId is required for Regional scope.");
        RuleFor(x => x.StoreId).NotNull().When(x => x.Scope == "Store")
            .WithMessage("StoreId is required for Store scope.");
    }
}
