using FluentAssertions;
using OpsFlow.Domain.Entities;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace OpsFlow.Tests.Integration.Scenarios;

/// <summary>
/// End-to-end form submission lifecycle:
/// Admin creates form template → employee creates draft → updates draft → submits →
/// reviewer approves or rejects → employee resubmits after rejection.
/// Also tests NotificationOnly short-circuit and wrong-role approval rejection.
/// </summary>
public sealed class FormSubmissionLifecycleTests : IClassFixture<TenantAwareWebApplicationFactory>
{
    private readonly TenantAwareWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public FormSubmissionLifecycleTests(TenantAwareWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private void UseToken(string token)
        => _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

    // Seeds a FormTemplate directly in the DB — bypasses the API to keep test focus on the submission flow
    private async Task<Guid> SeedFormTemplateAsync(
        string propagationType = "Parallel",
        string approvalStepsJson = """[{"role":"admin","order":1}]""",
        string fieldsJson = """[{"id":"notes","label":"Notes","type":"Text","required":false}]""")
    {
        await _factory.SeedCommonDataAsync();
        var db = await _factory.GetTenantDbAsync();
        var id = Guid.NewGuid();
        db.FormTemplates.Add(new FormTemplate
        {
            Id = id,
            TenantId = TenantAwareWebApplicationFactory.TenantId,
            Name = "Test Form Template",
            Scope = "System",
            PropagationType = propagationType,
            ApprovalStepsJson = approvalStepsJson,
            FieldsJson = fieldsJson,
            IsActive = true,
            CreatedByUserId = TenantAwareWebApplicationFactory.AdminUserId,
        });
        await db.SaveChangesAsync();
        return id;
    }

    // Creates a draft submission via the API
    private async Task<Guid> CreateDraftAsync(Guid formTemplateId, Dictionary<string, string>? fieldValues = null)
    {
        var employeeToken = _factory.MintToken(
            TenantAwareWebApplicationFactory.EmployeeUserId, "store_employee",
            storeId: TenantAwareWebApplicationFactory.StoreId.ToString());
        UseToken(employeeToken);

        var response = await _client.PostAsJsonAsync("/form-submissions", new
        {
            formTemplateId,
            storeId = TenantAwareWebApplicationFactory.StoreId,
            fieldValues = fieldValues ?? new Dictionary<string, string> { ["notes"] = "Initial value" },
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 1: NotificationOnly template → submission immediately Recorded
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Submit_NotificationOnly_StatusBecomesRecordedImmediately()
    {
        var templateId = await SeedFormTemplateAsync(
            propagationType: "NotificationOnly",
            approvalStepsJson: """[{"role":"admin","order":1}]""");

        var submissionId = await CreateDraftAsync(templateId);

        // Employee submits
        UseToken(_factory.MintToken(TenantAwareWebApplicationFactory.EmployeeUserId, "store_employee",
            storeId: TenantAwareWebApplicationFactory.StoreId.ToString()));
        (await _client.PostAsJsonAsync<object?>($"/form-submissions/{submissionId}/submit", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify status = Recorded (no approval step needed)
        var db = await _factory.GetTenantDbAsync();
        var submission = await db.FormSubmissions.FindAsync(submissionId);
        submission!.Status.Should().Be("Recorded");
        submission.ResolvedAt.Should().NotBeNull();
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 2: Full sequential approval flow — Draft → PendingApproval → Approved
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Submit_Sequential_AdminApproves_StatusBecomesApproved()
    {
        var templateId = await SeedFormTemplateAsync(
            propagationType: "Sequential",
            approvalStepsJson: """[{"role":"admin","order":1}]""");

        var submissionId = await CreateDraftAsync(templateId);

        // Employee submits
        UseToken(_factory.MintToken(TenantAwareWebApplicationFactory.EmployeeUserId, "store_employee",
            storeId: TenantAwareWebApplicationFactory.StoreId.ToString()));
        (await _client.PostAsJsonAsync<object?>($"/form-submissions/{submissionId}/submit", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var db = await _factory.GetTenantDbAsync();
        (await db.FormSubmissions.FindAsync(submissionId))!.Status.Should().Be("PendingApproval");

        // Admin approves
        UseToken(_factory.MintToken(TenantAwareWebApplicationFactory.AdminUserId, "admin", regionId: TenantAwareWebApplicationFactory.RegionId.ToString()));
        (await _client.PostAsJsonAsync<object?>($"/form-submissions/{submissionId}/approve", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        db = await _factory.GetTenantDbAsync();
        var submission = await db.FormSubmissions.FindAsync(submissionId);
        submission!.Status.Should().Be("Approved");
        submission.ResolvedAt.Should().NotBeNull();
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 3: Admin rejects → employee resubmits → admin approves
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Submit_Rejected_EmployeeResubmits_ThenApproved()
    {
        var templateId = await SeedFormTemplateAsync(
            propagationType: "Sequential",
            approvalStepsJson: """[{"role":"admin","order":1}]""");

        var adminRegionToken = _factory.MintToken(
            TenantAwareWebApplicationFactory.AdminUserId, "admin",
            regionId: TenantAwareWebApplicationFactory.RegionId.ToString());
        var employeeToken = _factory.MintToken(
            TenantAwareWebApplicationFactory.EmployeeUserId, "store_employee",
            storeId: TenantAwareWebApplicationFactory.StoreId.ToString());

        // ── Part 1: rejection is terminal ────────────────────────────────────────
        var rejectedId = await CreateDraftAsync(templateId);
        UseToken(employeeToken);
        await _client.PostAsJsonAsync<object?>($"/form-submissions/{rejectedId}/submit", null);

        UseToken(adminRegionToken);
        (await _client.PostAsJsonAsync($"/form-submissions/{rejectedId}/reject", new { reason = "Incomplete information" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var db = await _factory.GetTenantDbAsync();
        (await db.FormSubmissions.FindAsync(rejectedId))!.Status.Should().Be("Rejected");

        // ── Part 2: return → employee resubmits → admin approves ─────────────────
        var returnedId = await CreateDraftAsync(templateId);
        UseToken(employeeToken);
        await _client.PostAsJsonAsync<object?>($"/form-submissions/{returnedId}/submit", null);

        // Admin returns it to the employee (Returned is non-terminal — resubmit re-enters the step)
        UseToken(adminRegionToken);
        (await _client.PostAsJsonAsync($"/form-submissions/{returnedId}/return", new { comments = "Please add more detail" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        db = await _factory.GetTenantDbAsync();
        (await db.FormSubmissions.FindAsync(returnedId))!.Status.Should().Be("Returned");

        // Employee resubmits from Returned
        UseToken(employeeToken);
        (await _client.PostAsJsonAsync($"/form-submissions/{returnedId}/submit",
            new { fieldValues = new Dictionary<string, string> { ["notes"] = "Updated with full detail" } }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Admin approves the resubmission
        UseToken(adminRegionToken);
        (await _client.PostAsJsonAsync<object?>($"/form-submissions/{returnedId}/approve", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        db = await _factory.GetTenantDbAsync();
        (await db.FormSubmissions.FindAsync(returnedId))!.Status.Should().Be("Approved");
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 4: Employee updates draft field values before submitting
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Employee_UpdatesDraft_ThenSubmits_PersistsLatestValues()
    {
        var templateId = await SeedFormTemplateAsync(
            propagationType: "NotificationOnly",
            fieldsJson: """[{"id":"notes","label":"Notes","type":"Text","required":true}]""");

        var submissionId = await CreateDraftAsync(templateId, new() { ["notes"] = "First draft" });

        // Update the draft
        UseToken(_factory.MintToken(TenantAwareWebApplicationFactory.EmployeeUserId, "store_employee",
            storeId: TenantAwareWebApplicationFactory.StoreId.ToString()));
        (await _client.PatchAsJsonAsync($"/form-submissions/{submissionId}/draft",
            new { fieldValues = new Dictionary<string, string> { ["notes"] = "Final value" } }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Submit (provides no new values — uses persisted draft values)
        (await _client.PostAsJsonAsync<object?>($"/form-submissions/{submissionId}/submit", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var db = await _factory.GetTenantDbAsync();
        var submission = await db.FormSubmissions.FindAsync(submissionId);
        submission!.Status.Should().Be("Recorded");
        submission.FieldValuesJson.Should().Contain("Final value");
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 5: Submitting without required fields returns 400
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Submit_WithMissingRequiredField_Returns400()
    {
        var templateId = await SeedFormTemplateAsync(
            propagationType: "NotificationOnly",
            fieldsJson: """[{"id":"incident_desc","label":"Incident Description","type":"Text","required":true}]""");

        // Create draft with EMPTY values
        var submissionId = await CreateDraftAsync(templateId, new() { });

        // Submit without providing required field
        UseToken(_factory.MintToken(TenantAwareWebApplicationFactory.EmployeeUserId, "store_employee",
            storeId: TenantAwareWebApplicationFactory.StoreId.ToString()));
        var response = await _client.PostAsJsonAsync<object?>($"/form-submissions/{submissionId}/submit", null);

        // ValidationException → 400 (via our global exception handler)
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 6: Wrong-role user cannot approve
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Approve_WrongRole_Returns401()
    {
        // Template requires admin approval
        var templateId = await SeedFormTemplateAsync(
            propagationType: "Sequential",
            approvalStepsJson: """[{"role":"admin","order":1}]""");

        var submissionId = await CreateDraftAsync(templateId);

        // Submit it
        UseToken(_factory.MintToken(TenantAwareWebApplicationFactory.EmployeeUserId, "store_employee",
            storeId: TenantAwareWebApplicationFactory.StoreId.ToString()));
        await _client.PostAsJsonAsync<object?>($"/form-submissions/{submissionId}/submit", null);

        // Store employee (wrong role) tries to approve
        UseToken(_factory.MintToken(TenantAwareWebApplicationFactory.EmployeeUserId, "store_employee",
            storeId: TenantAwareWebApplicationFactory.StoreId.ToString()));
        var response = await _client.PostAsJsonAsync<object?>($"/form-submissions/{submissionId}/approve", null);

        // ApproveFormSubmissionHandler throws UnauthorizedAccessException when role doesn't match
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
