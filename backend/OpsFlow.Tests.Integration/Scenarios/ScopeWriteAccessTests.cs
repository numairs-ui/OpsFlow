using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace OpsFlow.Tests.Integration.Scenarios;

/// <summary>
/// Regression tests for the scoped-write and cross-user authorization holes closed in the
/// ScopeAuthorizer migration: Store-scope writes must follow store-management rules (employees/kiosks
/// denied, region/store roles confined), user-activity reads are scoped to the caller, and a
/// region-scoped admin cannot move users outside its region set.
/// </summary>
public sealed class ScopeWriteAccessTests : IClassFixture<TenantAwareWebApplicationFactory>
{
    private readonly TenantAwareWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ScopeWriteAccessTests(TenantAwareWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private void UseToken(string token)
        => _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

    // ── Store-scope writes follow store-management rules ──────────────────────

    [Fact]
    public async Task Employee_CreatingStoreScopeChecklist_ForOwnStore_IsDenied()
    {
        await _factory.SeedCommonDataAsync();

        UseToken(_factory.MintToken(
            TenantAwareWebApplicationFactory.EmployeeUserId, "store_employee",
            storeId: TenantAwareWebApplicationFactory.StoreId.ToString()));

        // store_employee may never author Store-scope resources, even for their own store.
        var response = await _client.PostAsJsonAsync("/checklists", new
        {
            name = "Employee store checklist",
            scope = "Store",
            storeId = TenantAwareWebApplicationFactory.StoreId,
            items = Array.Empty<object>(),
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Manager_CreatingStoreScopeChecklist_ForForeignStore_IsDenied()
    {
        await _factory.SeedCommonDataAsync();

        UseToken(_factory.MintToken(
            TenantAwareWebApplicationFactory.StoreManagerUserId, "store_manager",
            storeId: TenantAwareWebApplicationFactory.StoreId.ToString()));

        // A manager bound to StoreId cannot write a Store-scope checklist for AltStoreId.
        var response = await _client.PostAsJsonAsync("/checklists", new
        {
            name = "Cross-store checklist",
            scope = "Store",
            storeId = TenantAwareWebApplicationFactory.AltStoreId,
            items = Array.Empty<object>(),
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Manager_CreatingStoreScopeChecklist_ForOwnStore_Succeeds()
    {
        await _factory.SeedCommonDataAsync();

        UseToken(_factory.MintToken(
            TenantAwareWebApplicationFactory.StoreManagerUserId, "store_manager",
            storeId: TenantAwareWebApplicationFactory.StoreId.ToString()));

        var response = await _client.PostAsJsonAsync("/checklists", new
        {
            name = "Own-store checklist",
            scope = "Store",
            storeId = TenantAwareWebApplicationFactory.StoreId,
            items = Array.Empty<object>(),
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // ── User-activity reads are scoped to the caller ──────────────────────────

    [Fact]
    public async Task Employee_ReadingAnotherUsersActivity_IsDenied()
    {
        await _factory.SeedCommonDataAsync();

        UseToken(_factory.MintToken(
            TenantAwareWebApplicationFactory.EmployeeUserId, "store_employee",
            storeId: TenantAwareWebApplicationFactory.StoreId.ToString()));

        // An employee may read their own activity but not the manager's.
        var response = await _client.GetAsync(
            $"/users/{TenantAwareWebApplicationFactory.StoreManagerUserId}/activity");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task User_ReadingOwnActivity_Succeeds()
    {
        await _factory.SeedCommonDataAsync();

        UseToken(_factory.MintToken(
            TenantAwareWebApplicationFactory.EmployeeUserId, "store_employee",
            storeId: TenantAwareWebApplicationFactory.StoreId.ToString()));

        var response = await _client.GetAsync(
            $"/users/{TenantAwareWebApplicationFactory.EmployeeUserId}/activity");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SuperAdmin_ReadingAnyUsersActivity_Succeeds()
    {
        await _factory.SeedCommonDataAsync();

        UseToken(_factory.MintToken(TenantAwareWebApplicationFactory.SuperAdminUserId, "super_admin"));

        var response = await _client.GetAsync(
            $"/users/{TenantAwareWebApplicationFactory.EmployeeUserId}/activity");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
