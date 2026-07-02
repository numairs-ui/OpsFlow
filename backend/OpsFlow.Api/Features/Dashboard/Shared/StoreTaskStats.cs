namespace OpsFlow.Api.Features.Dashboard.Shared;

/// <summary>Per-store task counts for a window, classified by the status groupings the dashboards use.</summary>
internal sealed record StoreTaskStats(Guid StoreId, int Total, int Completed, int Open, int Overdue, int Corrective)
{
    public double CompletionRate => Total > 0 ? (double)Completed / Total : 0;
    public double CorrectiveRate => Total > 0 ? (double)Corrective / Total : 0;
}
