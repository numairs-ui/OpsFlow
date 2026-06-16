using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Infrastructure;
using System.Security.Claims;

namespace OpsFlow.Api.Features.FormSubmissions.GetMySubmissions;

internal sealed class GetMySubmissionsHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<GetMySubmissionsQuery, List<MySubmissionDto>>
{
    public async Task<List<MySubmissionDto>> Handle(GetMySubmissionsQuery query, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub")!;

        await using var db = await factory.CreateAsync(ct);

        var q = db.FormSubmissions
            .Include(s => s.FormTemplate)
            .Where(s => s.SubmittedByUserId == userId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Status))
            q = q.Where(s => s.Status == query.Status);

        var items = await q
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new MySubmissionDto(
                s.Id, s.FormTemplateId, s.FormTemplate != null ? s.FormTemplate.Name : null,
                s.StoreId, s.Status, s.CurrentStepOrder, s.CreatedAt, s.SubmittedAt, s.ResolvedAt))
            .ToListAsync(ct);

        return items;
    }
}
