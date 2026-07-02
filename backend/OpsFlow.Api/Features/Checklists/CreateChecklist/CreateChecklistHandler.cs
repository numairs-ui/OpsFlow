using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Security;
using OpsFlow.Domain.Authorization;
using OpsFlow.Domain.Entities;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Checklists.CreateChecklist;

internal sealed class CreateChecklistHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<CreateChecklistCommand, Guid>
{
    public async Task<Guid> Handle(CreateChecklistCommand cmd, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var tenantId = user.GetTenantId();
        var userId = user.GetUserId();

        var spec = user.ToCaller().Scope();

        await using var db = await factory.CreateAsync(ct);

        await spec.AssertCanWriteScopeAsync(db, cmd.Scope, cmd.RegionId, cmd.StoreId, ct);

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
