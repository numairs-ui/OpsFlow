using MediatR;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Users.ReactivateUser;

internal sealed class ReactivateUserHandler(TenantDbContextFactory factory)
    : IRequestHandler<ReactivateUserCommand>
{
    public async Task Handle(ReactivateUserCommand cmd, CancellationToken ct)
    {
        await using var db = await factory.CreateAsync(ct);
        var profile = await db.UserProfiles.FindAsync([cmd.UserId], ct)
            ?? throw new KeyNotFoundException($"User {cmd.UserId} not found.");
        profile.IsActive = true;
        await db.SaveChangesAsync(ct);
    }
}
