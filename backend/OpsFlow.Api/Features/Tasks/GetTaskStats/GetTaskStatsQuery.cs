using MediatR;

namespace OpsFlow.Api.Features.Tasks.GetTaskStats;

internal sealed record GetTaskStatsQuery : IRequest<TaskStatsDto>;

internal sealed record TaskStatsDto(
    int OpenToday,
    int UpcomingCount,
    int OverdueCount,
    int CorrectiveActionCount,
    int CompletedToday,
    double CompletionRateToday);
