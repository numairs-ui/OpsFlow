using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Security;
using OpsFlow.Domain.Authorization;
using OpsFlow.Infrastructure;
using System.Text.Json;

namespace OpsFlow.Api.Features.FormTemplates.GetFormTemplates;

internal sealed class GetFormTemplatesHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<GetFormTemplatesQuery, FormTemplateListResult>
{
    public async Task<FormTemplateListResult> Handle(GetFormTemplatesQuery query, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var spec = user.ToCaller().Scope();

        await using var db = await factory.CreateAsync(ct);

        var q = db.FormTemplates
            .Include(t => t.Region)
            .Include(t => t.Store)
            .WhereScopedVisible(spec, t => t.Scope, t => t.RegionId, t => t.StoreId, t => t.Store!.RegionId);

        if (query.Scope != null) q = q.Where(t => t.Scope == query.Scope);
        if (query.PropagationType != null) q = q.Where(t => t.PropagationType == query.PropagationType);
        if (query.IsActive.HasValue) q = q.Where(t => t.IsActive == query.IsActive.Value);
        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(t => t.Name.Contains(query.Search));

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderBy(t => t.Scope).ThenBy(t => t.Name)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(t => new FormTemplateDto(
                t.Id, t.Name, t.Description, t.Scope,
                t.RegionId, t.Region != null ? t.Region.Name : null,
                t.StoreId, t.Store != null ? t.Store.Name : null,
                t.PropagationType, CountFields(t.FieldsJson),
                t.IsActive, t.CreatedAt))
            .ToListAsync(ct);

        return new FormTemplateListResult(items, total);
    }

    private static int CountFields(string json)
    {
        try { return JsonDocument.Parse(json).RootElement.GetArrayLength(); }
        catch { return 0; }
    }
}
