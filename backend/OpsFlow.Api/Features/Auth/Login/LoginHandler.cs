using MediatR;
using OpsFlow.Api.Services;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Interfaces;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Auth.Login;

internal sealed class LoginHandler(
    IAuthProvider authProvider,
    TenantDbContextFactory tenantFactory,
    TokenService tokenService) : IRequestHandler<LoginCommand, LoginResult>
{
    public async Task<LoginResult> Handle(LoginCommand cmd, CancellationToken ct)
    {
        var auth = await authProvider.AuthenticateAsync(cmd.Email, cmd.Password, cmd.TenantId, ct)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        var accessToken = tokenService.MintAccessToken(auth);
        var (raw, hash, expiresAt) = tokenService.GenerateRefreshToken();

        await using var tenantDb = await tenantFactory.CreateForTenantAsync(cmd.TenantId, ct);
        tenantDb.RefreshTokens.Add(new RefreshToken
        {
            UserId = auth.UserId,
            TokenHash = hash,
            UserRole = auth.Role,
            StoreId = auth.StoreId,
            RegionIdsCsv = auth.RegionIds.Count > 0 ? string.Join(',', auth.RegionIds) : null,
            ExpiresAt = expiresAt,
        });
        await tenantDb.SaveChangesAsync(ct);

        return new LoginResult(accessToken, 15 * 60, raw, cmd.TenantId, expiresAt);
    }
}
