using MediatR;

namespace OpsFlow.Api.Features.TenantSettings.GetTenantSettings;

internal sealed record GetTenantSettingsQuery : IRequest<TenantSettingsDto>;

internal sealed record TenantSettingsDto(
    string Id,
    string Name,
    string? LogoUrl,
    string? PrimaryContactEmail,
    bool IsActive);
