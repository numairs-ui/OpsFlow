using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Users.DeactivateUser;

internal sealed class DeactivateUserHandler(TenantDbContextFactory factory)
    : IRequestHandler<DeactivateUserCommand>
{
    public async Task Handle(DeactivateUserCommand cmd, CancellationToken ct)
    {
        await using var db = await factory.CreateAsync(ct);
        var profile = await db.UserProfiles.FindAsync([cmd.UserId], ct)
            ?? throw new KeyNotFoundException($"User {cmd.UserId} not found.");

        profile.IsActive = false;

        // Invalidate all refresh tokens for this user
        var tokens = await db.RefreshTokens
            .Where(t => t.UserId == cmd.UserId && !t.IsUsed)
            .ToListAsync(ct);
        foreach (var t in tokens) t.IsUsed = true;

        await db.SaveChangesAsync(ct);
    }
}
