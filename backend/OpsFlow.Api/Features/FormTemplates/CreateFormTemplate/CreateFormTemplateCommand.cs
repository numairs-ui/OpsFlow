using FluentValidation;
using MediatR;
using OpsFlow.Domain.Authorization;

namespace OpsFlow.Api.Features.FormTemplates.CreateFormTemplate;

internal sealed record ApprovalStepInput(string Role, int Order);

internal sealed record CreateFormTemplateCommand(
    string Name,
    string? Description,
    string Scope,
    Guid? RegionId,
    Guid? StoreId,
    string PropagationType,
    List<ApprovalStepInput> ApprovalSteps,
    string FieldsJson) : IRequest<Guid>;

internal sealed class CreateFormTemplateValidator : AbstractValidator<CreateFormTemplateCommand>
{
    private static readonly string[] ValidScopes = ["System", "Regional", "Store"];
    private static readonly string[] ValidPropagation = ["Sequential", "Parallel", "NotificationOnly"];
    // Approver roles for form workflow steps — the shared station (store_kiosk) is never an approver.
    private static readonly string[] ValidRoles =
        [Roles.StoreEmployee, Roles.StoreManager, Roles.Supervisor, Roles.Admin, Roles.SuperAdmin];

    public CreateFormTemplateValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Scope).Must(s => ValidScopes.Contains(s))
            .WithMessage("Scope must be System, Regional, or Store.");
        RuleFor(x => x.RegionId).NotNull().When(x => x.Scope == "Regional")
            .WithMessage("RegionId is required for Regional scope.");
        RuleFor(x => x.StoreId).NotNull().When(x => x.Scope == "Store")
            .WithMessage("StoreId is required for Store scope.");
        RuleFor(x => x.PropagationType).Must(p => ValidPropagation.Contains(p))
            .WithMessage("PropagationType must be Sequential, Parallel, or NotificationOnly.");
        RuleFor(x => x.ApprovalSteps).NotEmpty()
            .WithMessage("At least one approval step is required.");
        RuleForEach(x => x.ApprovalSteps).ChildRules(step =>
        {
            step.RuleFor(s => s.Role).Must(r => ValidRoles.Contains(r))
                .WithMessage($"Role must be one of: {string.Join(", ", ValidRoles)}.");
        });
    }
}
