using MediatR;
using OpsFlow.Domain.Entities;
using OpsFlow.Infrastructure;
using System.Security.Claims;

namespace OpsFlow.Api.Features.Templates.CreateTemplate;

internal sealed class CreateTemplateHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<CreateTemplateCommand, Guid>
{
    public async Task<Guid> Handle(CreateTemplateCommand cmd, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var tenantId = user.FindFirstValue("tenantId")!;
        var role = user.FindFirstValue("role") ?? user.FindFirstValue(ClaimTypes.Role) ?? "";
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub")!;

        // Scope-role authorization
        if (cmd.Scope == "System" && role != "admin")
            throw new UnauthorizedAccessException("Only admins can create System-scope templates.");
        if (cmd.Scope == "Regional" && role != "admin" && role != "supervisor")
            throw new UnauthorizedAccessException("Regional templates require supervisor or admin role.");

        await using var db = await factory.CreateAsync(ct);

        var template = new TaskTemplate
        {
            TenantId = tenantId,
            Name = cmd.Name,
            Description = cmd.Description,
            Category = cmd.Category,
            Scope = cmd.Scope,
            RegionId = cmd.RegionId,
            StoreId = cmd.StoreId,
            FieldsJson = cmd.FieldsJson,
            CreatedByUserId = userId,
        };

        db.TaskTemplates.Add(template);
        await db.SaveChangesAsync(ct);
        return template.Id;
    }
}
