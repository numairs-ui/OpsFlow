using Microsoft.EntityFrameworkCore;
using OpsFlow.Domain.Entities;
using OpsFlow.Infrastructure;
using System.Text.Json;

namespace OpsFlow.Api.Services;

public sealed class TemplateSeederService(
    IServiceScopeFactory scopeFactory,
    ILogger<TemplateSeederService> logger) : IHostedService
{
    private static readonly string TillCountFieldsJson = JsonSerializer.Serialize(new[]
    {
        new { id = "till_a", type = "Numeric", label = "Till A ($)", required = true, rangeMin = (double?)null, rangeMax = (double?)null, correctiveActionText = (string?)"Variance detected — record reason and manager initials", subItems = (object?)null },
        new { id = "till_b", type = "Numeric", label = "Till B ($)", required = true, rangeMin = (double?)null, rangeMax = (double?)null, correctiveActionText = (string?)"Variance detected — record reason and manager initials", subItems = (object?)null },
        new { id = "variance_note", type = "Text", label = "Variance Reason", required = false, rangeMin = (double?)null, rangeMax = (double?)null, correctiveActionText = (string?)null, subItems = (object?)null },
        new { id = "manager_initials", type = "Text", label = "Manager Initials", required = false, rangeMin = (double?)null, rangeMax = (double?)null, correctiveActionText = (string?)null, subItems = (object?)null }
    });

    private static readonly string MdogFieldsJson = JsonSerializer.Serialize(new[]
    {
        new { id = "dough_10in", type = "Numeric", label = "Dough 10\"", required = true, rangeMin = (double?)0d, rangeMax = (double?)null, correctiveActionText = (string?)null, subItems = (object?)null },
        new { id = "dough_12in", type = "Numeric", label = "Dough 12\"", required = true, rangeMin = (double?)0d, rangeMax = (double?)null, correctiveActionText = (string?)null, subItems = (object?)null },
        new { id = "dough_14in", type = "Numeric", label = "Dough 14\"", required = true, rangeMin = (double?)0d, rangeMax = (double?)null, correctiveActionText = (string?)null, subItems = (object?)null },
        new { id = "dough_16in", type = "Numeric", label = "Dough 16\"", required = true, rangeMin = (double?)0d, rangeMax = (double?)null, correctiveActionText = (string?)null, subItems = (object?)null },
        new { id = "walkin_temp", type = "Numeric", label = "Walk-In Temperature (°F)", required = true, rangeMin = (double?)null, rangeMax = (double?)56d, correctiveActionText = (string?)"Product > 56°F — return to refrigeration immediately", subItems = (object?)null }
    });

    public async Task StartAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var masterDb = scope.ServiceProvider.GetRequiredService<MasterDbContext>();
        var factory = scope.ServiceProvider.GetRequiredService<TenantDbContextFactory>();

        var tenants = await masterDb.Tenants.Where(t => t.IsActive).ToListAsync(ct);
        foreach (var tenant in tenants)
        {
            try { await SeedForTenantAsync(tenant.Id, factory, ct); }
            catch (Exception ex) { logger.LogError(ex, "System template seed failed for tenant {TenantId}", tenant.Id); }
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    private static async Task SeedForTenantAsync(string tenantId, TenantDbContextFactory factory, CancellationToken ct)
    {
        await using var db = await factory.CreateForTenantAsync(tenantId, ct);

        const string mdogName = "Daily MDOG Check";
        if (!await db.TaskTemplates.AnyAsync(t => t.Name == mdogName && t.Scope == "System", ct))
        {
            db.TaskTemplates.Add(new TaskTemplate
            {
                TenantId = tenantId,
                Name = mdogName,
                Description = "Daily dough inventory and walk-in temperature check",
                Category = "Inventory",
                Scope = "System",
                FieldsJson = MdogFieldsJson,
                CreatedByUserId = "system"
            });
        }

        const string tillName = "Till Count";
        if (!await db.TaskTemplates.AnyAsync(t => t.Name == tillName && t.Scope == "System", ct))
        {
            db.TaskTemplates.Add(new TaskTemplate
            {
                TenantId = tenantId,
                Name = tillName,
                Description = "Daily till count — record Till A and Till B amounts",
                Category = "Safe",
                Scope = "System",
                FieldsJson = TillCountFieldsJson,
                CreatedByUserId = "system"
            });
        }

        await db.SaveChangesAsync(ct);
    }
}
