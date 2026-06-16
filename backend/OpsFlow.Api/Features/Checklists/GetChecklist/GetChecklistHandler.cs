using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Checklists.GetChecklist;

internal sealed class GetChecklistHandler(TenantDbContextFactory factory)
    : IRequestHandler<GetChecklistQuery, ChecklistDetailDto>
{
    public async Task<ChecklistDetailDto> Handle(GetChecklistQuery query, CancellationToken ct)
    {
        await using var db = await factory.CreateAsync(ct);
        var c = await db.Checklists
            .Include(x => x.Region)
            .Include(x => x.Store)
            .Include(x => x.Items).ThenInclude(i => i.Template)
            .FirstOrDefaultAsync(x => x.Id == query.Id, ct)
            ?? throw new KeyNotFoundException($"Checklist {query.Id} not found.");

        var items = c.Items.OrderBy(i => i.Order)
            .Select(i => new ChecklistItemDto(i.TemplateId, i.Template.Name, i.Order))
            .ToList();

        return new ChecklistDetailDto(
            c.Id, c.Name, c.Description, c.Scope,
            c.RegionId, c.Region?.Name, c.StoreId, c.Store?.Name,
            items, c.IsActive, c.CreatedAt);
    }
}
