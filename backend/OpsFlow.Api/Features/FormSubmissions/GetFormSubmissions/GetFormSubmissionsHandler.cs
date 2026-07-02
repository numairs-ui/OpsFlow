using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Security;
using OpsFlow.Domain.Authorization;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.FormSubmissions.GetFormSubmissions;

internal sealed class GetFormSubmissionsHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<GetFormSubmissionsQuery, List<FormSubmissionSummaryDto>>
{
    public async Task<List<FormSubmissionSummaryDto>> Handle(GetFormSubmissionsQuery query, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var spec = user.ToCaller().Scope();

        if (!(spec.IsGlobal || spec.IsRegionScoped))
            throw new UnauthorizedAccessException("Only super_admin, admin, or supervisor roles can view store/region submission lists.");

        await using var db = await factory.CreateAsync(ct);

        var q = db.FormSubmissions
            .Include(s => s.FormTemplate)
            .Include(s => s.Store)
            .AsQueryable();

        if (query.StoreId.HasValue) q = q.Where(s => s.StoreId == query.StoreId.Value);
        if (query.RegionId.HasValue) q = q.Where(s => s.Store!.RegionId == query.RegionId.Value);
        if (!string.IsNullOrWhiteSpace(query.Status)) q = q.Where(s => s.Status == query.Status);

        // admin / supervisor restricted to their region set; super_admin sees all.
        q = q.WhereStoreInScope(spec, s => s.StoreId, s => s.Store!.RegionId);

        var items = await q
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new FormSubmissionSummaryDto(
                s.Id, s.FormTemplateId, s.FormTemplate != null ? s.FormTemplate.Name : null,
                s.StoreId, s.Store != null ? s.Store.Name : null, s.SubmittedByUserId,
                s.Status, s.CreatedAt, s.SubmittedAt, s.ResolvedAt))
            .ToListAsync(ct);

        return items;
    }
}
