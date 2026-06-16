using FluentValidation;

namespace OpsFlow.Api.Features.Stores.UpdateStore;

internal sealed class UpdateStoreValidator : AbstractValidator<UpdateStoreCommand>
{
    public UpdateStoreValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.RegionId).NotEmpty();
        RuleFor(x => x.Address).MaximumLength(300).When(x => x.Address is not null);
    }
}
