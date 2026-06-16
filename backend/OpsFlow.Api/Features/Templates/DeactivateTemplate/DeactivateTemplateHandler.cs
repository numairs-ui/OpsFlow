using MediatR;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Templates.DeactivateTemplate;

internal sealed class DeactivateTemplateHandler(TenantDbContextFactory factory)
    : IRequestHandler<DeactivateTemplateCommand>
{
    public async Task Handle(DeactivateTemplateCommand cmd, CancellationToken ct)
    {
        await using var db = await factory.CreateAsync(ct);
        var template = await db.TaskTemplates.FindAsync([cmd.Id], ct)
            ?? throw new KeyNotFoundException($"Template {cmd.Id} not found.");

        if (!cmd.Activate)
        {
            // Placeholder: check active recurring assignments when Wave 5 is built
            template.IsActive = false;
        }
        else
        {
            template.IsActive = true;
        }

        await db.SaveChangesAsync(ct);
    }
}
