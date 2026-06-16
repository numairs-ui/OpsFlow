using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.FormTemplates.GetFormTemplate;

internal sealed class GetFormTemplateHandler(TenantDbContextFactory factory)
    : IRequestHandler<GetFormTemplateQuery, FormTemplateDetailDto>
{
    public async Task<FormTemplateDetailDto> Handle(GetFormTemplateQuery query, CancellationToken ct)
    {
        await using var db = await factory.CreateAsync(ct);
        var t = await db.FormTemplates
            .Include(x => x.Region)
            .Include(x => x.Store)
            .FirstOrDefaultAsync(x => x.Id == query.Id, ct)
            ?? throw new KeyNotFoundException($"Form template {query.Id} not found.");

        return new FormTemplateDetailDto(
            t.Id, t.Name, t.Description, t.Scope,
            t.RegionId, t.Region?.Name, t.StoreId, t.Store?.Name,
            t.PropagationType, t.ApprovalStepsJson, t.FieldsJson, t.IsActive, t.CreatedAt);
    }
}
