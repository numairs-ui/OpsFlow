namespace OpsFlow.Api.Features.Dashboard.Shared;

/// <summary>The "today" UTC window every dashboard rollup scopes its task/deposit queries to.</summary>
internal readonly record struct DashboardWindow(DateTimeOffset Now, DateTimeOffset Start, DateTimeOffset End)
{
    public static DashboardWindow Today()
    {
        var now = DateTimeOffset.UtcNow;
        var start = new DateTimeOffset(now.Date, TimeSpan.Zero);
        return new DashboardWindow(now, start, start.AddDays(1));
    }
}
