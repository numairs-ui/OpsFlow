using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Users.GetUserActivity;

internal sealed class GetUserActivityHandler(TenantDbContextFactory factory)
    : IRequestHandler<GetUserActivityQuery, List<UserActivityDto>>
{
    public async Task<List<UserActivityDto>> Handle(GetUserActivityQuery query, CancellationToken ct)
    {
        await using var db = await factory.CreateAsync(ct);
        var uid = query.UserId;

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
