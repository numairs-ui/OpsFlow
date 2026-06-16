using MediatR;

namespace OpsFlow.Api.Features.Auth.Login;

internal sealed record LoginCommand(string Email, string Password, string TenantId) : IRequest<LoginResult>;

internal sealed record LoginResult(
    string AccessToken,
    int ExpiresIn,
    string RawRefreshToken,
    string TenantId,
    DateTimeOffset RefreshExpiresAt);
