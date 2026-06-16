using FluentValidation;
using MediatR;

namespace OpsFlow.Api.Features.Templates.CreateTemplate;

internal sealed record CreateTemplateCommand(
    string Name,
    string? Description,
    string Category,
    string Scope,
    Guid? RegionId,
    Guid? StoreId,
    string FieldsJson) : IRequest<Guid>;

internal sealed class CreateTemplateValidator : AbstractValidator<CreateTemplateCommand>
{
    private static readonly string[] ValidScopes = ["System", "Regional", "Store"];

    public CreateTemplateValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Scope).Must(s => ValidScopes.Contains(s))
            .WithMessage("Scope must be System, Regional, or Store.");
        RuleFor(x => x.RegionId).NotNull().When(x => x.Scope == "Regional")
            .WithMessage("RegionId is required for Regional scope.");
        RuleFor(x => x.StoreId).NotNull().When(x => x.Scope == "Store")
            .WithMessage("StoreId is required for Store scope.");
    }
}
