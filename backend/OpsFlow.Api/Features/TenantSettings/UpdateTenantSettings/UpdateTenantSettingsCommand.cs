using MediatR;

namespace OpsFlow.Api.Features.TenantSettings.UpdateTenantSettings;

internal sealed record UpdateTenantSettingsCommand(
    string Name,
    string? LogoUrl,
    string? PrimaryContactEmail) : IRequest;
