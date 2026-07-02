using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Security;
using OpsFlow.Domain.Forms;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.FormSubmissions.GetPendingReview;

internal sealed class GetPendingReviewHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<GetPendingReviewQuery, List<PendingReviewDto>>
{
    public async Task<List<PendingReviewDto>> Handle(GetPendingReviewQuery query, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var spec = user.ToCaller().Scope();

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
            // The step this caller would act on (parallel/sequential resolution lives in the module)…
            var step = ApprovalWorkflow.ResolveCurrentStep(s, spec.Role);
            if (step is null) continue;

            // …and they must be scoped to the submission's store (super_admin sees all).
            if (!spec.CanViewStore(s.Store!.RegionId, s.StoreId)) continue;

            results.Add(new PendingReviewDto(
                s.Id, s.FormTemplateId, s.FormTemplate?.Name, s.StoreId, s.Store?.Name,
                s.SubmittedByUserId, s.Status, step.StepOrder, s.SubmittedAt));
        }

        return results.OrderBy(r => r.SubmittedAt).ToList();
    }
}
