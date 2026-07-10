using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Jobs;
using OpsFlow.Domain.Entities;
using OpsFlow.Infrastructure;
using Xunit;

namespace OpsFlow.Tests.Unit.Tasks;

/// <summary>
/// GenerateTaskInstancesJob fans a recurring assignment out to one task instance per target store per
/// firing, and must not double-generate. Uses an every-minute cron so the next firing is always inside
/// the [now, now+1h) window, making the result deterministic.
/// </summary>
public sealed class GenerateTaskInstancesJobTests
{
    private const string TenantId = "t1";

    private static TenantDbContext NewDb() =>
        new(new DbContextOptionsBuilder<TenantDbContext>()
            .UseInMemoryDatabase($"generate-{Guid.NewGuid():N}")
            .Options);

    private static (Guid checklistId, Guid s1, Guid s2) Seed(TenantDbContext db, int storeCount)
    {
        var regionId = Guid.NewGuid();
        db.Regions.Add(new Region { Id = regionId, TenantId = TenantId, Name = "R" });

        var checklistId = Guid.NewGuid();
        db.Checklists.Add(new Checklist { Id = checklistId, TenantId = TenantId, Name = "C", Scope = "System", IsActive = true, CreatedByUserId = "admin" });

        var s1 = Guid.NewGuid();
        var s2 = Guid.NewGuid();
        db.Stores.Add(new Store { Id = s1, TenantId = TenantId, RegionId = regionId, Name = "S1", IsActive = true });
        if (storeCount > 1) db.Stores.Add(new Store { Id = s2, TenantId = TenantId, RegionId = regionId, Name = "S2", IsActive = true });

        var targets = new List<RecurringAssignmentStore> { new() { StoreId = s1 } };
        if (storeCount > 1) targets.Add(new RecurringAssignmentStore { StoreId = s2 });

        db.RecurringAssignments.Add(new RecurringAssignment
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            Name = "A",
            ChecklistId = checklistId,
            CronExpression = "0 * * * * ?", // every minute
            StartsAt = DateTimeOffset.UtcNow.AddDays(-1),
            CreatedByUserId = "admin",
            TargetStores = targets,
        });

        return (checklistId, s1, s2);
    }

    [Fact]
    public async Task FansOut_OneInstancePerTargetStore()
    {
        await using var db = NewDb();
        var (_, s1, s2) = Seed(db, storeCount: 2);
        await db.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;
        var created = await GenerateTaskInstancesJob.GenerateAsync(db, TenantId, now, default);

        created.Should().Be(2);
        db.TaskInstances.Select(t => t.StoreId).Should().BeEquivalentTo([s1, s2]);
    }

    [Fact]
    public async Task IsIdempotent_DoesNotDoubleGenerate_PerStore()
    {
        await using var db = NewDb();
        Seed(db, storeCount: 2);
        await db.SaveChangesAsync();

        // Same "now" → same dueAt → the second run must add nothing (this is the dedup-bug guard:
        // without the StoreId condition, the second store would be skipped on the first run instead).
        var now = new DateTimeOffset(DateTime.UtcNow.Date.AddHours(8), TimeSpan.Zero);
        var first = await GenerateTaskInstancesJob.GenerateAsync(db, TenantId, now, default);
        var second = await GenerateTaskInstancesJob.GenerateAsync(db, TenantId, now, default);

        first.Should().Be(2);
        second.Should().Be(0);
        db.TaskInstances.Should().HaveCount(2);
    }
}
