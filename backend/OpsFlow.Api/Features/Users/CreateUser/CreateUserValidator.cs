using FluentValidation;
using OpsFlow.Domain.Authorization;

namespace OpsFlow.Api.Features.Users.CreateUser;

internal sealed class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Role).NotEmpty().Must(r => Roles.All.Contains(r))
            .WithMessage($"Role must be one of: {string.Join(", ", Roles.All)}.");

        // Store-scoped roles (manager, employee, kiosk) need a store.
        RuleFor(x => x.StoreId).NotNull()
            .When(x => Roles.IsStoreScoped(x.Role))
            .WithMessage("StoreId is required for store_manager, store_employee, and store_kiosk.");

        // Region-scoped roles need at least one region. supervisor = exactly one; admin = one or more.
        RuleFor(x => x.RegionIds).NotEmpty()
            .When(x => x.Role is Roles.Supervisor or Roles.Admin)
            .WithMessage("At least one region is required for supervisor and admin.");
        RuleFor(x => x.RegionIds).Must(r => r is { Count: 1 })
            .When(x => x.Role == Roles.Supervisor)
            .WithMessage("A supervisor must be assigned exactly one region.");
    }
}
