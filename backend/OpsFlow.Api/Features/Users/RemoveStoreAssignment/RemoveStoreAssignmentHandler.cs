using MediatR;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Users.RemoveStoreAssignment;

internal sealed class RemoveStoreAssignmentHandler(TenantDbContextFactory factory)
    : IRequestHandler<RemoveStoreAssignmentCommand>
{
    public async Task Handle(RemoveStoreAssignmentCommand cmd, CancellationToken ct)
    {
        await using var db = await factory.CreateAsync(ct);
        var assignment = await db.UserStoreAssignments.FindAsync([cmd.UserId, cmd.StoreId], ct);
        if (assignment is null) return; // idempotent
        db.UserStoreAssignments.Remove(assignment);
        await db.SaveChangesAsync(ct);
    }
}
