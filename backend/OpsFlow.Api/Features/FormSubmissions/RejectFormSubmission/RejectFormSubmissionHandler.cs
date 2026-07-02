using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Hubs;
using OpsFlow.Api.Security;
using OpsFlow.Domain.Forms;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.FormSubmissions.RejectFormSubmission;

internal sealed class RejectFormSubmissionHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor,
    IHubContext<TaskBoardHub> hub) : IRequestHandler<RejectFormSubmissionCommand>
{
    public async Task Handle(RejectFormSubmissionCommand cmd, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var spec = user.ToCaller().Scope();

        await using var db = await factory.CreateAsync(ct);

        var submission = await db.FormSubmissions
            .Include(s => s.ApprovalSteps)
            .Include(s => s.FormTemplate)
            .Include(s => s.Store)
            .FirstOrDefaultAsync(s => s.Id == cmd.Id, ct)
            ?? throw new KeyNotFoundException($"Form submission {cmd.Id} not found.");

        var outcome = ApprovalWorkflow.Apply(
            submission, ApprovalAction.Reject, user.GetUserId(), spec, cmd.Reason, DateTimeOffset.UtcNow);

        await db.SaveChangesAsync(ct);

        await hub.Clients.Group($"store-{submission.StoreId}").SendAsync(
            "FormSubmissionUpdated",
            new { submission.Id, submission.Status, Event = outcome.Event, Reason = cmd.Reason },
            ct);
    }
}
