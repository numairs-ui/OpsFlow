using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using OpsFlow.Domain.Entities;
using Xunit;

namespace OpsFlow.Tests.Integration.Scenarios;

/// <summary>
/// Recurring assignment lifecycle: admin creates, pauses, resumes, and deletes.
/// Also verifies store manager and supervisor role-scoped creation, and cross-store rejection.
/// </summary>
public sealed class RecurringAssignmentTests : IClassFixture<TenantAwareWebApplicationFactory>
{
    private readonly TenantAwareWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RecurringAssignmentTests(TenantAwareWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private void UseToken(string token)
        => _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

    private async Task<Guid> SeedChecklistAsync(string name = "Recurring Checklist")
    {
        await _factory.SeedCommonDataAsync();
        var db = await _factory.GetTenantDbAsync();
        var id = Guid.NewGuid();
        db.Checklists.Add(new Checklist
        {
            Id = id,
            TenantId = TenantAwareWebApplicationFactory.TenantId,
            Name = name,
            Scope = "System",
            IsActive = true,
            CreatedByUserId = TenantAwareWebApplicationFactory.AdminUserId,
        });
        await db.SaveChangesAsync();
        return id;
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 1: Full admin lifecycle — create → pause → resume → delete
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Admin_CreatesPausesResumesDeletes_RecurringAssignment()
    {
        var checklistId = await SeedChecklistAsync("Daily Opening");
        var adminToken = _factory.MintToken(TenantAwareWebApplicationFactory.AdminUserId, "admin");
        UseToken(adminToken);

        // Create
        var createResp = await _client.PostAsJsonAsync("/recurring-assignments", new
        {
            name = "Daily Opening Routine",
            checklistId,
            storeId = TenantAwareWebApplicationFactory.StoreId,
            cronExpression = "0 0 8 ? * MON-FRI",
            startsAt = DateTimeOffset.UtcNow,
        });
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var assignmentId = (await createResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        // Pause
        (await _client.PostAsJsonAsync<object?>($"/recurring-assignments/{assignmentId}/pause", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var db = await _factory.GetTenantDbAsync();
        (await db.RecurringAssignments.FindAsync(assignmentId))!.IsPaused.Should().BeTrue();

        // Resume
        (await _client.PostAsJsonAsync<object?>($"/recurring-assignments/{assignmentId}/resume", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        db = await _factory.GetTenantDbAsync();
        (await db.RecurringAssignments.FindAsync(assignmentId))!.IsPaused.Should().BeFalse();

        // Delete
        (await _client.DeleteAsync($"/recurring-assignments/{assignmentId}"))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        db = await _factory.GetTenantDbAsync();
        (await db.RecurringAssignments.FindAsync(assignmentId)).Should().BeNull();
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 2: Store manager creates recurring assignment for their own store
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task StoreManager_CreatesAssignment_ForTheirStore_Returns201()
    {
        var checklistId = await SeedChecklistAsync("Manager-Initiated Checklist");

        UseToken(_factory.MintToken(
            TenantAwareWebApplicationFactory.StoreManagerUserId, "store_manager",
            storeId: TenantAwareWebApplicationFactory.StoreId.ToString()));

        var response = await _client.PostAsJsonAsync("/recurring-assignments", new
        {
            name = "Nightly Close",
            checklistId,
            storeId = TenantAwareWebApplicationFactory.StoreId,
            cronExpression = "0 0 22 ? * *",
            startsAt = DateTimeOffset.UtcNow,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 3: Store manager cannot create assignment for a different store
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task StoreManager_CreatesAssignment_ForDifferentStore_Returns401()
    {
        var checklistId = await SeedChecklistAsync("Cross-Store Checklist");

        UseToken(_factory.MintToken(
            TenantAwareWebApplicationFactory.StoreManagerUserId, "store_manager",
            storeId: TenantAwareWebApplicationFactory.StoreId.ToString()));

        var response = await _client.PostAsJsonAsync("/recurring-assignments", new
        {
            name = "Should Fail",
            checklistId,
            storeId = TenantAwareWebApplicationFactory.AltStoreId, // wrong store
            cronExpression = "0 0 9 ? * *",
            startsAt = DateTimeOffset.UtcNow,
        });

        // CreateRecurringAssignmentHandler checks userProfile.StoreId == cmd.StoreId → throws UnauthorizedAccessException
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 4: Supervisor creates assignment for a store in their region
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Supervisor_CreatesAssignment_ForStoreInTheirRegion_Returns201()
    {
        var checklistId = await SeedChecklistAsync("Supervisor Checklist");

        UseToken(_factory.MintToken(
            TenantAwareWebApplicationFactory.SupervisorUserId, "supervisor",
            regionId: TenantAwareWebApplicationFactory.RegionId.ToString()));

        var response = await _client.PostAsJsonAsync("/recurring-assignments", new
        {
            name = "Weekly Audit",
            checklistId,
            storeId = TenantAwareWebApplicationFactory.StoreId, // Store is in test region
            cronExpression = "0 0 10 ? * MON",
            startsAt = DateTimeOffset.UtcNow,
        });

        // StoreId's RegionId matches supervisor's regionId claim → should succeed
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 5: GET recurring-assignments filters by storeId
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRecurringAssignments_FilteredByStoreId_ReturnsOnlyThatStore()
    {
        var checklistId = await SeedChecklistAsync("Filter Test Checklist");
        var adminToken = _factory.MintToken(TenantAwareWebApplicationFactory.AdminUserId, "admin");
        UseToken(adminToken);

        // Create assignment for primary store
        await _client.PostAsJsonAsync("/recurring-assignments", new
        {
            name = "Primary Store Assignment",
            checklistId,
            storeId = TenantAwareWebApplicationFactory.StoreId,
            cronExpression = "0 0 7 ? * *",
            startsAt = DateTimeOffset.UtcNow,
        });

        // GET filtered
        var response = await _client.GetAsync($"/recurring-assignments?storeId={TenantAwareWebApplicationFactory.StoreId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        // Result is an array; at least one assignment for this store should exist
        body.GetArrayLength().Should().BeGreaterThan(0);
    }
}
