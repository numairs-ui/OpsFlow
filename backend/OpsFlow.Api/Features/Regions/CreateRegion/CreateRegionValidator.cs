using FluentValidation;

namespace OpsFlow.Api.Features.Regions.CreateRegion;

internal sealed class CreateRegionValidator : AbstractValidator<CreateRegionCommand>
{
    public CreateRegionValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
    }
}
