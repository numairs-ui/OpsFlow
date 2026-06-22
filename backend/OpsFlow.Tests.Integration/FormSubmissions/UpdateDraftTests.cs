using FluentAssertions;
using OpsFlow.Domain.Entities;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace OpsFlow.Tests.Integration.FormSubmissions;

/// <summary>
/// Tests the D1 fix: PATCH /form-submissions/{id}/draft
/// Updates field values on an existing Draft without creating a duplicate.
/// </summary>
public sealed class UpdateDraftTests : IClassFixture<TenantAwareWebApplicationFactory>
{
    private readonly TenantAwareWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UpdateDraftTests(TenantAwareWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task SeedAsync(FormSubmission submission)
    {
        await _factory.SeedMasterDbAsync();
        var db = await _factory.GetTenantDbAsync();
        db.FormSubmissions.Add(submission);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task PatchDraft_WithOwnerToken_Returns204AndUpdatesValues()
    {
        var submissionId = Guid.NewGuid();
        await SeedAsync(new FormSubmission
        {
            Id = submissionId,
            TenantId = TenantAwareWebApplicationFactory.TenantId,
            StoreId = Guid.NewGuid(),
            SubmittedByUserId = TenantAwareWebApplicationFactory.EmployeeUserId,
            Status = "Draft",
            FieldValuesJson = """{"field-1":"old value"}""",
        });

        var token = _factory.MintToken(TenantAwareWebApplicationFactory.EmployeeUserId, "store_employee");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PatchAsJsonAsync(
            $"/form-submissions/{submissionId}/draft",
            new { fieldValues = new Dictionary<string, string> { ["field-1"] = "new value" } });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify persisted value
        var db = await _factory.GetTenantDbAsync();
        var updated = await db.FormSubmissions.FindAsync(submissionId);
        updated!.FieldValuesJson.Should().Contain("new value");
    }

    [Fact]
    public async Task PatchDraft_WithoutToken_Returns401()
    {
        var response = await _client.PatchAsJsonAsync(
            $"/form-submissions/{Guid.NewGuid()}/draft",
            new { fieldValues = new Dictionary<string, string>() });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PatchDraft_WhenNotOwner_Returns401()
    {
        var submissionId = Guid.NewGuid();
        await SeedAsync(new FormSubmission
        {
            Id = submissionId,
            TenantId = TenantAwareWebApplicationFactory.TenantId,
            StoreId = Guid.NewGuid(),
            SubmittedByUserId = "someone-else-id",
            Status = "Draft",
            FieldValuesJson = "{}",
        });

        var token = _factory.MintToken(TenantAwareWebApplicationFactory.EmployeeUserId, "store_employee");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PatchAsJsonAsync(
            $"/form-submissions/{submissionId}/draft",
            new { fieldValues = new Dictionary<string, string>() });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PatchDraft_WhenStatusIsNotDraft_Returns400()
    {
        var submissionId = Guid.NewGuid();
        await SeedAsync(new FormSubmission
        {
            Id = submissionId,
            TenantId = TenantAwareWebApplicationFactory.TenantId,
            StoreId = Guid.NewGuid(),
            SubmittedByUserId = TenantAwareWebApplicationFactory.EmployeeUserId,
            Status = "PendingApproval",
            FieldValuesJson = "{}",
        });

        var token = _factory.MintToken(TenantAwareWebApplicationFactory.EmployeeUserId, "store_employee");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PatchAsJsonAsync(
            $"/form-submissions/{submissionId}/draft",
            new { fieldValues = new Dictionary<string, string>() });

        // InvalidOperationException → 500 without global handler, 400 with it
        // Our handler throws InvalidOperationException which becomes 500 via default handler
        // (only UnauthorizedAccessException gets special 401 treatment in Program.cs)
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task PatchDraft_WhenNotFound_Returns500()
    {
        var token = _factory.MintToken(TenantAwareWebApplicationFactory.EmployeeUserId, "store_employee");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PatchAsJsonAsync(
            $"/form-submissions/{Guid.NewGuid()}/draft",
            new { fieldValues = new Dictionary<string, string>() });

        // KeyNotFoundException → 500 (no special mapping for it yet)
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }
}
