using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Security;
using OpsFlow.Domain.Authorization;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Checklists.GetChecklists;

internal sealed class GetChecklistsHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<GetChecklistsQuery, List<ChecklistDto>>
{
    public async Task<List<ChecklistDto>> Handle(GetChecklistsQuery query, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var spec = user.ToCaller().Scope();

        await using var db = await factory.CreateAsync(ct);

        var q = db.Checklists
            .Include(c => c.Region)
            .Include(c => c.Store)
            .Include(c => c.Items).ThenInclude(i => i.Template)
            .WhereScopedVisible(spec, c => c.Scope, c => c.RegionId, c => c.StoreId);

        if (query.Scope != null) q = q.Where(c => c.Scope == query.Scope);
        if (query.IsActive.HasValue) q = q.Where(c => c.IsActive == query.IsActive.Value);
        if (!string.IsNullOrWhiteSpace(query.Search)) q = q.Where(c => c.Name.Contains(query.Search));

        var checklists = await q.OrderBy(c => c.Scope).ThenBy(c => c.Name).ToListAsync(ct);

        return checklists.Select(c =>
        {
            var ordered = c.Items.OrderBy(i => i.Order).ToList();
            var preview = ordered.Take(3).Select(i => i.Template.Name).ToList();
            return new ChecklistDto(
                c.Id, c.Name, c.Description, c.Scope,
                c.RegionId, c.Region?.Name, c.StoreId, c.Store?.Name,
                ordered.Count, preview, c.IsActive, c.CreatedAt);
        }).ToList();
    }
}
