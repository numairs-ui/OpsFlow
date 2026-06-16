using MediatR;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.RecurringAssignments.PauseRecurringAssignment;

internal sealed class PauseRecurringAssignmentHandler(
    TenantDbContextFactory factory) : IRequestHandler<PauseRecurringAssignmentCommand>
{
    public async Task Handle(PauseRecurringAssignmentCommand cmd, CancellationToken ct)
    {
        await using var db = await factory.CreateAsync(ct);
        var assignment = await db.RecurringAssignments.FindAsync([cmd.Id], ct)
            ?? throw new KeyNotFoundException($"Recurring assignment {cmd.Id} not found.");

        assignment.IsPaused = !cmd.Resume;
        await db.SaveChangesAsync(ct);
    }
}
