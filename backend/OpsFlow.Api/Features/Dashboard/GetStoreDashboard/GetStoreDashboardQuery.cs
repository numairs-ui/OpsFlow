using MediatR;

namespace OpsFlow.Api.Features.Dashboard.GetStoreDashboard;

internal sealed record GetStoreDashboardQuery(Guid StoreId) : IRequest<StoreDashboardDto>;

internal sealed record StoreDashboardDto(
    double CompletionRate,
    int OpenCount,
    int OverdueCount,
    int ActiveCorrectiveActionCount,
    bool DepositLoggedToday,
    decimal? DepositAmount,
    List<OverdueTaskSummary> OverdueTasks);

internal sealed record OverdueTaskSummary(
    Guid Id,
    string Name,
    DateTimeOffset DueAt,
    string Status,
    int ElapsedMinutes);
