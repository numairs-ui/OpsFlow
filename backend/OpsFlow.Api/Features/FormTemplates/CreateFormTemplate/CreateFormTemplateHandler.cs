using MediatR;
using OpsFlow.Domain.Entities;
using OpsFlow.Infrastructure;
using System.Security.Claims;
using System.Text.Json;

namespace OpsFlow.Api.Features.FormTemplates.CreateFormTemplate;

internal sealed class CreateFormTemplateHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<CreateFormTemplateCommand, Guid>
{
    public async Task<Guid> Handle(CreateFormTemplateCommand cmd, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var tenantId = user.FindFirstValue("tenantId")!;
        var role = user.FindFirstValue("role") ?? user.FindFirstValue(ClaimTypes.Role) ?? "";
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub")!;

        if (cmd.Scope == "System" && role != "admin")
            throw new UnauthorizedAccessException("Only admins can create System-scope form templates.");
        if (cmd.Scope == "Regional" && role != "admin" && role != "supervisor")
            throw new UnauthorizedAccessException("Regional form templates require supervisor or admin role.");

        await using var db = await factory.CreateAsync(ct);

        var template = new FormTemplate
        {
            TenantId = tenantId,
            Name = cmd.Name,
            Description = cmd.Description,
            Scope = cmd.Scope,
            RegionId = cmd.RegionId,
            StoreId = cmd.StoreId,
            PropagationType = cmd.PropagationType,
            ApprovalStepsJson = JsonSerializer.Serialize(cmd.ApprovalSteps),
            FieldsJson = cmd.FieldsJson,
            CreatedByUserId = userId,
        };

        db.FormTemplates.Add(template);
        await db.SaveChangesAsync(ct);
        return template.Id;
    }
}
