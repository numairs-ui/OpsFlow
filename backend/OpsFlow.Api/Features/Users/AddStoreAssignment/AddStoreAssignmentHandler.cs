using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Domain.Entities;
using OpsFlow.Infrastructure;
using System.Security.Claims;

namespace OpsFlow.Api.Features.Users.AddStoreAssignment;

internal sealed class AddStoreAssignmentHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<AddStoreAssignmentCommand>
{
    public async Task Handle(AddStoreAssignmentCommand cmd, CancellationToken ct)
    {
        var adminId = httpContextAccessor.HttpContext!.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?? httpContextAccessor.HttpContext.User.FindFirstValue("sub")!;

        await using var db = await factory.CreateAsync(ct);

        var profile = await db.UserProfiles.FindAsync([cmd.UserId], ct)
            ?? throw new KeyNotFoundException($"User {cmd.UserId} not found.");
        if (profile.Role != "store_manager")
            throw new InvalidOperationException("Only store_manager users can have multiple store assignments.");

        var exists = await db.UserStoreAssignments.AnyAsync(a => a.UserId == cmd.UserId && a.StoreId == cmd.StoreId, ct);
        if (exists) return; // idempotent

        db.UserStoreAssignments.Add(new UserStoreAssignment
        {
            UserId = cmd.UserId,
            StoreId = cmd.StoreId,
            AssignedByAdminId = adminId,
        });
        await db.SaveChangesAsync(ct);
    }
}
