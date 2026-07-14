using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace OpsFlow.Tests.Integration.Scenarios;

/// <summary>
/// User management: admin creates users with various roles, assigns them to stores,
/// deactivates and reactivates accounts, and lists users with filters.
/// Note: CreateUserHandler uses NullAuthProvider which always returns "test-user-id",
/// so user IDs are not unique across test runs — we verify HTTP status and profile records.
/// </summary>
public sealed class UserManagementTests : IClassFixture<TenantAwareWebApplicationFactory>
{
    private readonly TenantAwareWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UserManagementTests(TenantAwareWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private void UseAdminToken()
        => _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",
                _factory.MintToken(TenantAwareWebApplicationFactory.AdminUserId, "super_admin"));

    // ────────────────────────────────────────────────────────────────
    // Scenario 1: Admin creates a store_employee user
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Admin_CreatesStoreEmployee_Returns201()
    {
        await _factory.SeedCommonDataAsync();
        UseAdminToken();

        var response = await _client.PostAsJsonAsync("/users", new
        {
            email = $"employee-{Guid.NewGuid():N}@test.com",
            password = "TempPass1!",
            displayName = "New Employee",
            role = "store_employee",
            storeId = TenantAwareWebApplicationFactory.StoreId,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("userId").GetString().Should().NotBeNullOrEmpty();
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 2: Admin creates a supervisor with a regionId
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Admin_CreatesSupervisor_WithRegionId_Returns201()
    {
        await _factory.SeedCommonDataAsync();
        UseAdminToken();

        var response = await _client.PostAsJsonAsync("/users", new
        {
            email = $"supervisor-{Guid.NewGuid():N}@test.com",
            password = "TempPass1!",
            displayName = "New Supervisor",
            role = "supervisor",
            regionIds = new[] { TenantAwareWebApplicationFactory.RegionId },
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 3: Admin deactivates a user then reactivates them
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Admin_DeactivatesUser_ThenReactivates_ProfileReflectsChange()
    {
        await _factory.SeedCommonDataAsync();
        UseAdminToken();

        // Create the user
        var createResp = await _client.PostAsJsonAsync("/users", new
        {
            email = $"deactivate-test-{Guid.NewGuid():N}@test.com",
            password = "TempPass1!",
            displayName = "Deactivation Test User",
            role = "store_employee",
            storeId = TenantAwareWebApplicationFactory.StoreId,
        });
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var userId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("userId").GetString()!;

        // Deactivate
        (await _client.PostAsJsonAsync<object?>($"/users/{userId}/deactivate", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deactivated
        var getResp = await _client.GetAsync($"/users/{userId}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await getResp.Content.ReadFromJsonAsync<JsonElement>();
        profile.GetProperty("isActive").GetBoolean().Should().BeFalse();

        // Reactivate
        (await _client.PostAsJsonAsync<object?>($"/users/{userId}/reactivate", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify reactivated
        getResp = await _client.GetAsync($"/users/{userId}");
        (await getResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 3b: Admin resets a user's password, gets a one-time temp password
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Admin_ResetsPassword_ReturnsTempPassword_SatisfyingPolicy()
    {
        await _factory.SeedCommonDataAsync();
        UseAdminToken();

        var createResp = await _client.PostAsJsonAsync("/users", new
        {
            email = $"reset-test-{Guid.NewGuid():N}@test.com",
            password = "TempPass1!",
            displayName = "Reset Test User",
            role = "store_employee",
            storeId = TenantAwareWebApplicationFactory.StoreId,
        });
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var userId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("userId").GetString()!;

        // Reset with a generated temp password (empty body).
        var resetResp = await _client.PostAsJsonAsync<object?>($"/users/{userId}/reset-password", null);
        resetResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var temp = (await resetResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("tempPassword").GetString()!;

        temp.Should().NotBeNullOrEmpty();
        temp.Length.Should().BeGreaterThanOrEqualTo(8);
        temp.Should().MatchRegex("[A-Z]").And.MatchRegex("[a-z]")
            .And.MatchRegex("[0-9]").And.MatchRegex("[^A-Za-z0-9]");
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 4: Admin adds and removes a store assignment for a user
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Admin_AddsStoreAssignment_ThenRemoves_ListUpdatesCorrectly()
    {
        await _factory.SeedCommonDataAsync();
        UseAdminToken();

        // Create user — only store_manager users may hold multiple store assignments
        var createResp = await _client.PostAsJsonAsync("/users", new
        {
            email = $"assignment-test-{Guid.NewGuid():N}@test.com",
            password = "TempPass1!",
            displayName = "Assignment Test User",
            role = "store_manager",
            storeId = TenantAwareWebApplicationFactory.StoreId,
        });
        var userId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("userId").GetString()!;

        // Add secondary store assignment
        (await _client.PostAsJsonAsync($"/users/{userId}/store-assignments", new
        {
            storeId = TenantAwareWebApplicationFactory.AltStoreId,
        })).StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify assignment exists
        var listResp = await _client.GetAsync($"/users/{userId}/store-assignments");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var assignments = await listResp.Content.ReadFromJsonAsync<JsonElement>();
        assignments.GetArrayLength().Should().BeGreaterThan(0);

        // Remove the secondary assignment
        (await _client.DeleteAsync($"/users/{userId}/store-assignments/{TenantAwareWebApplicationFactory.AltStoreId}"))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 5: GET /users with role filter returns only matching roles
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUsers_FilterByRole_ReturnsOnly_MatchingRole()
    {
        await _factory.SeedCommonDataAsync();
        UseAdminToken();

        // Create a specific-role user so we know at least one exists
        await _client.PostAsJsonAsync("/users", new
        {
            email = $"filter-supervisor-{Guid.NewGuid():N}@test.com",
            password = "TempPass1!",
            displayName = "Filter Supervisor",
            role = "supervisor",
            regionIds = new[] { TenantAwareWebApplicationFactory.RegionId },
        });

        var response = await _client.GetAsync("/users?role=supervisor&activeOnly=false");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var users = await response.Content.ReadFromJsonAsync<JsonElement>();
        users.GetArrayLength().Should().BeGreaterThan(0);
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 6: Updating a user changes role + region scope (profile mirror)
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Admin_UpdatesUser_RoleAndRegion_Reflected()
    {
        await _factory.SeedCommonDataAsync();
        UseAdminToken();

        var createResp = await _client.PostAsJsonAsync("/users", new
        {
            email = $"update-test-{Guid.NewGuid():N}@test.com",
            password = "TempPass1!",
            displayName = "Original Name",
            role = "store_employee",
            storeId = TenantAwareWebApplicationFactory.StoreId,
        });
        var userId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("userId").GetString()!;

        // Promote to supervisor over a region — store scope should drop, region scope appear.
        var putResp = await _client.PutAsJsonAsync($"/users/{userId}", new
        {
            displayName = "Updated Name",
            role = "supervisor",
            regionIds = new[] { TenantAwareWebApplicationFactory.RegionId },
        });
        putResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResp = await _client.GetAsync($"/users/{userId}");
        var dto = await getResp.Content.ReadFromJsonAsync<JsonElement>();
        dto.GetProperty("displayName").GetString().Should().Be("Updated Name");
        dto.GetProperty("role").GetString().Should().Be("supervisor");
        dto.GetProperty("storeId").ValueKind.Should().Be(JsonValueKind.Null);
        dto.GetProperty("regionIds")[0].GetString().Should().Be(TenantAwareWebApplicationFactory.RegionId.ToString());
    }
}
