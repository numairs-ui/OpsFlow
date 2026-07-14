using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using OpsFlow.Domain.Entities;
using Xunit;

namespace OpsFlow.Tests.Integration.Scenarios;

/// <summary>
/// A2: checklist items carry scoring config (type, weight, photo-required, fail corrective text,
/// fail threshold). UpdateItems persists it, GetChecklist surfaces it, and invalid combinations
/// are rejected.
/// </summary>
public sealed class ChecklistScoringTests : IClassFixture<TenantAwareWebApplicationFactory>
{
    private readonly TenantAwareWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ChecklistScoringTests(TenantAwareWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private void UseAdmin()
        => _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",
                _factory.MintToken(TenantAwareWebApplicationFactory.AdminUserId, "super_admin"));

    private async Task<(Guid checklistId, Guid templateId)> SeedChecklistAndTemplateAsync()
    {
        await _factory.SeedCommonDataAsync();
        var db = await _factory.GetTenantDbAsync();
        var checklistId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        db.Checklists.Add(new Checklist
        {
            Id = checklistId, TenantId = TenantAwareWebApplicationFactory.TenantId,
            Name = $"Scored {checklistId:N}", Scope = "System", IsActive = true,
            CreatedByUserId = TenantAwareWebApplicationFactory.AdminUserId,
        });
        db.TaskTemplates.Add(new TaskTemplate
        {
            Id = templateId, TenantId = TenantAwareWebApplicationFactory.TenantId,
            Name = "Walk Item", Category = "General", Scope = "System", IsActive = true,
            FieldsJson = "[]", CreatedByUserId = TenantAwareWebApplicationFactory.AdminUserId,
        });
        await db.SaveChangesAsync();
        return (checklistId, templateId);
    }

    [Fact]
    public async Task UpdateItems_WithScoring_Persists_AndGetReturnsIt()
    {
        var (checklistId, templateId) = await SeedChecklistAndTemplateAsync();
        UseAdmin();

        var putResp = await _client.PutAsJsonAsync($"/checklists/{checklistId}/items", new[]
        {
            new
            {
                templateId,
                order = 0,
                scoringType = "Scale1To5",
                weight = 2.5m,
                photoRequired = true,
                failCorrectiveActionText = "Re-clean and re-inspect",
                failScoreThreshold = 2,
            },
        });
        putResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detail = await (await _client.GetAsync($"/checklists/{checklistId}")).Content.ReadFromJsonAsync<JsonElement>();
        var item = detail.GetProperty("items").EnumerateArray().Single();
        item.GetProperty("scoringType").GetString().Should().Be("Scale1To5");
        item.GetProperty("weight").GetDecimal().Should().Be(2.5m);
        item.GetProperty("photoRequired").GetBoolean().Should().BeTrue();
        item.GetProperty("failCorrectiveActionText").GetString().Should().Be("Re-clean and re-inspect");
        item.GetProperty("failScoreThreshold").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task UpdateItems_ThresholdOnPassFail_Returns400()
    {
        var (checklistId, templateId) = await SeedChecklistAndTemplateAsync();
        UseAdmin();

        var putResp = await _client.PutAsJsonAsync($"/checklists/{checklistId}/items", new[]
        {
            new { templateId, order = 0, scoringType = "PassFail", weight = 1.0m, failScoreThreshold = 3 },
        });

        putResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateItems_UnscoredItem_DefaultsPreserved()
    {
        var (checklistId, templateId) = await SeedChecklistAndTemplateAsync();
        UseAdmin();

        // No scoring fields sent → defaults (null scoring, weight 1.0).
        var putResp = await _client.PutAsJsonAsync($"/checklists/{checklistId}/items", new[]
        {
            new { templateId, order = 0 },
        });
        putResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detail = await (await _client.GetAsync($"/checklists/{checklistId}")).Content.ReadFromJsonAsync<JsonElement>();
        var item = detail.GetProperty("items").EnumerateArray().Single();
        item.GetProperty("scoringType").ValueKind.Should().Be(JsonValueKind.Null);
        item.GetProperty("weight").GetDecimal().Should().Be(1.0m);
    }
}
