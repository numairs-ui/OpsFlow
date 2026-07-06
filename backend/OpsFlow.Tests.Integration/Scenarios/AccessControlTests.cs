using FluentAssertions;
using OpsFlow.Domain.Entities;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace OpsFlow.Tests.Integration.Scenarios;

/// <summary>
/// Cross-cutting authorization tests: verifies that unauthenticated requests are rejected,
/// tokens signed with the wrong key fail, and store-scoped endpoints enforce store membership.
/// Also tests the dashboard endpoints for all three role levels.
/// </summary>
public sealed class AccessControlTests : IClassFixture<TenantAwareWebApplicationFactory>
{
    private readonly TenantAwareWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AccessControlTests(TenantAwareWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private void UseToken(string token)
        => _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

    private void ClearToken()
        => _client.DefaultRequestHeaders.Authorization = null;

    // ────────────────────────────────────────────────────────────────
    // Scenario 1: Key protected endpoints require authentication
    // ────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("GET", "/tasks")]
    [InlineData("GET", "/checklists")]
    [InlineData("GET", "/form-submissions")]
    [InlineData("GET", "/form-templates")]
    [InlineData("GET", "/recurring-assignments")]
    [InlineData("GET", "/users")]
    [InlineData("GET", "/regions")]
    [InlineData("GET", "/tenant/settings")]
    public async Task Unauthenticated_Request_Returns401(string method, string route)
    {
        ClearToken();
        var request = new HttpRequestMessage(new HttpMethod(method), route);
        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            because: $"{method} {route} requires a valid token");
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 2: Token signed with wrong key is rejected
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TokenSignedWithWrongKey_Returns401()
    {
        var wrongSecret = "wrong-secret-that-is-long-enough-for-hmac256";
        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(wrongSecret));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(
            key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            "opsflow-test", "opsflow-test",
            new[] { new System.Security.Claims.Claim("sub", "fake-user"), new System.Security.Claims.Claim("tenantId", "test-tenant") },
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: creds);
        var tokenStr = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);

