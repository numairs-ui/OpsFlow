using MediatR;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Users.UpdateUser;

internal sealed class UpdateUserHandler(TenantDbContextFactory factory)
    : IRequestHandler<UpdateUserCommand>
{
    public async Task Handle(UpdateUserCommand cmd, CancellationToken ct)
    {
        await using var db = await factory.CreateAsync(ct);
        var profile = await db.UserProfiles.FindAsync([cmd.UserId], ct)
            ?? throw new KeyNotFoundException($"User {cmd.UserId} not found.");

        profile.DisplayName = cmd.DisplayName;
        profile.Role = cmd.Role;
        profile.StoreId = cmd.StoreId;
        profile.RegionId = cmd.RegionId;
        await db.SaveChangesAsync(ct);
    }
}
