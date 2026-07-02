using OpsFlow.Domain.Authorization;
using OpsFlow.Domain.Entities;

namespace OpsFlow.Domain.Forms;

public enum ApprovalAction { Approve, Reject, Return }

/// <summary>The result of applying an action — what the submission became and the event to broadcast.</summary>
public sealed record ApprovalOutcome(string Status, string Event);

/// <summary>
/// The form-submission approval state machine. Pure: it operates on an already-loaded submission graph
/// (ApprovalSteps + FormTemplate + Store) and mutates it in place; the handler owns load, save, and broadcast.
/// Resolution, role-match, scope, and the sequential/parallel transition all live here — one place to test.
/// </summary>
public static class ApprovalWorkflow
{
    private const string Pending = "Pending";

    /// <summary>
    /// The pending step this actor would act on, or null. Parallel: any pending step for the actor's role.
    /// Sequential: the pending step at the submission's current order, but only if its role is the actor's.
    /// super_admin is the ultimate user — it can act on any pending step (parallel: any; sequential: the
    /// current-order step regardless of that step's role), so a workflow never stalls for want of a role.
    /// </summary>
    public static FormSubmissionApprovalStep? ResolveCurrentStep(FormSubmission submission, string actorRole)
    {
        var isParallel = submission.FormTemplate?.PropagationType == "Parallel";
        var isSuperAdmin = Roles.IsSuperAdmin(actorRole);
        return isParallel
            ? submission.ApprovalSteps.FirstOrDefault(s => s.Action == Pending && (isSuperAdmin || s.Role == actorRole))
            : submission.ApprovalSteps.FirstOrDefault(s =>
                s.Action == Pending && s.StepOrder == submission.CurrentStepOrder && (isSuperAdmin || s.Role == actorRole));
    }

    /// <summary>
    /// Authorize the actor against the current step and apply the action, mutating the submission graph.
    /// Throws if the submission isn't pending, the actor has no matching current step, or the actor is
    /// not scoped to the submission's store.
    /// </summary>
    public static ApprovalOutcome Apply(
        FormSubmission submission,
        ApprovalAction action,
        string actorUserId,
        ScopeSpec scope,
        string? note,
        DateTimeOffset now)
    {
        if (submission.Status != "PendingApproval")
            throw new InvalidOperationException($"Submission is not pending approval (status: {submission.Status}).");

        var step = ResolveCurrentStep(submission, scope.Role)
            ?? throw new UnauthorizedAccessException("Your role does not match the current approval step.");

        scope.AssertCanViewStore(submission.Store!.RegionId, submission.StoreId);

        step.ActionByUserId = actorUserId;
        step.ActionAt = now;
        step.Comments = note;

        return action switch
        {
            ApprovalAction.Approve => Approve(submission, step, now),
            ApprovalAction.Reject => Reject(submission, step, now),
            ApprovalAction.Return => ReturnToSubmitter(submission, step),
            _ => throw new ArgumentOutOfRangeException(nameof(action)),
        };
    }

    private static ApprovalOutcome Approve(FormSubmission submission, FormSubmissionApprovalStep step, DateTimeOffset now)
    {
        step.Action = "Approved";
        var isParallel = submission.FormTemplate?.PropagationType == "Parallel";

        if (isParallel)
        {
            AutoCloseOtherPending(submission, step);
            Resolve(submission, "Approved", now);
            return new ApprovalOutcome(submission.Status, "FormApproved");
        }

        var nextOrder = submission.ApprovalSteps
            .Where(s => s.StepOrder > step.StepOrder)
            .Select(s => (int?)s.StepOrder)
            .OrderBy(o => o)
            .FirstOrDefault();

        if (nextOrder is null)
            Resolve(submission, "Approved", now);
        else
            submission.CurrentStepOrder = nextOrder;

        return new ApprovalOutcome(submission.Status, "FormApproved");
    }

    private static ApprovalOutcome Reject(FormSubmission submission, FormSubmissionApprovalStep step, DateTimeOffset now)
    {
        step.Action = "Rejected";
        AutoCloseOtherPending(submission, step);
        Resolve(submission, "Rejected", now);
        return new ApprovalOutcome(submission.Status, "FormRejected");
    }

    private static ApprovalOutcome ReturnToSubmitter(FormSubmission submission, FormSubmissionApprovalStep step)
    {
        step.Action = "Returned";
        // CurrentStepOrder retained intentionally — resubmit re-enters at this step.
        submission.Status = "Returned";
        return new ApprovalOutcome(submission.Status, "FormReturned");
    }

    private static void AutoCloseOtherPending(FormSubmission submission, FormSubmissionApprovalStep current)
    {
        foreach (var other in submission.ApprovalSteps.Where(s => s.Id != current.Id && s.Action == Pending))
            other.Action = "AutoClosed";
    }

    private static void Resolve(FormSubmission submission, string status, DateTimeOffset now)
    {
        submission.Status = status;
        submission.ResolvedAt = now;
        submission.CurrentStepOrder = null;
    }
}
