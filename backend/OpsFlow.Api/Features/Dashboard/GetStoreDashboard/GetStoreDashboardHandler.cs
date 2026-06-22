using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Dashboard.GetStoreDashboard;

internal sealed class GetStoreDashboardHandler(
    TenantDbContextFactory factory) : IRequestHandler<GetStoreDashboardQuery, StoreDashboardDto>
{
    public async Task<StoreDashboardDto> Handle(GetStoreDashboardQuery query, CancellationToken ct)
    {
        await using var db = await factory.CreateAsync(ct);

        var now        = DateTimeOffset.UtcNow;
        var todayStart = new DateTimeOffset(now.Date, TimeSpan.Zero);
        var todayEnd   = todayStart.AddDays(1);

        // All tasks for today for this store
        var tasks = await db.TaskInstances
            .Include(t => t.Checklist)
            .Where(t => t.StoreId == query.StoreId
                && t.DueAt >= todayStart
                && t.DueAt < todayEnd)
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
                (int)(now - t.DueAt).TotalMinutes))
            .ToList();

        // Today's deposit
        var deposit = await db.DepositLogs
            .Where(d => d.StoreId == query.StoreId && d.SubmittedAt >= todayStart && d.SubmittedAt < todayEnd)
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
