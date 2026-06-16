using FluentValidation;

namespace OpsFlow.Api.Features.Regions.UpdateRegion;

internal sealed class UpdateRegionValidator : AbstractValidator<UpdateRegionCommand>
{
    public UpdateRegionValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
    }
}
