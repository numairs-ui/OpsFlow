using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Domain.Entities;
using OpsFlow.Infrastructure;
using System.Security.Claims;

namespace OpsFlow.Api.Features.FormSubmissions.GetPendingReview;

internal sealed class GetPendingReviewHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<GetPendingReviewQuery, List<PendingReviewDto>>
{
    public async Task<List<PendingReviewDto>> Handle(GetPendingReviewQuery query, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var role = user.FindFirstValue("role") ?? user.FindFirstValue(ClaimTypes.Role) ?? "";
        var userStoreId = user.FindFirstValue("storeId");
        var userRegionId = user.FindFirstValue("regionId");

        await using var db = await factory.CreateAsync(ct);

        var submissions = await db.FormSubmissions
            .Include(s => s.FormTemplate)
            .Include(s => s.Store)
            .Include(s => s.ApprovalSteps)
            .Where(s => s.Status == "PendingApproval")
            .ToListAsync(ct);

        var results = new List<PendingReviewDto>();
        foreach (var s in submissions)
        {
            var isParallel = s.FormTemplate?.PropagationType == "Parallel";
            FormSubmissionApprovalStep? step = isParallel
                ? s.ApprovalSteps.FirstOrDefault(a => a.Action == "Pending" && a.Role == role)
                : s.ApprovalSteps.FirstOrDefault(a => a.Action == "Pending" && a.StepOrder == s.CurrentStepOrder && a.Role == role);

            if (step == null) continue;

            if (role != "admin")
            {
                if (step.Role is "store_manager" or "store_employee" && userStoreId != s.StoreId.ToString()) continue;
                if (step.Role == "supervisor" && (userRegionId == null || s.Store == null || userRegionId != s.Store.RegionId.ToString())) continue;
            }

            results.Add(new PendingReviewDto(
                s.Id, s.FormTemplateId, s.FormTemplate?.Name, s.StoreId, s.Store?.Name,
                s.SubmittedByUserId, s.Status, step.StepOrder, s.SubmittedAt));
        }

        return results.OrderBy(r => r.SubmittedAt).ToList();
    }
}
