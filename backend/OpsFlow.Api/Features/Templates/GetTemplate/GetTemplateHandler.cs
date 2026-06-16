using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Templates.GetTemplate;

internal sealed class GetTemplateHandler(TenantDbContextFactory factory)
    : IRequestHandler<GetTemplateQuery, TemplateDetailDto>
{
    public async Task<TemplateDetailDto> Handle(GetTemplateQuery query, CancellationToken ct)
    {
        await using var db = await factory.CreateAsync(ct);
        var t = await db.TaskTemplates
            .Include(x => x.Region)
            .Include(x => x.Store)
            .FirstOrDefaultAsync(x => x.Id == query.Id, ct)
            ?? throw new KeyNotFoundException($"Template {query.Id} not found.");

        return new TemplateDetailDto(
            t.Id, t.Name, t.Description, t.Category, t.Scope,
            t.RegionId, t.Region?.Name, t.StoreId, t.Store?.Name,
            t.FieldsJson, t.IsActive, t.CreatedAt);
    }
}
