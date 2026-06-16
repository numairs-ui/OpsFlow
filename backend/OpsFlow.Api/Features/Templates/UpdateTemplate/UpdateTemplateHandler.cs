using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Infrastructure;
using System.Security.Claims;

namespace OpsFlow.Api.Features.Templates.UpdateTemplate;

internal sealed class UpdateTemplateHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<UpdateTemplateCommand>
{
    public async Task Handle(UpdateTemplateCommand cmd, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var role = user.FindFirstValue("role") ?? user.FindFirstValue(ClaimTypes.Role) ?? "";

        await using var db = await factory.CreateAsync(ct);
        var template = await db.TaskTemplates.FindAsync([cmd.Id], ct)
            ?? throw new KeyNotFoundException($"Template {cmd.Id} not found.");

        if (template.Scope == "System" && role != "admin")
            throw new UnauthorizedAccessException("Only admins can edit System-scope templates.");
        if (template.Scope == "Regional" && role != "admin" && role != "supervisor")
            throw new UnauthorizedAccessException("Regional templates require supervisor or admin role.");

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
