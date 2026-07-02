using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Security;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Users.GetUserActivity;

internal sealed class GetUserActivityHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor)
    : IRequestHandler<GetUserActivityQuery, List<UserActivityDto>>
{
    public async Task<List<UserActivityDto>> Handle(GetUserActivityQuery query, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var spec = user.ToCaller().Scope();
        var callerId = user.GetUserId();
        var uid = query.UserId;

        await using var db = await factory.CreateAsync(ct);

        // Authorize: anyone may read their own activity. Reading another user's is a management
        // function — store-scoped roles (employee/kiosk/manager) get only themselves; a region role
        // may read users within its region set; super_admin reads anyone. (Tenant isolation already
        // comes from the tenant-scoped DbContext.)
        if (uid != callerId)
        {
            if (spec.IsStoreScoped)
                throw new UnauthorizedAccessException("You can only view your own activity.");

            var target = await db.UserProfiles
                .Where(p => p.UserId == uid)
                .Select(p => new { p.StoreId, p.RegionId })
                .FirstOrDefaultAsync(ct)
                ?? throw new KeyNotFoundException($"User {uid} not found.");

            if (target.StoreId is { } storeId)
            {
                var storeRegionId = await db.Stores
                    .Where(s => s.Id == storeId)
                    .Select(s => (Guid?)s.RegionId)
                    .FirstOrDefaultAsync(ct);
                spec.AssertCanViewStore(storeRegionId ?? Guid.Empty, storeId);
            }
            else if (target.RegionId is { } regionId)
            {
                spec.AssertCanViewRegion(regionId);
            }
            else
            {
                // A user with neither store nor region (e.g. another admin/super_admin) — only super_admin may inspect.
                spec.AssertGlobal();
            }
        }

        var submissions = await db.FormSubmissions
            .Where(f => f.SubmittedByUserId == uid)
            .Include(f => f.FormTemplate)
            .OrderByDescending(f => f.CreatedAt)
            .Take(50)
            .Select(f => new UserActivityDto(
                "form",
                f.FormTemplate != null ? f.FormTemplate.Name : "Form Submission",
                f.Status,
                f.SubmittedAt ?? f.CreatedAt))
            .ToListAsync(ct);

        var tasks = await db.TaskInstances
            .Where(t => t.AssignedToUserId == uid || t.CompletedByUserId == uid)
            .Include(t => t.Checklist)
            .OrderByDescending(t => t.CreatedAt)
            .Take(50)
            .Select(t => new UserActivityDto(
                "task",
                t.Checklist != null ? t.Checklist.Name : "Task",
                t.Status,
                t.CompletedAt ?? t.CreatedAt))
            .ToListAsync(ct);

        return submissions
            .Concat(tasks)
            .OrderByDescending(a => a.Date)
            .Take(40)
            .ToList();
    }
}
