using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Security;
using OpsFlow.Domain.Authorization;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Templates.UpdateTemplate;

internal sealed class UpdateTemplateHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<UpdateTemplateCommand>
{
    public async Task Handle(UpdateTemplateCommand cmd, CancellationToken ct)
    {
        var spec = httpContextAccessor.HttpContext!.User.ToCaller().Scope();

        await using var db = await factory.CreateAsync(ct);
        var template = await db.TaskTemplates.FindAsync([cmd.Id], ct)
            ?? throw new KeyNotFoundException($"Template {cmd.Id} not found.");

        await spec.AssertCanWriteScopeAsync(db, template.Scope, template.RegionId, template.StoreId, ct);

        // Block fields update if active recurring assignments exist (TB-23 constraint)
        if (cmd.FieldsJson != null && cmd.FieldsJson != template.FieldsJson)
        {
            var hasActiveAssignments = false; // RecurringAssignments not yet built (Wave 5) — placeholder
            if (hasActiveAssignments)
                throw new InvalidOperationException(
                    "Cannot change fields while active recurring assignments reference this template.");
            template.FieldsJson = cmd.FieldsJson;
        }

        template.Name = cmd.Name;
        template.Description = cmd.Description;
        template.Category = cmd.Category;
        await db.SaveChangesAsync(ct);
    }
}
