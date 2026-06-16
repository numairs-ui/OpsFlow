using MediatR;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Checklists.DeactivateChecklist;

internal sealed class DeactivateChecklistHandler(TenantDbContextFactory factory)
    : IRequestHandler<DeactivateChecklistCommand>
{
    public async Task Handle(DeactivateChecklistCommand cmd, CancellationToken ct)
    {
        await using var db = await factory.CreateAsync(ct);
        var checklist = await db.Checklists.FindAsync([cmd.Id], ct)
            ?? throw new KeyNotFoundException($"Checklist {cmd.Id} not found.");

        // Placeholder: check active RecurringAssignments when Wave 5 is built
        checklist.IsActive = cmd.Activate;
        await db.SaveChangesAsync(ct);
    }
}
