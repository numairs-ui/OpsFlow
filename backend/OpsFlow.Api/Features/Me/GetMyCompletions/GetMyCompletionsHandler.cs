using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Me.GetMyCompletions;

internal sealed class GetMyCompletionsHandler(
    TenantDbContextFactory factory) : IRequestHandler<GetMyCompletionsQuery, List<MyCompletionDto>>
{
    public async Task<List<MyCompletionDto>> Handle(GetMyCompletionsQuery query, CancellationToken ct)
    {
        await using var db = await factory.CreateAsync(ct);

        var since = DateTimeOffset.UtcNow.AddDays(-query.Days);

        var completions = await db.TaskCompletions
            .Include(c => c.TaskInstance)
                .ThenInclude(t => t!.Checklist)
            .Where(c => c.CompletedByUserId == query.UserId && c.CompletedAt >= since)
            .OrderByDescending(c => c.CompletedAt)
            .Take(50)
            .Select(c => new MyCompletionDto(
                c.TaskInstanceId,
                c.TaskInstance != null && c.TaskInstance.Checklist != null
                    ? c.TaskInstance.Checklist.Name
                    : "Task",
                c.TaskInstance != null ? c.TaskInstance.Status : "Completed",
                c.CompletedAt))
            .ToListAsync(ct);

        return completions;
    }
}
