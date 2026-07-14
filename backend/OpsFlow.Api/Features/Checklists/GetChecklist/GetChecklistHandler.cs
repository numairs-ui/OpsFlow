using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Security;
using OpsFlow.Domain.Authorization;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Checklists.GetChecklist;

internal sealed class GetChecklistHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<GetChecklistQuery, ChecklistDetailDto>
{
    public async Task<ChecklistDetailDto> Handle(GetChecklistQuery query, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var spec = user.ToCaller().Scope();

        await using var db = await factory.CreateAsync(ct);

        // Same visibility rule as the list endpoint, narrowed to one row — a checklist outside
        // the caller's scope 404s exactly like one that doesn't exist, rather than leaking presence.
        var c = await db.Checklists
            .Include(x => x.Region)
            .Include(x => x.Store)
            .Include(x => x.Items).ThenInclude(i => i.Template)
            .WhereScopedVisible(spec, x => x.Scope, x => x.RegionId, x => x.StoreId, x => x.Store!.RegionId)
            .FirstOrDefaultAsync(x => x.Id == query.Id, ct)
            ?? throw new KeyNotFoundException($"Checklist {query.Id} not found.");

        var items = c.Items.OrderBy(i => i.Order)
            .Select(i => new ChecklistItemDto(
                i.TemplateId, i.Template.Name, i.Order, i.Template.FieldsJson,
                i.ScoringType, i.Weight, i.PhotoRequired, i.FailCorrectiveActionText, i.FailScoreThreshold))
            .ToList();

        return new ChecklistDetailDto(
            c.Id, c.Name, c.Description, c.Scope,
            c.RegionId, c.Region?.Name, c.StoreId, c.Store?.Name,
            items, c.IsActive, c.CreatedAt);
    }
}
