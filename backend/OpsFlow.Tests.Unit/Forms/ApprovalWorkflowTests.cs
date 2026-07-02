using FluentAssertions;
using OpsFlow.Domain.Authorization;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Forms;
using Xunit;

namespace OpsFlow.Tests.Unit.Forms;

/// <summary>
/// The approval workflow is pure — it mutates an in-memory submission graph — so it is tested
/// directly with constructed entities, no DbContext.
/// </summary>
public sealed class ApprovalWorkflowTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 28, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Region = Guid.NewGuid();
    private static readonly Guid Store = Guid.NewGuid();

    private static ScopeSpec Manager(Guid store) => new Caller(Roles.StoreManager, store, []).Scope();
    private static ScopeSpec Supervisor(Guid region) => new Caller(Roles.Supervisor, null, [region]).Scope();

    private static FormSubmission Submission(
        string propagation, int? currentStepOrder,
        params (int order, string role, string action)[] steps) => new()
    {
        StoreId = Store,
        Status = "PendingApproval",
        CurrentStepOrder = currentStepOrder,
        FormTemplate = new FormTemplate { PropagationType = propagation },
        Store = new Store { Id = Store, RegionId = Region },
        ApprovalSteps = steps
            .Select(s => new FormSubmissionApprovalStep { StepOrder = s.order, Role = s.role, Action = s.action })
            .ToList(),
    };

    // ── Sequential happy path ─────────────────────────────────────────────────

    [Fact]
    public void Sequential_approve_advances_then_completes()
    {
        var sub = Submission("Sequential", 1,
            (1, Roles.StoreManager, "Pending"),
            (2, Roles.Supervisor, "Pending"));

        var first = ApprovalWorkflow.Apply(sub, ApprovalAction.Approve, "u1", Manager(Store), null, Now);

        first.Status.Should().Be("PendingApproval");      // not done yet
        first.Event.Should().Be("FormApproved");
        sub.CurrentStepOrder.Should().Be(2);
        sub.ApprovalSteps.Single(s => s.StepOrder == 1).Action.Should().Be("Approved");

        var second = ApprovalWorkflow.Apply(sub, ApprovalAction.Approve, "u2", Supervisor(Region), null, Now);

        second.Status.Should().Be("Approved");
        sub.Status.Should().Be("Approved");
        sub.ResolvedAt.Should().Be(Now);
        sub.CurrentStepOrder.Should().BeNull();
    }

    // ── Parallel approve closes the rest ──────────────────────────────────────

    [Fact]
    public void Parallel_approve_autocloses_other_pending_and_completes()
    {
        var sub = Submission("Parallel", null,
            (1, Roles.StoreManager, "Pending"),
            (2, Roles.Supervisor, "Pending"));

        var outcome = ApprovalWorkflow.Apply(sub, ApprovalAction.Approve, "u1", Manager(Store), null, Now);

        outcome.Status.Should().Be("Approved");
        sub.Status.Should().Be("Approved");
        sub.ApprovalSteps.Single(s => s.Role == Roles.StoreManager).Action.Should().Be("Approved");
        sub.ApprovalSteps.Single(s => s.Role == Roles.Supervisor).Action.Should().Be("AutoClosed");
    }

    // ── Reject terminates + autocloses; note recorded ─────────────────────────

    [Fact]
    public void Reject_terminates_and_records_note()
    {
        var sub = Submission("Sequential", 1,
            (1, Roles.StoreManager, "Pending"),
            (2, Roles.Supervisor, "Pending"));

        var outcome = ApprovalWorkflow.Apply(sub, ApprovalAction.Reject, "u1", Manager(Store), "Incomplete", Now);

        outcome.Status.Should().Be("Rejected");
        outcome.Event.Should().Be("FormRejected");
        sub.Status.Should().Be("Rejected");
        sub.ResolvedAt.Should().Be(Now);
        var step1 = sub.ApprovalSteps.Single(s => s.StepOrder == 1);
        step1.Action.Should().Be("Rejected");
        step1.Comments.Should().Be("Incomplete");
        sub.ApprovalSteps.Single(s => s.StepOrder == 2).Action.Should().Be("AutoClosed");
    }

    // ── Return keeps the step in place ────────────────────────────────────────

    [Fact]
    public void Return_keeps_current_step_for_resubmit()
    {
        var sub = Submission("Sequential", 1, (1, Roles.StoreManager, "Pending"));

        var outcome = ApprovalWorkflow.Apply(sub, ApprovalAction.Return, "u1", Manager(Store), "More detail", Now);

        outcome.Status.Should().Be("Returned");
        sub.Status.Should().Be("Returned");
        sub.CurrentStepOrder.Should().Be(1);   // retained
        sub.ResolvedAt.Should().BeNull();
    }

    // ── Guards ────────────────────────────────────────────────────────────────

    [Fact]
    public void Wrong_role_for_current_step_is_unauthorized()
    {
        var sub = Submission("Sequential", 1, (1, Roles.Supervisor, "Pending"));

        FluentActions.Invoking(() => ApprovalWorkflow.Apply(sub, ApprovalAction.Approve, "u1", Manager(Store), null, Now))
            .Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void Not_pending_is_invalid()
    {
        var sub = Submission("Sequential", 1, (1, Roles.StoreManager, "Pending"));
        sub.Status = "Approved";

        FluentActions.Invoking(() => ApprovalWorkflow.Apply(sub, ApprovalAction.Approve, "u1", Manager(Store), null, Now))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Approver_not_scoped_to_store_is_unauthorized()
    {
        var sub = Submission("Sequential", 1, (1, Roles.StoreManager, "Pending"));

        // Manager whose store is a different one than the submission's.
        FluentActions.Invoking(() => ApprovalWorkflow.Apply(sub, ApprovalAction.Approve, "u1", Manager(Guid.NewGuid()), null, Now))
            .Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void ResolveCurrentStep_returns_null_when_no_matching_pending_step()
    {
        var sub = Submission("Sequential", 1, (1, Roles.Supervisor, "Pending"));
        ApprovalWorkflow.ResolveCurrentStep(sub, Roles.StoreManager).Should().BeNull();
        ApprovalWorkflow.ResolveCurrentStep(sub, Roles.Supervisor).Should().NotBeNull();
    }
}
