using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Features.Dashboard.Shared;
using OpsFlow.Api.Security;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Dashboard.GetStoreDashboard;

internal sealed class GetStoreDashboardHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<GetStoreDashboardQuery, StoreDashboardDto>
{
    public async Task<StoreDashboardDto> Handle(GetStoreDashboardQuery query, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var spec = user.ToCaller().Scope();

        await using var db = await factory.CreateAsync(ct);

        // Only those who can view this store may see its dashboard.
        var store = await db.Stores.FindAsync([query.StoreId], ct)
            ?? throw new KeyNotFoundException($"Store {query.StoreId} not found.");
        var assigned = spec.IsStoreScoped
            && await db.UserStoreAssignments.AnyAsync(a => a.UserId == user.GetUserId() && a.StoreId == query.StoreId, ct);
        spec.AssertCanViewStore(store.RegionId, store.Id, assigned);

        var window = DashboardWindow.Today();

        // All tasks for today for this store
        var tasks = await db.TaskInstances
            .Include(t => t.Checklist)
            .Where(t => t.StoreId == query.StoreId
                && t.DueAt >= window.Start
                && t.DueAt < window.End)
            .Select(t => new { t.Id, t.Status, t.DueAt, Name = t.Checklist != null ? t.Checklist.Name : "Task" })
            .ToListAsync(ct);

        var total = tasks.Count(t => t.Status != "Cancelled" && t.Status != "Deferred");
        var completed = tasks.Count(t => t.Status == "Completed" || t.Status == "Verified");
        var completionRate = total > 0 ? (double)completed / total : 0;

        var open = tasks.Count(t => t.Status == "Pending" || t.Status == "InProgress");
        var overdue = tasks.Count(t => t.Status == "Overdue" || t.Status == "CorrectiveActionRaised");
        var corrective = tasks.Count(t => t.Status == "CorrectiveActionRaised");

        var overdueTasks = tasks
            .Where(t => t.Status == "Overdue" || t.Status == "CorrectiveActionRaised")
            .OrderBy(t => t.DueAt)
            .Select(t => new OverdueTaskSummary(
                t.Id,
                t.Name,
                t.DueAt,
                t.Status,
                (int)(window.Now - t.DueAt).TotalMinutes))
            .ToList();

        // Today's deposit
        var deposit = await db.DepositLogs
            .Where(d => d.StoreId == query.StoreId && d.SubmittedAt >= window.Start && d.SubmittedAt < window.End)
            .Select(d => new { d.Amount })
            .FirstOrDefaultAsync(ct);

        return new StoreDashboardDto(
            completionRate,
            open,
            overdue,
            corrective,
            deposit != null,
            deposit?.Amount,
            overdueTasks);
    }
}
