using MediatR;
using OpsFlow.Infrastructure;
using System.Security.Claims;
using System.Text.Json;

namespace OpsFlow.Api.Features.FormTemplates.UpdateFormTemplate;

internal sealed class UpdateFormTemplateHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<UpdateFormTemplateCommand>
{
    public async Task Handle(UpdateFormTemplateCommand cmd, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var role = user.FindFirstValue("role") ?? user.FindFirstValue(ClaimTypes.Role) ?? "";

        await using var db = await factory.CreateAsync(ct);
        var template = await db.FormTemplates.FindAsync([cmd.Id], ct)
            ?? throw new KeyNotFoundException($"Form template {cmd.Id} not found.");

        if (template.Scope == "System" && role != "admin")
            throw new UnauthorizedAccessException("Only admins can edit System-scope form templates.");
        if (template.Scope == "Regional" && role != "admin" && role != "supervisor")
            throw new UnauthorizedAccessException("Regional form templates require supervisor or admin role.");

        template.Name = cmd.Name;
        template.Description = cmd.Description;
        template.PropagationType = cmd.PropagationType;
        template.ApprovalStepsJson = JsonSerializer.Serialize(cmd.ApprovalSteps);
        if (cmd.FieldsJson != null) template.FieldsJson = cmd.FieldsJson;

        await db.SaveChangesAsync(ct);
    }
}
