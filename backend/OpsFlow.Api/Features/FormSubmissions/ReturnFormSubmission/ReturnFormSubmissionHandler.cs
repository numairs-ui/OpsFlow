using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Hubs;
using OpsFlow.Infrastructure;
using System.Security.Claims;

namespace OpsFlow.Api.Features.FormSubmissions.ReturnFormSubmission;

internal sealed class ReturnFormSubmissionHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor,
    IHubContext<TaskBoardHub> hub) : IRequestHandler<ReturnFormSubmissionCommand>
{
    public async Task Handle(ReturnFormSubmissionCommand cmd, CancellationToken ct)
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
        step.Action = "Returned";
        step.ActionByUserId = userId;
        step.ActionAt = now;
        step.Comments = cmd.Comments;

        // currentStepOrder retained intentionally — resubmit re-enters at this step
        submission.Status = "Returned";

        await db.SaveChangesAsync(ct);

        await hub.Clients.Group($"store-{submission.StoreId}").SendAsync(
            "FormSubmissionUpdated",
            new { submission.Id, submission.Status, Event = "FormReturned", Comments = cmd.Comments },
            ct);
    }
}
