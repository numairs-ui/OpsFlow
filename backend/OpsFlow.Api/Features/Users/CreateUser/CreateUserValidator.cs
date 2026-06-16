using FluentValidation;

namespace OpsFlow.Api.Features.Users.CreateUser;

internal sealed class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    private static readonly string[] ValidRoles = ["store_employee", "store_manager", "supervisor", "admin"];

    public CreateUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Role).NotEmpty().Must(r => ValidRoles.Contains(r))
            .WithMessage("Role must be one of: store_employee, store_manager, supervisor, admin.");
        RuleFor(x => x.StoreId).NotNull()
            .When(x => x.Role == "store_employee" || x.Role == "store_manager")
            .WithMessage("StoreId is required for store_employee and store_manager.");
        RuleFor(x => x.RegionId).NotNull()
            .When(x => x.Role == "supervisor")
            .WithMessage("RegionId is required for supervisor.");
    }
}
