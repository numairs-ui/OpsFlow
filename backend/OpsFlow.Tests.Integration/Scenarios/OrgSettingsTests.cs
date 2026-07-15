using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace OpsFlow.Tests.Integration.Scenarios;

/// <summary>
/// Org-wide settings: tenant defaults round-trip through the API, new stores are seeded from those
/// defaults (falling back to code literals when unset), and per-store deposit deadline is editable.
/// </summary>
public sealed class OrgSettingsTests : IClassFixture<TenantAwareWebApplicationFactory>
{
    private readonly TenantAwareWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public OrgSettingsTests(TenantAwareWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private void UseSuperAdmin() =>
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", _factory.MintToken(TenantAwareWebApplicationFactory.SuperAdminUserId, "super_admin"));

    private async Task PutTenantSettingsAsync(object body)
    {
        var resp = await _client.PutAsJsonAsync("/tenant/settings", body);
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // Full body with all org defaults cleared — used to establish a known baseline per test.
    private static object ClearedDefaultsBody() => new
    {
        name = "Test Tenant",
        logoUrl = (string?)null,
        primaryContactEmail = (string?)null,
        defaultTimezoneId = (string?)null,
        defaultOverdueGraceMinutes = (int?)null,
        defaultDepositDeadlineLocalTime = (string?)null,
        defaultTillABase = (decimal?)null,
        defaultTillBBase = (decimal?)null,
        defaultDoughNeedTargets = (Dictionary<string, object>?)null,
        localeCode = (string?)null,
        currencyCode = (string?)null,
    };

    [Fact]
    public async Task TenantSettings_PutThenGet_RoundTripsOrgDefaults()
    {
        await _factory.SeedMasterDbAsync();
        UseSuperAdmin();

        await PutTenantSettingsAsync(new
        {
            name = "Test Tenant",
            logoUrl = (string?)null,
            primaryContactEmail = "ops@test.com",
            defaultTimezoneId = "Europe/London",
            defaultOverdueGraceMinutes = 45,
            defaultDepositDeadlineLocalTime = "20:30:00",
            defaultTillABase = 150.00m,
            defaultTillBBase = 175.00m,
            defaultDoughNeedTargets = new Dictionary<string, object> { ["dough_10in"] = new { day2Need = 12, day3Need = 24 } },
            localeCode = "en-GB",
            currencyCode = "GBP",
        });

        var get = await _client.GetAsync("/tenant/settings");
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        var s = await get.Content.ReadFromJsonAsync<TenantSettingsResponse>();

        s!.DefaultTimezoneId.Should().Be("Europe/London");
        s.DefaultOverdueGraceMinutes.Should().Be(45);
        s.DefaultDepositDeadlineLocalTime.Should().StartWith("20:30");
        s.DefaultTillABase.Should().Be(150.00m);
        s.DefaultTillBBase.Should().Be(175.00m);
        s.LocaleCode.Should().Be("en-GB");
        s.CurrencyCode.Should().Be("GBP");
        s.DefaultDoughNeedTargets.Should().ContainKey("dough_10in");
        s.DefaultDoughNeedTargets!["dough_10in"].Day2Need.Should().Be(12);
    }

    [Fact]
    public async Task CreateStore_SeedsSettings_FromTenantDefaults()
    {
        await _factory.SeedCommonDataAsync();
        UseSuperAdmin();

        await PutTenantSettingsAsync(new
        {
            name = "Test Tenant",
            defaultTimezoneId = "Europe/Dublin",
            defaultOverdueGraceMinutes = 20,
            defaultDepositDeadlineLocalTime = "19:15:00",
            defaultTillABase = 111.00m,
            defaultTillBBase = 222.00m,
            defaultDoughNeedTargets = new Dictionary<string, object> { ["dough_12in"] = new { day2Need = 30, day3Need = 60 } },
            localeCode = (string?)null,
            currencyCode = (string?)null,
        });

        var create = await _client.PostAsJsonAsync("/stores", new
        {
            name = $"Seeded Store {Guid.NewGuid():N}",
            address = (string?)null,
            regionId = TenantAwareWebApplicationFactory.RegionId,
        });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await create.Content.ReadFromJsonAsync<CreatedStore>();

        var settings = await (await _client.GetAsync($"/stores/{created!.Id}/settings"))
            .Content.ReadFromJsonAsync<StoreSettingsResponse>();

        settings!.TimezoneId.Should().Be("Europe/Dublin");
        settings.OverdueGraceMinutes.Should().Be(20);
        settings.DepositDeadlineLocalTime.Should().StartWith("19:15");
        settings.TillABase.Should().Be(111.00m);
        settings.TillBBase.Should().Be(222.00m);
        settings.DoughNeedTargets.Should().ContainKey("dough_12in");
        settings.DoughNeedTargets["dough_12in"].Day2Need.Should().Be(30);
    }

    [Fact]
    public async Task CreateStore_SeedsSettings_FromLiterals_WhenNoTenantDefaults()
    {
        await _factory.SeedCommonDataAsync();
        UseSuperAdmin();

        // Explicitly clear org defaults so this test is order-independent.
        await PutTenantSettingsAsync(ClearedDefaultsBody());

        var create = await _client.PostAsJsonAsync("/stores", new
        {
            name = $"Literal Store {Guid.NewGuid():N}",
            address = (string?)null,
            regionId = TenantAwareWebApplicationFactory.RegionId,
        });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await create.Content.ReadFromJsonAsync<CreatedStore>();

        var settings = await (await _client.GetAsync($"/stores/{created!.Id}/settings"))
            .Content.ReadFromJsonAsync<StoreSettingsResponse>();

        settings!.TimezoneId.Should().Be("America/New_York");
        settings.OverdueGraceMinutes.Should().Be(30);
        settings.TillABase.Should().BeNull();
        settings.DoughNeedTargets.Should().ContainKey("dough_10in");
        settings.DoughNeedTargets["dough_10in"].Day2Need.Should().Be(24);
        settings.DoughNeedTargets["dough_10in"].Day3Need.Should().Be(48);
    }

    [Fact]
    public async Task StoreSettings_PutThenGet_PersistsDepositDeadline()
    {
        await _factory.SeedCommonDataAsync();
        UseSuperAdmin();

        var put = await _client.PutAsJsonAsync($"/stores/{TenantAwareWebApplicationFactory.StoreId}/settings", new
        {
            tillABase = (decimal?)null,
            tillBBase = (decimal?)null,
            doughNeedTargets = new Dictionary<string, object>(),
            timezoneId = "America/Chicago",
            overdueGraceMinutes = 15,
            depositDeadlineLocalTime = "18:45:00",
        });
        put.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var settings = await (await _client.GetAsync($"/stores/{TenantAwareWebApplicationFactory.StoreId}/settings"))
            .Content.ReadFromJsonAsync<StoreSettingsResponse>();

        settings!.TimezoneId.Should().Be("America/Chicago");
        settings.OverdueGraceMinutes.Should().Be(15);
        settings.DepositDeadlineLocalTime.Should().StartWith("18:45");
    }

    // Local response shapes (the API DTOs are internal). Bound case-insensitively from camelCase JSON.
    private sealed record TenantSettingsResponse(
        string Id, string Name, string? LogoUrl, string? PrimaryContactEmail, bool IsActive,
        string? DefaultTimezoneId, int? DefaultOverdueGraceMinutes, string? DefaultDepositDeadlineLocalTime,
        decimal? DefaultTillABase, decimal? DefaultTillBBase,
        Dictionary<string, DoughTarget>? DefaultDoughNeedTargets, string? LocaleCode, string? CurrencyCode);

    private sealed record StoreSettingsResponse(
        Guid StoreId, decimal? TillABase, decimal? TillBBase,
        Dictionary<string, DoughTarget> DoughNeedTargets, string TimezoneId, int OverdueGraceMinutes,
        string? DepositDeadlineLocalTime);

    private sealed record DoughTarget(double Day2Need, double Day3Need);

    private sealed record CreatedStore(Guid Id);
}
