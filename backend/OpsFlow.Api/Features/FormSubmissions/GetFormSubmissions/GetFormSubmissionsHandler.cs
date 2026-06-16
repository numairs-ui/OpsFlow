using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Infrastructure;
using System.Security.Claims;

namespace OpsFlow.Api.Features.FormSubmissions.GetFormSubmissions;

internal sealed class GetFormSubmissionsHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<GetFormSubmissionsQuery, List<FormSubmissionSummaryDto>>
{
    public async Task<List<FormSubmissionSummaryDto>> Handle(GetFormSubmissionsQuery query, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var role = user.FindFirstValue("role") ?? user.FindFirstValue(ClaimTypes.Role) ?? "";
        var userRegionId = user.FindFirstValue("regionId");

        if (role != "admin" && role != "supervisor")
            throw new UnauthorizedAccessException("Only admin or supervisor roles can view store/region submission lists.");

        await using var db = await factory.CreateAsync(ct);

        var q = db.FormSubmissions
            .Include(s => s.FormTemplate)
            .Include(s => s.Store)
            .AsQueryable();

        if (query.StoreId.HasValue) q = q.Where(s => s.StoreId == query.StoreId.Value);
        if (query.RegionId.HasValue) q = q.Where(s => s.Store!.RegionId == query.RegionId.Value);
        if (!string.IsNullOrWhiteSpace(query.Status)) q = q.Where(s => s.Status == query.Status);

        // Supervisors restricted to their own region unless a specific store/region filter already narrows it
        if (role == "supervisor" && userRegionId != null)
            q = q.Where(s => s.Store!.RegionId.ToString() == userRegionId);

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
