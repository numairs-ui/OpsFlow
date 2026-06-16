using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Domain.Entities;
using OpsFlow.Infrastructure;
using System.Security.Claims;

namespace OpsFlow.Api.Features.Checklists.CreateChecklist;

internal sealed class CreateChecklistHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<CreateChecklistCommand, Guid>
{
    public async Task<Guid> Handle(CreateChecklistCommand cmd, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var tenantId = user.FindFirstValue("tenantId")!;
        var role = user.FindFirstValue("role") ?? user.FindFirstValue(ClaimTypes.Role) ?? "";
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub")!;

        if (cmd.Scope == "System" && role != "admin")
            throw new UnauthorizedAccessException("Only admins can create System-scope checklists.");
        if (cmd.Scope == "Regional" && role != "admin" && role != "supervisor")
            throw new UnauthorizedAccessException("Regional checklists require supervisor or admin role.");

        await using var db = await factory.CreateAsync(ct);

        // Validate all template IDs exist and are visible to the creator
        var templateIds = cmd.Items.Select(i => i.TemplateId).Distinct().ToList();
        var foundCount = await db.TaskTemplates.CountAsync(t => templateIds.Contains(t.Id) && t.IsActive, ct);
        if (foundCount != templateIds.Count)
            throw new KeyNotFoundException("One or more template IDs are invalid or inactive.");

        var checklist = new Checklist
        {
            TenantId = tenantId,
            Name = cmd.Name,
            Description = cmd.Description,
            Scope = cmd.Scope,
            RegionId = cmd.RegionId,
            StoreId = cmd.StoreId,
            CreatedByUserId = userId,
        };
        db.Checklists.Add(checklist);

        foreach (var item in cmd.Items)
        {
            db.ChecklistTemplateItems.Add(new ChecklistTemplateItem
            {
                ChecklistId = checklist.Id,
                TemplateId = item.TemplateId,
                Order = item.Order,
            });
        }

        await db.SaveChangesAsync(ct);
        return checklist.Id;
    }
}
