using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Jobs;
using OpsFlow.Domain.Entities;
using OpsFlow.Infrastructure;
using Xunit;

namespace OpsFlow.Tests.Unit.Tasks;

/// <summary>
/// DepositEscalationJob.FlagMissedDepositsAsync decides which active stores get a missed-deposit
/// flag for the current business day. Tested against an in-memory context with deterministic
/// deadlines (UTC timezone + a boundary deadline) so the result doesn't depend on wall-clock time.
/// </summary>
public sealed class DepositEscalationJobTests
{
    private const string TenantId = "t1";

    private static TenantDbContext NewDb() =>
        new(new DbContextOptionsBuilder<TenantDbContext>()
            .UseInMemoryDatabase($"deposit-{Guid.NewGuid():N}")
            .Options);

    private static Store SeedStore(TenantDbContext db, TimeOnly? deadline, string tz = "UTC")
    {
        var regionId = Guid.NewGuid();
        var store = new Store { Id = Guid.NewGuid(), TenantId = TenantId, RegionId = regionId, Name = "S", IsActive = true };
        db.Regions.Add(new Region { Id = regionId, TenantId = TenantId, Name = "R" });
        db.Stores.Add(store);
        db.StoreSettings.Add(new StoreSettings
        {
            StoreId = store.Id,
            TenantId = TenantId,
            TimezoneId = tz,
            DepositDeadlineLocalTime = deadline,
        });
        return store;
    }

    [Fact]
    public async Task FlagsStore_PastDeadline_WithNoDeposit()
    {
        await using var db = NewDb();
        // Deadline at UTC midnight → always "passed" regardless of when the test runs.
        var store = SeedStore(db, new TimeOnly(0, 0), tz: "UTC");
        await db.SaveChangesAsync();

        var count = await DepositEscalationJob.FlagMissedDepositsAsync(db, TenantId, DateTimeOffset.UtcNow, default);

        count.Should().Be(1);
        db.MissedDepositFlags.Should().ContainSingle(f => f.StoreId == store.Id);
    }

    [Fact]
    public async Task DoesNotFlag_WhenDepositLoggedToday()
    {
        await using var db = NewDb();
        var store = SeedStore(db, new TimeOnly(0, 0), tz: "UTC");
        db.DepositLogs.Add(new DepositLog
        {
            TenantId = TenantId,
            StoreId = store.Id,
            Amount = 100m,
            SubmittedByManagerId = "mgr",
            SubmittedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        var count = await DepositEscalationJob.FlagMissedDepositsAsync(db, TenantId, DateTimeOffset.UtcNow, default);

        count.Should().Be(0);
        db.MissedDepositFlags.Should().BeEmpty();
    }

    [Fact]
    public async Task DoesNotFlag_BeforeDeadline()
    {
        await using var db = NewDb();
        // Deadline one minute before "now" in UTC would pass; use end-of-day so it is effectively future.
        var store = SeedStore(db, new TimeOnly(23, 59, 59), tz: "UTC");
        await db.SaveChangesAsync();

        // Pin "now" to a morning time so the 23:59:59 deadline has not passed.
        var morning = new DateTimeOffset(DateTime.UtcNow.Date.AddHours(9), TimeSpan.Zero);
        var count = await DepositEscalationJob.FlagMissedDepositsAsync(db, TenantId, morning, default);

        count.Should().Be(0);
        db.MissedDepositFlags.Should().BeEmpty();
    }

    [Fact]
    public async Task IsIdempotent_DoesNotDoubleFlag()
    {
        await using var db = NewDb();
        SeedStore(db, new TimeOnly(0, 0), tz: "UTC");
        await db.SaveChangesAsync();

        var first = await DepositEscalationJob.FlagMissedDepositsAsync(db, TenantId, DateTimeOffset.UtcNow, default);
        var second = await DepositEscalationJob.FlagMissedDepositsAsync(db, TenantId, DateTimeOffset.UtcNow, default);

        first.Should().Be(1);
        second.Should().Be(0);
        db.MissedDepositFlags.Should().HaveCount(1);
    }
}
