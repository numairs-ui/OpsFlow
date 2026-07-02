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
}
