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
    IAuthProvider authProvider,
    IHttpContextAccessor httpContextAccessor,
    ILogger<RefreshHandler> logger) : IRequestHandler<RefreshCommand, RefreshResult>
{
    // A rotated-away token presented again within this window is treated as a benign race
    // (two near-simultaneous requests both carrying the pre-rotation cookie — e.g. a hard
    // page reload right after login racing another reload/tab, both reading the cookie jar
    // before either's Set-Cookie has landed) rather than a stolen/replayed token. Anything
    // reused after this window still hard-fails, same as before.
    private static readonly TimeSpan ReuseGracePeriod = TimeSpan.FromSeconds(15);

    public async Task<RefreshResult> Handle(RefreshCommand _, CancellationToken ct)
    {
        var ctx = httpContextAccessor.HttpContext
            ?? throw new UnauthorizedAccessException("No HTTP context.");

        var parsed = tokenService.ParseRefreshCookie(ctx.Request)
            ?? throw new UnauthorizedAccessException("Refresh token cookie missing or malformed.");

        var (tenantId, rawToken) = parsed;
        var hash = tokenService.HashToken(rawToken);

        await using var tenantDb = await tenantFactory.CreateForTenantAsync(tenantId, ct);

        var stored = await tenantDb.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == hash, ct)
            ?? throw new UnauthorizedAccessException("Invalid or already-used refresh token.");

        if (stored.IsUsed)
        {
            var withinGrace = stored.UsedAt is { } usedAt && DateTimeOffset.UtcNow - usedAt < ReuseGracePeriod;
            if (!withinGrace)
                throw new UnauthorizedAccessException("Invalid or already-used refresh token.");

            logger.LogInformation(
                "Refresh token for user {UserId} reused {Ms}ms after rotation — within grace period, recovering current session instead of logging out.",
                stored.UserId, (DateTimeOffset.UtcNow - stored.UsedAt!.Value).TotalMilliseconds);

            var current = await FollowToCurrentAsync(tenantDb, stored, ct)
                ?? throw new UnauthorizedAccessException("Invalid or already-used refresh token.");

            return await MintAsync(tenantId, current, ct);
        }

        if (stored.ExpiresAt < DateTimeOffset.UtcNow)
            throw new UnauthorizedAccessException("Refresh token expired.");

        // Rotate — mark old as used, link it to its successor for grace-period recovery.
        stored.IsUsed = true;
        stored.UsedAt = DateTimeOffset.UtcNow;

        var (newRaw, newHash, newExpiresAt) = tokenService.GenerateRefreshToken();
        var successor = new RefreshToken
        {
            UserId = stored.UserId,
            TokenHash = newHash,
            UserRole = stored.UserRole,
            StoreId = stored.StoreId,
            RegionIdsCsv = stored.RegionIdsCsv,
            ExpiresAt = newExpiresAt,
        };
        tenantDb.RefreshTokens.Add(successor);
        stored.ReplacedByTokenId = successor.Id;
        await tenantDb.SaveChangesAsync(ct);

        tokenService.SetRefreshCookie(ctx.Response, tenantId, newRaw, newExpiresAt);

        return await MintAsync(tenantId, stored, ct);
    }

    // A grace-period reuse can itself have already been rotated again by the request that won
    // the original race — walk the chain to whichever token is current (unused, unexpired).
    private static async Task<RefreshToken?> FollowToCurrentAsync(
        OpsFlow.Infrastructure.TenantDbContext db, RefreshToken token, CancellationToken ct)
    {
        var current = token;
        for (var hops = 0; hops < 5 && current.IsUsed; hops++)
        {
            if (current.ReplacedByTokenId is not { } nextId) return null;
            var next = await db.RefreshTokens.FindAsync([nextId], ct);
            if (next is null) return null;
            current = next;
        }
        return current.IsUsed || current.ExpiresAt < DateTimeOffset.UtcNow ? null : current;
    }

    private async Task<RefreshResult> MintAsync(string tenantId, RefreshToken token, CancellationToken ct)
    {
        var regionIds = OpsFlow.Domain.Authorization.UserRegionScope.Decode(token.RegionIdsCsv, null);
        var email = await authProvider.GetEmailAsync(token.UserId, ct);
        var accessToken = tokenService.MintAccessToken(new AuthResult(
            token.UserId, tenantId, token.UserRole, token.StoreId, regionIds), email);

        return new RefreshResult(accessToken, 15 * 60);
    }
}
