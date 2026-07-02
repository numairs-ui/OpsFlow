using Microsoft.EntityFrameworkCore;
using OpsFlow.Infrastructure;
using Quartz;

namespace OpsFlow.Api.Jobs;

/// <summary>
/// Base for Quartz jobs that run the same per-tenant unit of work across every active tenant.
/// A failure for one tenant is logged and does not stop the others.
/// </summary>
internal abstract class TenantIteratingJob(IServiceScopeFactory scopeFactory, ILogger logger) : IJob
{
    protected abstract string JobName { get; }

    protected abstract Task RunForTenantAsync(string tenantId, IServiceProvider services, CancellationToken ct);

    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;

        await using var scope = scopeFactory.CreateAsyncScope();
        var masterDb = scope.ServiceProvider.GetRequiredService<MasterDbContext>();

        var tenants = await masterDb.Tenants.Where(t => t.IsActive).ToListAsync(ct);

        foreach (var tenant in tenants)
        {
            try
            {
                await RunForTenantAsync(tenant.Id, scope.ServiceProvider, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{JobName} failed for tenant {TenantId}", JobName, tenant.Id);
            }
        }
    }
}
