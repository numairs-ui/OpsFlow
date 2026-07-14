using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace OpsFlow.Tests.Integration.Scenarios;

/// <summary>
/// A6: the template-import path now materializes Checklist rows into a Checklist + one TaskTemplate
/// per sub-item + scored ChecklistTemplateItem rows, and only counts rows that actually persist
/// (fixing the old fake-success bug where Checklist rows incremented the counter but saved nothing).
/// </summary>
public sealed class TemplateImportTests : IClassFixture<TenantAwareWebApplicationFactory>
{
    private readonly TenantAwareWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TemplateImportTests(TenantAwareWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private void UseSuperAdmin()
        => _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",
                _factory.MintToken(TenantAwareWebApplicationFactory.SuperAdminUserId, "super_admin"));

    [Fact]
    public async Task ImportChecklist_CreatesChecklistWithScoredItems()
    {
        await _factory.SeedCommonDataAsync();
        UseSuperAdmin();

        var response = await _client.PostAsJsonAsync("/templates/import", new
        {
            templates = new[]
            {
                new
                {
                    type = "Checklist",
                    name = "Spill & Hazard Walk",
                    category = "Safety",
                    scope = "System",
                    items = new object[]
                    {
                        new { name = "Floor walk completed", scoringType = "PassFail", weight = 1.0m, order = 0 },
                        new { name = "Spills cleaned", scoringType = "PassFail", weight = 1.0m, order = 1,
                              failCorrectiveActionText = "Re-clean and dry the area" },
                    },
                },
            },
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("created").GetInt32().Should().Be(1);

        var db = await _factory.GetTenantDbAsync();
        var checklist = await db.Checklists
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Name == "Spill & Hazard Walk");
        checklist.Should().NotBeNull();
        checklist!.Items.Should().HaveCount(2);
        checklist.Items.Should().OnlyContain(i => i.ScoringType == "PassFail");
        checklist.Items.Should().Contain(i => i.FailCorrectiveActionText == "Re-clean and dry the area");
    }

    [Fact]
    public async Task ImportChecklist_WithNoItems_IsRejected_NotFakeSuccess()
    {
        await _factory.SeedCommonDataAsync();
        UseSuperAdmin();

        var response = await _client.PostAsJsonAsync("/templates/import", new
        {
            templates = new[]
            {
                new { type = "Checklist", name = "Empty Walk", category = "Safety", scope = "System" },
            },
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        // The old bug counted this as created; now it's a failure, nothing persisted.
        body.GetProperty("created").GetInt32().Should().Be(0);
        body.GetProperty("failed").GetArrayLength().Should().Be(1);

        var db = await _factory.GetTenantDbAsync();
        (await db.Checklists.AnyAsync(c => c.Name == "Empty Walk")).Should().BeFalse();
    }
}
