using FluentValidation;

namespace OpsFlow.Api.Features.StoreSettings.UpdateStoreSettings;

internal sealed class UpdateStoreSettingsValidator : AbstractValidator<UpdateStoreSettingsCommand>
{
    public UpdateStoreSettingsValidator()
    {
        RuleFor(c => c.StoreId).NotEmpty();
        RuleFor(c => c.TimezoneId).NotEmpty();
        RuleFor(c => c.OverdueGraceMinutes).InclusiveBetween(0, 480);
    }
}
