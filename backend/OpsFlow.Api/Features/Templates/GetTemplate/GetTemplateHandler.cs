using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Security;
using OpsFlow.Domain.Authorization;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Templates.GetTemplate;

internal sealed class GetTemplateHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<GetTemplateQuery, TemplateDetailDto>
{
    public async Task<TemplateDetailDto> Handle(GetTemplateQuery query, CancellationToken ct)
    {
        var spec = httpContextAccessor.HttpContext!.User.ToCaller().Scope();

        await using var db = await factory.CreateAsync(ct);

        // Same visibility rule as the list endpoint, narrowed to one row — a template outside
        // the caller's scope 404s exactly like one that doesn't exist, rather than leaking presence.
        var t = await db.TaskTemplates
            .Include(x => x.Region)
            .Include(x => x.Store)
            .WhereScopedVisible(spec, x => x.Scope, x => x.RegionId, x => x.StoreId, x => x.Store!.RegionId)
            .FirstOrDefaultAsync(x => x.Id == query.Id, ct)
            ?? throw new KeyNotFoundException($"Template {query.Id} not found.");

        return new TemplateDetailDto(
            t.Id, t.Name, t.Description, t.Category, t.Scope,
            t.RegionId, t.Region?.Name, t.StoreId, t.Store?.Name,
            t.FieldsJson, t.IsActive, t.CreatedAt);
    }
}
