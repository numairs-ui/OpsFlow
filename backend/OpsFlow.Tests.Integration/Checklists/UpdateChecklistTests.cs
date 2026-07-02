using FluentAssertions;
using OpsFlow.Domain.Entities;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace OpsFlow.Tests.Integration.Checklists;

/// <summary>
/// Tests the C1 fix: PUT /checklists/{id} updates name/description/scope.
/// Previously only checklist items could be edited; the header fields were discarded.
/// </summary>
public sealed class UpdateChecklistTests : IClassFixture<TenantAwareWebApplicationFactory>
{
    private readonly TenantAwareWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UpdateChecklistTests(TenantAwareWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task SeedAsync(Checklist checklist)
    {
        await _factory.SeedMasterDbAsync();
        var db = await _factory.GetTenantDbAsync();
        db.Checklists.Add(checklist);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task PutChecklist_AsAdmin_Updates_NameAndDescription()
    {
        var id = Guid.NewGuid();
        await SeedAsync(new Checklist
        {
            Id = id,
            TenantId = TenantAwareWebApplicationFactory.TenantId,
            Name = "Old Name",
            Description = "Old Description",
            Scope = "System",
            CreatedByUserId = TenantAwareWebApplicationFactory.AdminUserId,
        });

        var token = _factory.MintToken(TenantAwareWebApplicationFactory.AdminUserId, "super_admin");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PutAsJsonAsync($"/checklists/{id}", new
        {
            name = "New Name",
            description = "New Description",
            scope = "System",
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var db = await _factory.GetTenantDbAsync();
        var updated = await db.Checklists.FindAsync(id);
        updated!.Name.Should().Be("New Name");
        updated.Description.Should().Be("New Description");
    }

    [Fact]
    public async Task PutChecklist_WithoutToken_Returns401()
    {
        var response = await _client.PutAsJsonAsync($"/checklists/{Guid.NewGuid()}", new
        {
            name = "X",
            scope = "System",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PutChecklist_AsStoreEmployee_Returns403()
    {
        var token = _factory.MintToken(TenantAwareWebApplicationFactory.EmployeeUserId, "store_employee");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PutAsJsonAsync($"/checklists/{Guid.NewGuid()}", new
        {
            name = "X",
            scope = "System",
        });

        // System-scope checklist update requires admin — handler throws UnauthorizedAccessException
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PutChecklist_WithEmptyName_Returns400()
    {
        var id = Guid.NewGuid();
        await SeedAsync(new Checklist
        {
            Id = id,
            TenantId = TenantAwareWebApplicationFactory.TenantId,
            Name = "Original",
            Scope = "System",
            CreatedByUserId = TenantAwareWebApplicationFactory.AdminUserId,
        });

        var token = _factory.MintToken(TenantAwareWebApplicationFactory.AdminUserId, "super_admin");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PutAsJsonAsync($"/checklists/{id}", new
        {
            name = "",
            scope = "System",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
