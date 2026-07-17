using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Security;
using OpsFlow.Domain.Authorization;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Checklists.GetChecklistPerformance;

internal sealed class GetChecklistPerformanceHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<GetChecklistPerformanceQuery, ChecklistPerformanceDto>
{
    // No per-tenant/per-checklist config for this yet — a fixed v1 threshold, easy to make
    // configurable later if needed.
    private const decimal FailThreshold = 70m;

    public async Task<ChecklistPerformanceDto> Handle(GetChecklistPerformanceQuery query, CancellationToken ct)
    {
        var spec = httpContextAccessor.HttpContext!.User.ToCaller().Scope();

        await using var db = await factory.CreateAsync(ct);

        var allStores = await db.Stores
            .Where(s => s.IsActive)
            .WhereStoreInScope(spec, s => s.Id, s => s.RegionId)
            .Select(s => new { s.Id, s.Name, s.RegionId })
            .ToListAsync(ct);

        var allRegions = await db.Regions
            .Where(r => r.IsActive && allStores.Select(s => s.RegionId).Contains(r.Id))
            .Select(r => new { r.Id, r.Name })
            .ToListAsync(ct);

        if (allStores.Count == 0)
            return new ChecklistPerformanceDto(0, 0, 0, 0, [], [], [], []);

        var since = DateTimeOffset.UtcNow.AddDays(-Math.Max(1, query.Days));

        var completions = await db.TaskCompletions
            .Include(c => c.TaskInstance)!.ThenInclude(t => t!.Checklist)
            .Where(c => c.CompletedAt >= since && c.TaskInstance != null && c.TaskInstance.ChecklistId != null)
            .WhereStoreInScope(spec, c => c.TaskInstance!.StoreId, c => c.TaskInstance!.Store!.RegionId)
            .OrderByDescending(c => c.CompletedAt)
            .Select(c => new
            {
                c.TaskInstanceId,
                c.CompositeScorePercent,
                c.CompletedAt,
                c.CompletedByUserId,
                StoreId = c.TaskInstance!.StoreId,
                ChecklistName = c.TaskInstance.Checklist!.Name,
            })
            .ToListAsync(ct);

        var storesById = allStores.ToDictionary(s => s.Id);
        var scored = completions.Where(c => c.CompositeScorePercent.HasValue).ToList();

        var averageScorePercent = scored.Count > 0 ? (double)scored.Average(c => c.CompositeScorePercent!.Value) : 0;
        var failingCount = scored.Count(c => c.CompositeScorePercent!.Value < FailThreshold);

        var scoreTrend = scored
            .GroupBy(c => DateOnly.FromDateTime(c.CompletedAt.UtcDateTime))
            .OrderBy(g => g.Key)
            .Select(g => new DailyScoreDto(g.Key, (double)g.Average(c => c.CompositeScorePercent!.Value), g.Count()))
            .ToList();

        var storeBreakdown = allStores.Select(s =>
        {
            var storeCompletions = scored.Where(c => c.StoreId == s.Id).ToList();
            var lastCompletedAt = completions.Where(c => c.StoreId == s.Id).Select(c => (DateTimeOffset?)c.CompletedAt).FirstOrDefault();
            return new StoreChecklistScoreDto(
                s.Id, s.Name,
                storeCompletions.Count > 0 ? (double)storeCompletions.Average(c => c.CompositeScorePercent!.Value) : 0,
                storeCompletions.Count,
                storeCompletions.Count(c => c.CompositeScorePercent!.Value < FailThreshold),
                lastCompletedAt);
        }).ToList();

        var regionBreakdown = allRegions.Select(r =>
        {
            var regionStoreIds = allStores.Where(s => s.RegionId == r.Id).Select(s => s.Id).ToHashSet();
            var regionCompletions = scored.Where(c => regionStoreIds.Contains(c.StoreId)).ToList();
            return new RegionChecklistScoreDto(
                r.Id, r.Name, regionStoreIds.Count,
                regionCompletions.Count > 0 ? (double)regionCompletions.Average(c => c.CompositeScorePercent!.Value) : 0,
                regionCompletions.Count(c => c.CompositeScorePercent!.Value < FailThreshold),
                regionCompletions.Count);
        }).ToList();

        var recentCompletions = completions
            .Take(20)
            .Select(c => new RecentCompletionDto(
                c.TaskInstanceId, c.StoreId, storesById.GetValueOrDefault(c.StoreId)?.Name ?? "",
                c.ChecklistName ?? "Checklist", c.CompositeScorePercent, c.CompletedAt, c.CompletedByUserId))
            .ToList();

        return new ChecklistPerformanceDto(
            Math.Round(averageScorePercent, 1),
            scored.Count,
            completions.Count,
            failingCount,
            scoreTrend,
            regionBreakdown,
            storeBreakdown,
            recentCompletions);
    }
}