        UseToken(tokenStr);
        var response = await _client.GetAsync("/tasks");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 3: Store employee cannot access today's tasks for a different store
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Employee_AccessingDifferentStoreTodayTasks_Returns401()
    {
        await _factory.SeedCommonDataAsync();

        UseToken(_factory.MintToken(
            TenantAwareWebApplicationFactory.EmployeeUserId, "store_employee",
            storeId: TenantAwareWebApplicationFactory.StoreId.ToString()));

        var response = await _client.GetAsync(
            $"/stores/{TenantAwareWebApplicationFactory.AltStoreId}/tasks/today");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 4: GET /dashboard/store returns 200 for any authenticated user
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStoreDashboard_AsAuthenticatedUser_Returns200()
    {
        await _factory.SeedCommonDataAsync();
        UseToken(_factory.MintToken(TenantAwareWebApplicationFactory.AdminUserId, "super_admin"));

        var response = await _client.GetAsync(
            $"/dashboard/store/{TenantAwareWebApplicationFactory.StoreId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 5: GET /dashboard/region returns 200 for supervisor
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRegionDashboard_AsSupervisor_Returns200()
    {
        await _factory.SeedCommonDataAsync();
        UseToken(_factory.MintToken(
            TenantAwareWebApplicationFactory.SupervisorUserId, "supervisor",
            regionId: TenantAwareWebApplicationFactory.RegionId.ToString()));

        var response = await _client.GetAsync(
            $"/dashboard/region/{TenantAwareWebApplicationFactory.RegionId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 6: GET /dashboard/system returns 200 for admin
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSystemDashboard_AsAdmin_Returns200()
    {
        await _factory.SeedCommonDataAsync();
        UseToken(_factory.MintToken(TenantAwareWebApplicationFactory.AdminUserId, "super_admin"));

        var response = await _client.GetAsync("/dashboard/system");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 7: Checklist with empty name is rejected with 400
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateChecklist_WithEmptyName_Returns400()
    {
        await _factory.SeedCommonDataAsync();
        UseToken(_factory.MintToken(TenantAwareWebApplicationFactory.AdminUserId, "super_admin"));

        var response = await _client.PostAsJsonAsync("/checklists", new
        {
            name = "",
            scope = "System",
            items = Array.Empty<object>(),
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 8: Employee can access their own store's today tasks
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Employee_GetTodayTasks_ForOwnStore_Returns200()
    {
        await _factory.SeedCommonDataAsync();

        var db = await _factory.GetTenantDbAsync();
        var checklistId = Guid.NewGuid();
        db.Checklists.Add(new Checklist
        {
            Id = checklistId,
            TenantId = TenantAwareWebApplicationFactory.TenantId,
            Name = $"AccessControl-{checklistId}",
            Scope = "System",
            IsActive = true,
            CreatedByUserId = TenantAwareWebApplicationFactory.AdminUserId,
        });
        db.TaskInstances.Add(new TaskInstance
        {
            TenantId = TenantAwareWebApplicationFactory.TenantId,
            ChecklistId = checklistId,
            StoreId = TenantAwareWebApplicationFactory.StoreId,
            DueAt = DateTimeOffset.UtcNow.Date,
            CreatedByUserId = TenantAwareWebApplicationFactory.AdminUserId,
        });
        await db.SaveChangesAsync();

        UseToken(_factory.MintToken(
            TenantAwareWebApplicationFactory.EmployeeUserId, "store_employee",
            storeId: TenantAwareWebApplicationFactory.StoreId.ToString()));

        var response = await _client.GetAsync(
            $"/stores/{TenantAwareWebApplicationFactory.StoreId}/tasks/today");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 9: Region-scoped admin only sees regions in its own set
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRegions_AsRegionScopedAdmin_OnlyReturnsAssignedRegions()
    {
        await _factory.SeedCommonDataAsync();

        var db = await _factory.GetTenantDbAsync();
        var otherRegionId = Guid.NewGuid();
        db.Regions.Add(new Region
        {
            Id = otherRegionId,
            TenantId = TenantAwareWebApplicationFactory.TenantId,
            Name = $"Unassigned-{otherRegionId}",
        });
        await db.SaveChangesAsync();

        UseToken(_factory.MintMultiRegionToken(
            TenantAwareWebApplicationFactory.AdminUserId, "admin", null,
            TenantAwareWebApplicationFactory.RegionId.ToString()));

        var response = await _client.GetAsync("/regions?activeOnly=false");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var regions = await response.Content.ReadFromJsonAsync<List<RegionDto>>();
        regions.Should().NotBeNull();
        regions!.Select(r => r.Id).Should().Contain(TenantAwareWebApplicationFactory.RegionId);
        regions.Select(r => r.Id).Should().NotContain(otherRegionId,
            because: "a region-scoped admin must not see regions outside its assigned set");
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 10: Region-scoped admin cannot create, edit, or deactivate regions
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateRegion_AsRegionScopedAdmin_Returns401()
    {
        await _factory.SeedCommonDataAsync();
        UseToken(_factory.MintMultiRegionToken(
            TenantAwareWebApplicationFactory.AdminUserId, "admin", null,
            TenantAwareWebApplicationFactory.RegionId.ToString()));

        var response = await _client.PostAsJsonAsync("/regions", new { name = "New Region" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateRegion_AsRegionScopedAdmin_Returns401()
    {
        await _factory.SeedCommonDataAsync();
        UseToken(_factory.MintMultiRegionToken(
            TenantAwareWebApplicationFactory.AdminUserId, "admin", null,
            TenantAwareWebApplicationFactory.RegionId.ToString()));

        var response = await _client.PutAsJsonAsync(
            $"/regions/{TenantAwareWebApplicationFactory.RegionId}",
            new { name = "Renamed Region" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeactivateRegion_AsRegionScopedAdmin_Returns401()
    {
        await _factory.SeedCommonDataAsync();
        UseToken(_factory.MintMultiRegionToken(
            TenantAwareWebApplicationFactory.AdminUserId, "admin", null,
            TenantAwareWebApplicationFactory.RegionId.ToString()));

        var response = await _client.PostAsync(
            $"/regions/{TenantAwareWebApplicationFactory.RegionId}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 11: Region-scoped admin only sees stores in its own region set
    // (regression: an unscoped store list previously made "Store Roster" pick an
    // out-of-region store first and 401 on the roster fetch)
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStores_AsRegionScopedAdmin_OnlyReturnsStoresInAssignedRegions()
    {
        await _factory.SeedCommonDataAsync();

        var db = await _factory.GetTenantDbAsync();
        var otherRegionId = Guid.NewGuid();
        var otherStoreId = Guid.NewGuid();
        db.Regions.Add(new Region { Id = otherRegionId, TenantId = TenantAwareWebApplicationFactory.TenantId, Name = "Unassigned Region" });
        db.Stores.Add(new Store { Id = otherStoreId, TenantId = TenantAwareWebApplicationFactory.TenantId, RegionId = otherRegionId, Name = "Unassigned Store" });
        await db.SaveChangesAsync();

        UseToken(_factory.MintMultiRegionToken(
            TenantAwareWebApplicationFactory.AdminUserId, "admin", null,
            TenantAwareWebApplicationFactory.RegionId.ToString()));

        var response = await _client.GetAsync("/stores");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var stores = await response.Content.ReadFromJsonAsync<List<StoreDto>>();
        stores.Should().NotBeNull();
        stores!.Select(s => s.Id).Should().Contain(TenantAwareWebApplicationFactory.StoreId);
        stores.Select(s => s.Id).Should().NotContain(otherStoreId,
            because: "a region-scoped admin must not see stores outside its assigned regions, " +
                     "or the Store Roster page can pick one it's then denied access to");
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 12: single-item fetch-by-id handlers all deny/hide out-of-scope data
    // for a region-scoped admin — the same bug class as GetChecklistHandler, found in
    // a follow-up audit (see project_scope_auth_audit_pending memory) and fixed here.
    // ────────────────────────────────────────────────────────────────

    private async Task<(Guid RegionId, Guid StoreId)> SeedForeignStoreAsync()
    {
        var db = await _factory.GetTenantDbAsync();
        var regionId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        db.Regions.Add(new Region { Id = regionId, TenantId = TenantAwareWebApplicationFactory.TenantId, Name = $"Foreign-{regionId}" });
        db.Stores.Add(new Store { Id = storeId, TenantId = TenantAwareWebApplicationFactory.TenantId, RegionId = regionId, Name = $"Foreign Store {storeId}" });
        await db.SaveChangesAsync();
        return (regionId, storeId);
    }

    private void UseRegionScopedAdmin() =>
        UseToken(_factory.MintMultiRegionToken(
            TenantAwareWebApplicationFactory.AdminUserId, "admin", null,
            TenantAwareWebApplicationFactory.RegionId.ToString()));

    [Fact]
    public async Task GetTemplate_AsRegionScopedAdmin_ForOutOfRegionStoreScopedTemplate_DoesNotReturnIt()
    {
        await _factory.SeedCommonDataAsync();
        var (_, foreignStoreId) = await SeedForeignStoreAsync();

        var db = await _factory.GetTenantDbAsync();
        var templateId = Guid.NewGuid();
        db.TaskTemplates.Add(new TaskTemplate
        {
            Id = templateId, TenantId = TenantAwareWebApplicationFactory.TenantId,
            Name = "Foreign Template", Category = "Operations", Scope = "Store",
            StoreId = foreignStoreId, CreatedByUserId = TenantAwareWebApplicationFactory.SuperAdminUserId,
        });
        await db.SaveChangesAsync();

        UseRegionScopedAdmin();
        var response = await _client.GetAsync($"/templates/{templateId}");

        response.StatusCode.Should().NotBe(HttpStatusCode.OK,
            because: "a region-scoped admin must not see a Store-scope template outside its assigned regions");
    }

    [Fact]
    public async Task GetFormTemplate_AsRegionScopedAdmin_ForOutOfRegionStoreScopedFormTemplate_DoesNotReturnIt()
    {
        await _factory.SeedCommonDataAsync();
        var (_, foreignStoreId) = await SeedForeignStoreAsync();

        var db = await _factory.GetTenantDbAsync();
        var templateId = Guid.NewGuid();
        db.FormTemplates.Add(new FormTemplate
        {
            Id = templateId, TenantId = TenantAwareWebApplicationFactory.TenantId,
            Name = "Foreign Form Template", Scope = "Store", StoreId = foreignStoreId,
            PropagationType = "NotificationOnly", CreatedByUserId = TenantAwareWebApplicationFactory.SuperAdminUserId,
        });
        await db.SaveChangesAsync();

        UseRegionScopedAdmin();
        var response = await _client.GetAsync($"/form-templates/{templateId}");

        response.StatusCode.Should().NotBe(HttpStatusCode.OK,
            because: "a region-scoped admin must not see a Store-scope form template outside its assigned regions");
    }

    [Fact]
    public async Task GetFormSubmission_AsRegionScopedAdmin_ForOutOfRegionStore_Returns401()
    {
        await _factory.SeedCommonDataAsync();
        var (_, foreignStoreId) = await SeedForeignStoreAsync();

        var db = await _factory.GetTenantDbAsync();
        var submissionId = Guid.NewGuid();
        db.FormSubmissions.Add(new FormSubmission
        {
            Id = submissionId, TenantId = TenantAwareWebApplicationFactory.TenantId,
            StoreId = foreignStoreId, SubmittedByUserId = TenantAwareWebApplicationFactory.EmployeeUserId,
        });
        await db.SaveChangesAsync();

        UseRegionScopedAdmin();
        var response = await _client.GetAsync($"/form-submissions/{submissionId}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetStoreSettings_AsRegionScopedAdmin_ForOutOfRegionStore_Returns401()
    {
        await _factory.SeedCommonDataAsync();
        var (_, foreignStoreId) = await SeedForeignStoreAsync();

        UseRegionScopedAdmin();
        var response = await _client.GetAsync($"/stores/{foreignStoreId}/settings");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetLatestInventory_AsRegionScopedAdmin_ForOutOfRegionStore_Returns401()
    {
        await _factory.SeedCommonDataAsync();
        var (_, foreignStoreId) = await SeedForeignStoreAsync();

        UseRegionScopedAdmin();
        var response = await _client.GetAsync($"/stores/{foreignStoreId}/inventory/latest");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetInventoryHistory_AsRegionScopedAdmin_ForOutOfRegionStore_Returns401()
    {
        await _factory.SeedCommonDataAsync();
        var (_, foreignStoreId) = await SeedForeignStoreAsync();

        UseRegionScopedAdmin();
        var response = await _client.GetAsync($"/stores/{foreignStoreId}/inventory/history");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDepositByDate_AsRegionScopedAdmin_ForOutOfRegionStore_Returns401()
    {
        await _factory.SeedCommonDataAsync();
        var (_, foreignStoreId) = await SeedForeignStoreAsync();

        UseRegionScopedAdmin();
        var response = await _client.GetAsync($"/stores/{foreignStoreId}/deposit-log/2026-01-01");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDepositLog_AsRegionScopedAdmin_ForOutOfRegionStore_Returns401()
    {
        await _factory.SeedCommonDataAsync();
        var (_, foreignStoreId) = await SeedForeignStoreAsync();

        UseRegionScopedAdmin();
        var response = await _client.GetAsync($"/stores/{foreignStoreId}/deposit-log?page=1&pageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUser_AsRegionScopedAdmin_ForUserInOutOfRegionStore_Returns401()
    {
        await _factory.SeedCommonDataAsync();
        var (_, foreignStoreId) = await SeedForeignStoreAsync();

        var db = await _factory.GetTenantDbAsync();
        var foreignUserId = $"foreign-user-{Guid.NewGuid()}";
        db.UserProfiles.Add(new UserProfile
        {
            UserId = foreignUserId, Email = "foreign@test.com", DisplayName = "Foreign Employee",
            Role = "store_employee", StoreId = foreignStoreId,
        });
        await db.SaveChangesAsync();

        UseRegionScopedAdmin();
        var response = await _client.GetAsync($"/users/{foreignUserId}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUser_AsAnyRole_ForOwnProfile_StillReturns200()
    {
        await _factory.SeedCommonDataAsync();
        UseRegionScopedAdmin();

        var response = await _client.GetAsync($"/users/{TenantAwareWebApplicationFactory.AdminUserId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "the new authorization check must not block a caller from reading its own profile");
    }

    [Fact]
    public async Task GetStoreAssignments_AsRegionScopedAdmin_ForUserInOutOfRegionStore_Returns401()
    {
        await _factory.SeedCommonDataAsync();
        var (_, foreignStoreId) = await SeedForeignStoreAsync();

        var db = await _factory.GetTenantDbAsync();
        var foreignUserId = $"foreign-user-{Guid.NewGuid()}";
        db.UserProfiles.Add(new UserProfile
        {
            UserId = foreignUserId, Email = "foreign2@test.com", DisplayName = "Foreign Employee 2",
            Role = "store_employee", StoreId = foreignStoreId,
        });
        await db.SaveChangesAsync();

        UseRegionScopedAdmin();
        var response = await _client.GetAsync($"/users/{foreignUserId}/store-assignments");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetStoreAssignments_AsAnyRole_ForOwnAssignments_StillReturns200()
    {
        await _factory.SeedCommonDataAsync();
        UseRegionScopedAdmin();

        var response = await _client.GetAsync($"/users/{TenantAwareWebApplicationFactory.AdminUserId}/store-assignments");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "the new authorization check must not block a caller from reading its own store assignments");
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 13: the same fixes must not break legitimate in-scope access —
    // every deny-side test above has a matching allow-side test here using
    // TenantAwareWebApplicationFactory.StoreId, which IS in the admin's assigned region.
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTemplate_AsRegionScopedAdmin_ForInRegionStoreScopedTemplate_Returns200()
    {
        await _factory.SeedCommonDataAsync();

        var db = await _factory.GetTenantDbAsync();
        var templateId = Guid.NewGuid();
        db.TaskTemplates.Add(new TaskTemplate
        {
            Id = templateId, TenantId = TenantAwareWebApplicationFactory.TenantId,
            Name = "In-Region Template", Category = "Operations", Scope = "Store",
            StoreId = TenantAwareWebApplicationFactory.StoreId, CreatedByUserId = TenantAwareWebApplicationFactory.SuperAdminUserId,
        });
        await db.SaveChangesAsync();

        UseRegionScopedAdmin();
        var response = await _client.GetAsync($"/templates/{templateId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "the new scope check must not block a region-scoped admin from a template inside its own region");
    }

    [Fact]
    public async Task GetFormTemplate_AsRegionScopedAdmin_ForInRegionFormTemplate_Returns200()
    {
        await _factory.SeedCommonDataAsync();

        var db = await _factory.GetTenantDbAsync();
        var templateId = Guid.NewGuid();
        db.FormTemplates.Add(new FormTemplate
        {
            Id = templateId, TenantId = TenantAwareWebApplicationFactory.TenantId,
            Name = "In-Region Form Template", Scope = "Store", StoreId = TenantAwareWebApplicationFactory.StoreId,
            PropagationType = "NotificationOnly", CreatedByUserId = TenantAwareWebApplicationFactory.SuperAdminUserId,
        });
        await db.SaveChangesAsync();

        UseRegionScopedAdmin();
        var response = await _client.GetAsync($"/form-templates/{templateId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "the new scope check must not block a region-scoped admin from a form template inside its own region");
    }

    [Fact]
    public async Task GetFormSubmission_AsRegionScopedAdmin_ForInRegionStore_Returns200()
    {
        await _factory.SeedCommonDataAsync();

        var db = await _factory.GetTenantDbAsync();
        var submissionId = Guid.NewGuid();
        db.FormSubmissions.Add(new FormSubmission
        {
            Id = submissionId, TenantId = TenantAwareWebApplicationFactory.TenantId,
            StoreId = TenantAwareWebApplicationFactory.StoreId, SubmittedByUserId = TenantAwareWebApplicationFactory.EmployeeUserId,
        });
        await db.SaveChangesAsync();

        UseRegionScopedAdmin();
        var response = await _client.GetAsync($"/form-submissions/{submissionId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "the new scope check must not block a region-scoped admin from a submission inside its own region");
    }

    [Fact]
    public async Task GetStoreSettings_AsRegionScopedAdmin_ForInRegionStore_Returns200()
    {
        await _factory.SeedCommonDataAsync();
        UseRegionScopedAdmin();

        var response = await _client.GetAsync($"/stores/{TenantAwareWebApplicationFactory.StoreId}/settings");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "the new scope check must not block a region-scoped admin from its own region's store settings");
    }

    [Fact]
    public async Task GetLatestInventory_AsRegionScopedAdmin_ForInRegionStore_Returns200()
    {
        await _factory.SeedCommonDataAsync();
        UseRegionScopedAdmin();

        var response = await _client.GetAsync($"/stores/{TenantAwareWebApplicationFactory.StoreId}/inventory/latest");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "the new scope check must not block a region-scoped admin from its own region's inventory");
    }

    [Fact]
    public async Task GetInventoryHistory_AsRegionScopedAdmin_ForInRegionStore_Returns200()
    {
        await _factory.SeedCommonDataAsync();
        UseRegionScopedAdmin();

        var response = await _client.GetAsync($"/stores/{TenantAwareWebApplicationFactory.StoreId}/inventory/history");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "the new scope check must not block a region-scoped admin from its own region's inventory history");
    }

    [Fact]
    public async Task GetDepositByDate_AsRegionScopedAdmin_ForInRegionStore_Returns200()
    {
        await _factory.SeedCommonDataAsync();

        var db = await _factory.GetTenantDbAsync();
        var date = new DateOnly(2026, 1, 1);
        db.DepositLogs.Add(new DepositLog
        {
            TenantId = TenantAwareWebApplicationFactory.TenantId,
            StoreId = TenantAwareWebApplicationFactory.StoreId,
            Amount = 100m,
            SubmittedByManagerId = TenantAwareWebApplicationFactory.StoreManagerUserId,
            SubmittedAt = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
        });
        await db.SaveChangesAsync();

        UseRegionScopedAdmin();
        var response = await _client.GetAsync($"/stores/{TenantAwareWebApplicationFactory.StoreId}/deposit-log/{date:yyyy-MM-dd}");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "the new scope check must not block a region-scoped admin from its own region's deposit log");
    }

    [Fact]
    public async Task GetDepositLog_AsRegionScopedAdmin_ForInRegionStore_Returns200()
    {
        await _factory.SeedCommonDataAsync();
        UseRegionScopedAdmin();

        var response = await _client.GetAsync($"/stores/{TenantAwareWebApplicationFactory.StoreId}/deposit-log?page=1&pageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "the new scope check must not block a region-scoped admin from its own region's deposit log list");
    }

    [Fact]
    public async Task GetUser_AsRegionScopedAdmin_ForOtherUserInOwnRegionStore_Returns200()
    {
        await _factory.SeedCommonDataAsync();
        UseRegionScopedAdmin();

        // EmployeeUserId belongs to StoreId, which is in the admin's assigned region.
        var response = await _client.GetAsync($"/users/{TenantAwareWebApplicationFactory.EmployeeUserId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "a region-scoped admin must still be able to view another user's profile within its own region");
    }

    [Fact]
    public async Task GetStoreAssignments_AsRegionScopedAdmin_ForOtherUserInOwnRegionStore_Returns200()
    {
        await _factory.SeedCommonDataAsync();
        UseRegionScopedAdmin();

        var response = await _client.GetAsync($"/users/{TenantAwareWebApplicationFactory.EmployeeUserId}/store-assignments");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "a region-scoped admin must still be able to view another user's store assignments within its own region");
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 14: moving a store between regions (UpdateStoreHandler) is bounded by the
    // caller's manageable region set on BOTH ends — added when building the "add stores to
    // region" picker on the Regions detail slide-over, which reuses this endpoint.
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStore_AsRegionScopedAdmin_MovingIntoASecondRegionItManages_Returns204()
    {
        await _factory.SeedCommonDataAsync();

        var db = await _factory.GetTenantDbAsync();
        var secondRegionId = Guid.NewGuid();
        db.Regions.Add(new Region { Id = secondRegionId, TenantId = TenantAwareWebApplicationFactory.TenantId, Name = "Second Managed Region" });
        await db.SaveChangesAsync();

        UseToken(_factory.MintMultiRegionToken(
            TenantAwareWebApplicationFactory.AdminUserId, "admin", null,
            TenantAwareWebApplicationFactory.RegionId.ToString(), secondRegionId.ToString()));

        var response = await _client.PutAsJsonAsync(
            $"/stores/{TenantAwareWebApplicationFactory.AltStoreId}",
            new { name = "Alt Store", address = (string?)null, regionId = secondRegionId });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent,
            because: "the admin manages both the store's current region and the destination region");
    }

    [Fact]
    public async Task UpdateStore_AsRegionScopedAdmin_MovingIntoAnUnmanagedRegion_Returns401()
    {
        await _factory.SeedCommonDataAsync();
        var (foreignRegionId, _) = await SeedForeignStoreAsync();

        UseRegionScopedAdmin();

        var response = await _client.PutAsJsonAsync(
            $"/stores/{TenantAwareWebApplicationFactory.AltStoreId}",
            new { name = "Alt Store", address = (string?)null, regionId = foreignRegionId });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            because: "the admin does not manage the destination region");
    }

    [Fact]
    public async Task UpdateStore_AsRegionScopedAdmin_MovingAnOutOfScopeStore_Returns401()
    {
        await _factory.SeedCommonDataAsync();
        var (_, foreignStoreId) = await SeedForeignStoreAsync();

        UseRegionScopedAdmin();

        var response = await _client.PutAsJsonAsync(
            $"/stores/{foreignStoreId}",
            new { name = "Foreign Store", address = (string?)null, regionId = TenantAwareWebApplicationFactory.RegionId });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            because: "the admin does not manage the store's current (foreign) region, even though it wants to pull it into a region it does manage");
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 15: GetUsersHandler (the list endpoint) only returns users in the caller's
    // manageable scope — found while building the "assign roster" picker on the Store detail
    // slide-over, which reuses this endpoint as its candidate-staff source.
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUsers_AsRegionScopedAdmin_OnlyReturnsUsersInAssignedRegions()
    {
        await _factory.SeedCommonDataAsync();
        var (_, foreignStoreId) = await SeedForeignStoreAsync();

        var db = await _factory.GetTenantDbAsync();
        var foreignUserId = $"foreign-user-{Guid.NewGuid()}";
        db.UserProfiles.Add(new UserProfile
        {
            UserId = foreignUserId, Email = "foreign3@test.com", DisplayName = "Foreign Employee 3",
            Role = "store_employee", StoreId = foreignStoreId,
        });
        await db.SaveChangesAsync();

        UseRegionScopedAdmin();
        var response = await _client.GetAsync("/users?activeOnly=false");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var users = await response.Content.ReadFromJsonAsync<List<UserListDto>>();
        users.Should().NotBeNull();
        users!.Select(u => u.UserId).Should().Contain(TenantAwareWebApplicationFactory.EmployeeUserId,
            because: "the employee at the admin's own in-region store must still be visible");
        users.Select(u => u.UserId).Should().NotContain(foreignUserId,
            because: "a region-scoped admin must not see users at a store outside its assigned regions");
    }

    [Fact]
    public async Task GetUsers_AsStoreScopedEmployee_OnlySeesOwnStore()
    {
        await _factory.SeedCommonDataAsync();

        UseToken(_factory.MintToken(
            TenantAwareWebApplicationFactory.EmployeeUserId, "store_employee",
            storeId: TenantAwareWebApplicationFactory.StoreId.ToString()));

        var response = await _client.GetAsync("/users?activeOnly=false");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var users = await response.Content.ReadFromJsonAsync<List<UserListDto>>();
        users.Should().NotBeNull();
        users!.Select(u => u.UserId).Should().Contain(TenantAwareWebApplicationFactory.EmployeeUserId);
        users.Select(u => u.StoreId).Should().OnlyContain(s => s == TenantAwareWebApplicationFactory.StoreId,
            because: "a store-scoped caller must only see its own store's roster");
    }

    private sealed record RegionDto(Guid Id, string Name, string? Description, bool IsActive, DateTimeOffset CreatedAt);
    private sealed record StoreDto(Guid Id, string Name, string? Address, Guid RegionId, string RegionName, bool IsActive, DateTimeOffset CreatedAt);
    private sealed record UserListDto(string UserId, string Email, string DisplayName, string Role, Guid? StoreId, string? StoreName, Guid? RegionId, string? RegionName, bool IsActive, bool MustChangePassword, DateTimeOffset CreatedAt, List<string> RegionIds);
}
