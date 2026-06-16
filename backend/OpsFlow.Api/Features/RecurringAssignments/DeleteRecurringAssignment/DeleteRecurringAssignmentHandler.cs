using MediatR;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.RecurringAssignments.DeleteRecurringAssignment;

internal sealed class DeleteRecurringAssignmentHandler(
    TenantDbContextFactory factory) : IRequestHandler<DeleteRecurringAssignmentCommand>
{
    public async Task Handle(DeleteRecurringAssignmentCommand cmd, CancellationToken ct)
    {
        await using var db = await factory.CreateAsync(ct);
        var assignment = await db.RecurringAssignments.FindAsync([cmd.Id], ct)
            ?? throw new KeyNotFoundException($"Recurring assignment {cmd.Id} not found.");

        db.RecurringAssignments.Remove(assignment);
        await db.SaveChangesAsync(ct);
    }
}
