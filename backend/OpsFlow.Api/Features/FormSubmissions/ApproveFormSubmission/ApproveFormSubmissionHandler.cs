using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Hubs;
using OpsFlow.Infrastructure;
using System.Security.Claims;

namespace OpsFlow.Api.Features.FormSubmissions.ApproveFormSubmission;

internal sealed class ApproveFormSubmissionHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor,
    IHubContext<TaskBoardHub> hub) : IRequestHandler<ApproveFormSubmissionCommand>
{
    public async Task Handle(ApproveFormSubmissionCommand cmd, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub")!;
        var role = user.FindFirstValue("role") ?? user.FindFirstValue(ClaimTypes.Role) ?? "";
        var userStoreId = user.FindFirstValue("storeId");
        var userRegionId = user.FindFirstValue("regionId");

        await using var db = await factory.CreateAsync(ct);

        var submission = await db.FormSubmissions
            .Include(s => s.ApprovalSteps)
            .Include(s => s.FormTemplate)
            .Include(s => s.Store)
            .FirstOrDefaultAsync(s => s.Id == cmd.Id, ct)
            ?? throw new KeyNotFoundException($"Form submission {cmd.Id} not found.");

        if (submission.Status != "PendingApproval")
            throw new InvalidOperationException($"Submission is not pending approval (status: {submission.Status}).");

        var isParallel = submission.FormTemplate!.PropagationType == "Parallel";

        var step = isParallel
            ? submission.ApprovalSteps.FirstOrDefault(s => s.Action == "Pending" && s.Role == role)
            : submission.ApprovalSteps.FirstOrDefault(s => s.Action == "Pending" && s.StepOrder == submission.CurrentStepOrder);

        if (step == null || step.Role != role)
            throw new UnauthorizedAccessException("Your role does not match the current approval step.");

        if (role != "admin")
        {
            if (step.Role is "store_manager" or "store_employee" && userStoreId != submission.StoreId.ToString())
                throw new UnauthorizedAccessException("You are not scoped to this submission's store.");
            if (step.Role == "supervisor" && (userRegionId == null || userRegionId != submission.Store!.RegionId.ToString()))
                throw new UnauthorizedAccessException("You are not scoped to this submission's region.");
        }

        var now = DateTimeOffset.UtcNow;
        step.Action = "Approved";
        step.ActionByUserId = userId;
        step.ActionAt = now;

        if (isParallel)
        {
            foreach (var other in submission.ApprovalSteps.Where(s => s.Id != step.Id && s.Action == "Pending"))
                other.Action = "AutoClosed";
            submission.Status = "Approved";
            submission.ResolvedAt = now;
            submission.CurrentStepOrder = null;
        }
        else
        {
            var nextOrder = submission.ApprovalSteps
                .Where(s => s.StepOrder > step.StepOrder)
                .Select(s => s.StepOrder)
                .OrderBy(o => o)
                .Cast<int?>()
                .FirstOrDefault();

            if (nextOrder == null)
            {
                submission.Status = "Approved";
                submission.ResolvedAt = now;
                submission.CurrentStepOrder = null;
            }
            else
            {
                submission.CurrentStepOrder = nextOrder;
            }
        }

        await db.SaveChangesAsync(ct);

        await hub.Clients.Group($"store-{submission.StoreId}").SendAsync(
            "FormSubmissionUpdated",
            new { submission.Id, submission.Status, Event = "FormApproved" },
            ct);
    }
}
