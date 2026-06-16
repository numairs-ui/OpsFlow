using MediatR;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Interfaces;
using OpsFlow.Infrastructure;
using System.Security.Claims;

namespace OpsFlow.Api.Features.Users.CreateUser;

internal sealed class CreateUserHandler(
    IAuthProvider authProvider,
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<CreateUserCommand, string>
{
    public async Task<string> Handle(CreateUserCommand cmd, CancellationToken ct)
    {
        var tenantId = httpContextAccessor.HttpContext!.User.FindFirstValue("tenantId")!;

        var userId = await authProvider.CreateUserAsync(new CreateUserRequest(
            cmd.Email, cmd.Password, cmd.Role, tenantId,
            cmd.StoreId?.ToString(), cmd.RegionId?.ToString()), ct);

        await using var db = await factory.CreateAsync(ct);
        db.UserProfiles.Add(new UserProfile
        {
            UserId = userId,
            Email = cmd.Email,
            DisplayName = cmd.DisplayName,
            Role = cmd.Role,
            StoreId = cmd.StoreId,
            RegionId = cmd.RegionId,
        });
        await db.SaveChangesAsync(ct);
        return userId;
    }
}
