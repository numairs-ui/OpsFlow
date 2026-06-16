using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.FormTemplates.DeactivateFormTemplate;

internal sealed class DeactivateFormTemplateHandler(TenantDbContextFactory factory)
    : IRequestHandler<DeactivateFormTemplateCommand>
{
    private static readonly string[] ActiveStatuses = ["Draft", "Submitted", "PendingApproval", "Returned"];

    public async Task Handle(DeactivateFormTemplateCommand cmd, CancellationToken ct)
    {
        await using var db = await factory.CreateAsync(ct);
        var template = await db.FormTemplates.FindAsync([cmd.Id], ct)
            ?? throw new KeyNotFoundException($"Form template {cmd.Id} not found.");

        if (!cmd.Activate)
        {
            var hasActiveSubmissions = await db.FormSubmissions
                .AnyAsync(s => s.FormTemplateId == cmd.Id && ActiveStatuses.Contains(s.Status), ct);
            if (hasActiveSubmissions)
                throw new InvalidOperationException(
                    "Cannot deactivate a form template with active (non-terminal) submissions.");
            template.IsActive = false;
        }
        else
        {
            template.IsActive = true;
        }

        await db.SaveChangesAsync(ct);
    }
}
