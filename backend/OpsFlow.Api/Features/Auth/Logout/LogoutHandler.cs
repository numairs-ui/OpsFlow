using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Services;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Auth.Logout;

internal sealed class LogoutHandler(
    TenantDbContextFactory tenantFactory,
    TokenService tokenService,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand _, CancellationToken ct)
    {
        var ctx = httpContextAccessor.HttpContext
            ?? throw new UnauthorizedAccessException("No HTTP context.");

        var parsed = tokenService.ParseRefreshCookie(ctx.Request);
        if (parsed is null)
        {
            tokenService.ClearRefreshCookie(ctx.Response);
            return;
        }

        var (tenantId, rawToken) = parsed.Value;
        var hash = tokenService.HashToken(rawToken);

        await using var tenantDb = await tenantFactory.CreateForTenantAsync(tenantId, ct);
        var stored = await tenantDb.RefreshTokens
            .FirstOrDefaultAsync(r => r.TokenHash == hash && !r.IsUsed, ct);

        if (stored is not null)
        {
            stored.IsUsed = true;
            await tenantDb.SaveChangesAsync(ct);
        }

        tokenService.ClearRefreshCookie(ctx.Response);
    }
}
