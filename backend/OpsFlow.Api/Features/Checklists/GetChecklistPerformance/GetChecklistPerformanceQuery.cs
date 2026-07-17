using MediatR;

namespace OpsFlow.Api.Features.Checklists.GetChecklistPerformance;

internal sealed record GetChecklistPerformanceQuery(int Days = 30) : IRequest<ChecklistPerformanceDto>;

internal sealed record ChecklistPerformanceDto(
    double AverageScorePercent,
    int ScoredCompletionCount,
    int TotalCompletionCount,
    int FailingCompletionCount,
    List<DailyScoreDto> ScoreTrend,
    List<RegionChecklistScoreDto> RegionBreakdown,
    List<StoreChecklistScoreDto> StoreBreakdown,
    List<RecentCompletionDto> RecentCompletions);

internal sealed record DailyScoreDto(DateOnly Date, double AverageScorePercent, int CompletionCount);

internal sealed record RegionChecklistScoreDto(
    Guid RegionId, string RegionName, int StoreCount, double AverageScorePercent, int FailingCount, int CompletionCount);

internal sealed record StoreChecklistScoreDto(
    Guid StoreId, string StoreName, double AverageScorePercent, int CompletionCount, int FailingCount, DateTimeOffset? LastCompletedAt);

internal sealed record RecentCompletionDto(
    Guid TaskInstanceId, Guid StoreId, string StoreName, string ChecklistName, decimal? ScorePercent, DateTimeOffset CompletedAt, string? CompletedByUserId);
