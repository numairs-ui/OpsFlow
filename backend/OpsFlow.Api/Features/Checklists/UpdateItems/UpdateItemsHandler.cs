using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Domain.Entities;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Checklists.UpdateItems;

internal sealed class UpdateItemsHandler(TenantDbContextFactory factory)
    : IRequestHandler<UpdateItemsCommand>
{
    public async Task Handle(UpdateItemsCommand cmd, CancellationToken ct)
    {
        await using var db = await factory.CreateAsync(ct);

        var checklist = await db.Checklists.FindAsync([cmd.ChecklistId], ct)
            ?? throw new KeyNotFoundException($"Checklist {cmd.ChecklistId} not found.");

        var templateIds = cmd.Items.Select(i => i.TemplateId).Distinct().ToList();
        var foundCount = await db.TaskTemplates.CountAsync(t => templateIds.Contains(t.Id) && t.IsActive, ct);
        if (foundCount != templateIds.Count)
            throw new KeyNotFoundException("One or more template IDs are invalid or inactive.");

        // Full replace — remove all existing items, add the new set
        var existing = await db.ChecklistTemplateItems
            .Where(i => i.ChecklistId == cmd.ChecklistId)
            .ToListAsync(ct);
        db.ChecklistTemplateItems.RemoveRange(existing);

        foreach (var item in cmd.Items)
        {
            db.ChecklistTemplateItems.Add(new ChecklistTemplateItem
            {
                ChecklistId = cmd.ChecklistId,
                TemplateId = item.TemplateId,
                Order = item.Order,
            });
        }

        await db.SaveChangesAsync(ct);
    }
}
