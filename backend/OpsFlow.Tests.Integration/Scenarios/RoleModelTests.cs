using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;
using F = OpsFlow.Tests.Integration.TenantAwareWebApplicationFactory;

namespace OpsFlow.Tests.Integration.Scenarios;

/// <summary>
/// Authorization tests for the six-role model: super_admin (global), admin (multi-region),
/// supervisor (single region), store_manager/store_employee/store_kiosk (single store).
/// Focuses on the new behaviours: super_admin-only System scope, region-scoped admin
/// allow/deny, and the store_kiosk read being locked to its own store.
/// </summary>
public sealed class RoleModelTests : IClassFixture<F>
{
    private readonly F _factory;
    private readonly HttpClient _client;

    public RoleModelTests(F factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private void UseToken(string token)
        => _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

    // ── store_kiosk is locked to its own store (closes the read-gap) ──────────────

    [Fact]
    public async Task Kiosk_GetTodayTasks_ForOwnStore_Returns200()
    {
        await _factory.SeedCommonDataAsync();
        UseToken(_factory.MintToken(F.KioskUserId, "store_kiosk", storeId: F.StoreId.ToString()));

        var response = await _client.GetAsync($"/stores/{F.StoreId}/tasks/today");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Kiosk_GetTodayTasks_ForDifferentStore_Returns401()
    {
        await _factory.SeedCommonDataAsync();
        UseToken(_factory.MintToken(F.KioskUserId, "store_kiosk", storeId: F.StoreId.ToString()));

        var response = await _client.GetAsync($"/stores/{F.AltStoreId}/tasks/today");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── System-scope templates are super_admin only ──────────────────────────────

    [Fact]
    public async Task SuperAdmin_CreatesSystemTemplate_Returns200()
    {
        await _factory.SeedCommonDataAsync();
        UseToken(_factory.MintToken(F.SuperAdminUserId, "super_admin"));

        var response = await _client.PostAsJsonAsync("/templates", new
        {
            name = $"Sys-{Guid.NewGuid():N}",
            category = "General",
            scope = "System",
            fieldsJson = "[]",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Admin_CreatesSystemTemplate_Returns401()
    {
        await _factory.SeedCommonDataAsync();
        UseToken(_factory.MintMultiRegionToken(F.AdminUserId, "admin", null, F.RegionId.ToString()));

        var response = await _client.PostAsJsonAsync("/templates", new
        {
            name = $"Sys-{Guid.NewGuid():N}",
            category = "General",
            scope = "System",
            fieldsJson = "[]",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Region-scoped admin: allowed in-region, denied out-of-region ──────────────

    [Fact]
    public async Task Admin_GetTodayTasks_StoreInRegion_Returns200()
    {
        await _factory.SeedCommonDataAsync();
        UseToken(_factory.MintMultiRegionToken(F.AdminUserId, "admin", null, F.RegionId.ToString()));

        var response = await _client.GetAsync($"/stores/{F.StoreId}/tasks/today");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Admin_GetTodayTasks_StoreOutsideRegion_Returns401()
    {
        await _factory.SeedCommonDataAsync();
        // Admin scoped to a region that does NOT contain the seeded store.
        var otherRegion = Guid.NewGuid().ToString();
        UseToken(_factory.MintMultiRegionToken(F.AdminUserId, "admin", null, otherRegion));

        var response = await _client.GetAsync($"/stores/{F.StoreId}/tasks/today");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── A region-scoped admin cannot mint a super_admin/admin peer ────────────────

    [Fact]
    public async Task Admin_CreatesAdminUser_Returns401()
    {
        await _factory.SeedCommonDataAsync();
        UseToken(_factory.MintMultiRegionToken(F.AdminUserId, "admin", null, F.RegionId.ToString()));

        var response = await _client.PostAsJsonAsync("/users", new
        {
            email = $"peer-{Guid.NewGuid():N}@test.com",
            password = "TempPass1!",
            displayName = "Would-be Admin",
            role = "admin",
            regionIds = new[] { F.RegionId },
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Dashboard scope enforcement (previously unguarded) ────────────────────────

    [Fact]
    public async Task Admin_RegionDashboard_OutOfRegion_Returns401()
    {
        await _factory.SeedCommonDataAsync();
        var otherRegion = Guid.NewGuid().ToString();
        UseToken(_factory.MintMultiRegionToken(F.AdminUserId, "admin", null, otherRegion));

        var response = await _client.GetAsync($"/dashboard/region/{F.RegionId}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Admin_SystemDashboard_Returns401()
    {
        await _factory.SeedCommonDataAsync();
        UseToken(_factory.MintMultiRegionToken(F.AdminUserId, "admin", null, F.RegionId.ToString()));

        var response = await _client.GetAsync("/dashboard/system");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Employee_StoreDashboard_OtherStore_Returns401()
    {
        await _factory.SeedCommonDataAsync();
        UseToken(_factory.MintToken(F.EmployeeUserId, "store_employee", storeId: F.StoreId.ToString()));

        var response = await _client.GetAsync($"/dashboard/store/{F.AltStoreId}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
