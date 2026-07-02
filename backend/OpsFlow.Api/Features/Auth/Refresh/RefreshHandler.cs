using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Services;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Interfaces;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Auth.Refresh;

internal sealed class RefreshHandler(
    TenantDbContextFactory tenantFactory,
    TokenService tokenService,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<RefreshCommand, RefreshResult>
{
    public async Task<RefreshResult> Handle(RefreshCommand _, CancellationToken ct)
    {
        var ctx = httpContextAccessor.HttpContext
            ?? throw new UnauthorizedAccessException("No HTTP context.");

        var parsed = tokenService.ParseRefreshCookie(ctx.Request)
            ?? throw new UnauthorizedAccessException("Refresh token cookie missing or malformed.");

        var (tenantId, rawToken) = parsed;
        var hash = tokenService.HashToken(rawToken);

        await using var tenantDb = await tenantFactory.CreateForTenantAsync(tenantId, ct);

        var stored = await tenantDb.RefreshTokens
            .FirstOrDefaultAsync(r => r.TokenHash == hash && !r.IsUsed, ct)
            ?? throw new UnauthorizedAccessException("Invalid or already-used refresh token.");

        if (stored.ExpiresAt < DateTimeOffset.UtcNow)
            throw new UnauthorizedAccessException("Refresh token expired.");

        // Rotate — mark old as used
        stored.IsUsed = true;

        // Issue new refresh token
        var (newRaw, newHash, newExpiresAt) = tokenService.GenerateRefreshToken();
        tenantDb.RefreshTokens.Add(new RefreshToken
        {
            UserId = stored.UserId,
            TokenHash = newHash,
            UserRole = stored.UserRole,
            StoreId = stored.StoreId,
            RegionIdsCsv = stored.RegionIdsCsv,
            ExpiresAt = newExpiresAt,
        });
        await tenantDb.SaveChangesAsync(ct);

        tokenService.SetRefreshCookie(ctx.Response, tenantId, newRaw, newExpiresAt);

        var regionIds = OpsFlow.Domain.Authorization.UserRegionScope.Decode(stored.RegionIdsCsv, null);
        var accessToken = tokenService.MintAccessToken(new AuthResult(
            stored.UserId, tenantId, stored.UserRole, stored.StoreId, regionIds));

        return new RefreshResult(accessToken, 15 * 60);
    }
}
