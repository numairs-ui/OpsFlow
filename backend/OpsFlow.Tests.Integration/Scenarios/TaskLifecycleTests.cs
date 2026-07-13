using FluentAssertions;
using OpsFlow.Domain.Entities;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace OpsFlow.Tests.Integration.Scenarios;

/// <summary>
/// End-to-end task lifecycle: Admin creates checklist → creates task → employee claims/starts/completes → admin verifies.
/// Also covers cancellation, deferral, and authorization edge cases on task state transitions.
/// </summary>
public sealed class TaskLifecycleTests : IClassFixture<TenantAwareWebApplicationFactory>
{
    private readonly TenantAwareWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TaskLifecycleTests(TenantAwareWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ────────────────────────────────────────────────────────────────
    // Helpers
    // ────────────────────────────────────────────────────────────────

    private void UseToken(string token)
        => _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

    private async Task<Guid> SeedChecklistAsync(string name = "Daily Ops")
    {
        await _factory.SeedCommonDataAsync();
        var db = await _factory.GetTenantDbAsync();
        var id = Guid.NewGuid();
        db.Checklists.Add(new Checklist
        {
            Id = id,
            TenantId = TenantAwareWebApplicationFactory.TenantId,
            Name = name,
            Scope = "System",
            IsActive = true,
            CreatedByUserId = TenantAwareWebApplicationFactory.AdminUserId,
        });
        await db.SaveChangesAsync();
        return id;
    }

    private async Task<Guid> SeedTaskAsync(Guid checklistId, string status = "Pending", string? assignedTo = null)
    {
        var db = await _factory.GetTenantDbAsync();
        var id = Guid.NewGuid();
        db.TaskInstances.Add(new TaskInstance
        {
            Id = id,
            TenantId = TenantAwareWebApplicationFactory.TenantId,
            ChecklistId = checklistId,
            StoreId = TenantAwareWebApplicationFactory.StoreId,
            DueAt = DateTimeOffset.UtcNow.AddHours(4),
            Status = status,
            AssignedToUserId = assignedTo,
            CreatedByUserId = TenantAwareWebApplicationFactory.AdminUserId,
        });
        await db.SaveChangesAsync();
        return id;
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 0: Photo upload URL round trip (B5) — the employee working a
    // task requests a signed upload URL for a Photo field and gets one back.
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PhotoUploadUrl_ForAssignedEmployee_ReturnsUploadAndBlobUrls()
    {
        var checklistId = await SeedChecklistAsync("Photo Ops");
        var taskId = await SeedTaskAsync(checklistId, status: "InProgress",
            assignedTo: TenantAwareWebApplicationFactory.EmployeeUserId);
        UseToken(_factory.MintToken(
            TenantAwareWebApplicationFactory.EmployeeUserId, "store_employee",
            storeId: TenantAwareWebApplicationFactory.StoreId.ToString()));

        var response = await _client.PostAsJsonAsync($"/tasks/{taskId}/photo-upload-url", new
        {
            templateId = "tmpl-1",
            fieldId = "photo-1",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("uploadUrl").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("blobUrl").GetString().Should().NotBeNullOrEmpty();
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 1: Admin creates a checklist via the API
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AdminCreatesChecklist_Returns201WithId()
    {
        await _factory.SeedCommonDataAsync();
        UseToken(_factory.MintToken(TenantAwareWebApplicationFactory.AdminUserId, "super_admin"));

        var response = await _client.PostAsJsonAsync("/checklists", new
        {
            name = "New Opening Checklist",
            scope = "System",
            items = Array.Empty<object>(),
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetGuid().Should().NotBeEmpty();
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 1a: Scored checklist session (A3) — completing with item scores
    // yields a composite score and surfaces a failed item's corrective action.
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ScoredChecklistSession_Complete_ComputesCompositeAndSurfacesFailure()
    {
        await _factory.SeedCommonDataAsync();
        var db = await _factory.GetTenantDbAsync();

        var checklistId = Guid.NewGuid();
        var passId = Guid.NewGuid();
        var failId = Guid.NewGuid();
        db.Checklists.Add(new Checklist
        {
            Id = checklistId, TenantId = TenantAwareWebApplicationFactory.TenantId,
            Name = "Manager Walk", Scope = "System", IsActive = true,
            CreatedByUserId = TenantAwareWebApplicationFactory.AdminUserId,
        });
        foreach (var (id, name) in new[] { (passId, "Lobby"), (failId, "Restroom") })
        {
            db.TaskTemplates.Add(new TaskTemplate
            {
                Id = id, TenantId = TenantAwareWebApplicationFactory.TenantId,
                Name = name, Category = "General", Scope = "System", IsActive = true,
                FieldsJson = "[]", CreatedByUserId = TenantAwareWebApplicationFactory.AdminUserId,
            });
        }
        db.ChecklistTemplateItems.Add(new ChecklistTemplateItem
        {
            ChecklistId = checklistId, TemplateId = passId, Order = 0,
            ScoringType = "PassFail", Weight = 1m,
        });
        db.ChecklistTemplateItems.Add(new ChecklistTemplateItem
        {
            ChecklistId = checklistId, TemplateId = failId, Order = 1,
            ScoringType = "PassFail", Weight = 1m, FailCorrectiveActionText = "Re-clean the restroom",
        });
        var taskId = Guid.NewGuid();
        db.TaskInstances.Add(new TaskInstance
        {
            Id = taskId, TenantId = TenantAwareWebApplicationFactory.TenantId,
            ChecklistId = checklistId, StoreId = TenantAwareWebApplicationFactory.StoreId,
            DueAt = DateTimeOffset.UtcNow.AddHours(4), Status = "InProgress",
            CreatedByUserId = TenantAwareWebApplicationFactory.AdminUserId,
        });
        await db.SaveChangesAsync();

        UseToken(_factory.MintToken(TenantAwareWebApplicationFactory.AdminUserId, "super_admin"));

        var completeResp = await _client.PostAsJsonAsync($"/tasks/{taskId}/complete", new
        {
            fieldValues = Array.Empty<object>(),
            itemScores = new[]
            {
                new { templateId = passId, score = 1 },   // pass
                new { templateId = failId, score = 0 },   // fail
            },
        });
        completeResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await completeResp.Content.ReadFromJsonAsync<JsonElement>();
        // One pass (100) + one fail (0), equal weight → 50%.
        body.GetProperty("compositeScorePercent").GetDecimal().Should().Be(50.0m);
        body.GetProperty("triggeredCorrectiveActions").EnumerateArray()
            .Select(a => a.GetProperty("text").GetString())
            .Should().Contain("Re-clean the restroom");
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 1a-ii: A failed scored item spawns a claimable corrective task (A4).
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task FailedScoredItem_SpawnsClaimableCorrectiveTask()
    {
        await _factory.SeedCommonDataAsync();
        var db = await _factory.GetTenantDbAsync();

        var checklistId = Guid.NewGuid();
        var failId = Guid.NewGuid();
        db.Checklists.Add(new Checklist
        {
            Id = checklistId, TenantId = TenantAwareWebApplicationFactory.TenantId,
            Name = "Walk", Scope = "System", IsActive = true,
            CreatedByUserId = TenantAwareWebApplicationFactory.AdminUserId,
        });
        db.TaskTemplates.Add(new TaskTemplate
        {
            Id = failId, TenantId = TenantAwareWebApplicationFactory.TenantId,
            Name = "Restroom", Category = "General", Scope = "System", IsActive = true,
            FieldsJson = "[]", CreatedByUserId = TenantAwareWebApplicationFactory.AdminUserId,
        });
        db.ChecklistTemplateItems.Add(new ChecklistTemplateItem
        {
            ChecklistId = checklistId, TemplateId = failId, Order = 0,
            ScoringType = "PassFail", Weight = 1m, FailCorrectiveActionText = "Re-clean the restroom",
        });
        var sessionId = Guid.NewGuid();
        db.TaskInstances.Add(new TaskInstance
        {
            Id = sessionId, TenantId = TenantAwareWebApplicationFactory.TenantId,
            ChecklistId = checklistId, StoreId = TenantAwareWebApplicationFactory.StoreId,
            DueAt = DateTimeOffset.UtcNow.AddHours(4), Status = "InProgress",
            CreatedByUserId = TenantAwareWebApplicationFactory.AdminUserId,
        });
        await db.SaveChangesAsync();

        UseToken(_factory.MintToken(TenantAwareWebApplicationFactory.AdminUserId, "super_admin"));

        var completeResp = await _client.PostAsJsonAsync($"/tasks/{sessionId}/complete", new
        {
            fieldValues = Array.Empty<object>(),
            itemScores = new[] { new { templateId = failId, score = 0 } },
        });
        completeResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await completeResp.Content.ReadFromJsonAsync<JsonElement>();
        var spawned = body.GetProperty("spawnedCorrectiveTaskIds").EnumerateArray().ToList();
        spawned.Should().ContainSingle();
        var correctiveId = spawned[0].GetGuid();

        // The spawned task is standalone, claimable, linked to the session, and due ~24h out.
        var fresh = await _factory.GetTenantDbAsync();
        var corrective = await fresh.TaskInstances.FindAsync(correctiveId);
        corrective.Should().NotBeNull();
        corrective!.ChecklistId.Should().BeNull();
        corrective.SourceTaskInstanceId.Should().Be(sessionId);
        corrective.AssignedToUserId.Should().BeNull();
        corrective.Status.Should().Be("Pending");
        corrective.Notes.Should().Contain("Re-clean the restroom");
        corrective.DueAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddHours(24), TimeSpan.FromMinutes(5));
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 1b: Standalone tasks (A1) — notes-only and single-template,
    // each created without a checklist and completed end-to-end.
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task NotesOnlyTask_CreateThenComplete_RoundTrips()
    {
        await _factory.SeedCommonDataAsync();
        UseToken(_factory.MintToken(TenantAwareWebApplicationFactory.AdminUserId, "super_admin"));

        var createResp = await _client.PostAsJsonAsync("/tasks", new
        {
            storeId = TenantAwareWebApplicationFactory.StoreId,
            dueAt = DateTimeOffset.UtcNow.AddHours(4),
            notes = "Sweep the patio before close.",
        });
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var taskId = (await createResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        // A standalone (no-checklist) task surfaces with a nullable checklistId on the detail read.
        var detail = await (await _client.GetAsync($"/tasks/{taskId}")).Content.ReadFromJsonAsync<JsonElement>();
        detail.GetProperty("checklistId").ValueKind.Should().Be(JsonValueKind.Null);
        detail.GetProperty("templates").GetArrayLength().Should().Be(0);

        // Notes-only completes with no field values.
        var completeResp = await _client.PostAsJsonAsync($"/tasks/{taskId}/complete", new
        {
            fieldValues = Array.Empty<object>(),
        });
        completeResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var db = await _factory.GetTenantDbAsync();
        (await db.TaskInstances.FindAsync(taskId))!.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task StandaloneTemplateTask_CreateThenComplete_ValidatesTemplateFields()
    {
        await _factory.SeedCommonDataAsync();
        var db = await _factory.GetTenantDbAsync();
        var templateId = Guid.NewGuid();
        db.TaskTemplates.Add(new TaskTemplate
        {
            Id = templateId,
            TenantId = TenantAwareWebApplicationFactory.TenantId,
            Name = "Fridge Temp Check",
            Category = "General",
            Scope = "System",
            IsActive = true,
            FieldsJson = """[{"id":"temp","type":"Text","label":"Reading","required":true}]""",
            CreatedByUserId = TenantAwareWebApplicationFactory.AdminUserId,
        });
        await db.SaveChangesAsync();

        UseToken(_factory.MintToken(TenantAwareWebApplicationFactory.AdminUserId, "super_admin"));

        var createResp = await _client.PostAsJsonAsync("/tasks", new
        {
            storeId = TenantAwareWebApplicationFactory.StoreId,
            dueAt = DateTimeOffset.UtcNow.AddHours(4),
            taskTemplateId = templateId,
        });
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var taskId = (await createResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        // The single ad-hoc template's fields surface for completion.
        var detail = await (await _client.GetAsync($"/tasks/{taskId}")).Content.ReadFromJsonAsync<JsonElement>();
        detail.GetProperty("templates").GetArrayLength().Should().Be(1);

        // Missing the required field is rejected.
        var missingResp = await _client.PostAsJsonAsync($"/tasks/{taskId}/complete", new
        {
            fieldValues = Array.Empty<object>(),
        });
        missingResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Supplying it completes the task.
        var okResp = await _client.PostAsJsonAsync($"/tasks/{taskId}/complete", new
        {
            fieldValues = new[] { new { templateId, fieldId = "temp", value = "4C" } },
        });
        okResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateTask_WithBothChecklistAndTemplate_Returns400()
    {
        var checklistId = await SeedChecklistAsync("Conflict Checklist");
        var db = await _factory.GetTenantDbAsync();
        var templateId = Guid.NewGuid();
        db.TaskTemplates.Add(new TaskTemplate
        {
            Id = templateId,
            TenantId = TenantAwareWebApplicationFactory.TenantId,
            Name = "T", Category = "General", Scope = "System", IsActive = true,
            FieldsJson = "[]", CreatedByUserId = TenantAwareWebApplicationFactory.AdminUserId,
        });
        await db.SaveChangesAsync();

        UseToken(_factory.MintToken(TenantAwareWebApplicationFactory.AdminUserId, "super_admin"));

        var response = await _client.PostAsJsonAsync("/tasks", new
        {
            checklistId,
            taskTemplateId = templateId,
            storeId = TenantAwareWebApplicationFactory.StoreId,
            dueAt = DateTimeOffset.UtcNow.AddHours(4),
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 2: Admin creates a task pointing to a checklist
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AdminCreatesTask_Returns201WithId()
    {
        var checklistId = await SeedChecklistAsync("Closing Checklist");
        UseToken(_factory.MintToken(TenantAwareWebApplicationFactory.AdminUserId, "super_admin"));

        var response = await _client.PostAsJsonAsync("/tasks", new
        {
            checklistId,
            storeId = TenantAwareWebApplicationFactory.StoreId,
            dueAt = DateTimeOffset.UtcNow.AddHours(8),
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetGuid().Should().NotBeEmpty();
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 3: Full happy-path lifecycle — Pending → InProgress → Completed → Verified
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TaskFullLifecycle_AdminCreates_EmployeeClaims_Starts_Completes_AdminVerifies()
    {
        await _factory.SeedCommonDataAsync();

        // Admin creates checklist
        var adminToken = _factory.MintToken(TenantAwareWebApplicationFactory.AdminUserId, "super_admin");
        UseToken(adminToken);
        var createChecklistResp = await _client.PostAsJsonAsync("/checklists", new
        {
            name = "Full Lifecycle Checklist",
            scope = "System",
            items = Array.Empty<object>(),
        });
        createChecklistResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var checklistId = (await createChecklistResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        // Admin creates task
        var createTaskResp = await _client.PostAsJsonAsync("/tasks", new
        {
            checklistId,
            storeId = TenantAwareWebApplicationFactory.StoreId,
            dueAt = DateTimeOffset.UtcNow.AddHours(2),
        });
        createTaskResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var taskId = (await createTaskResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        // Employee claims the task
        var employeeToken = _factory.MintToken(
            TenantAwareWebApplicationFactory.EmployeeUserId, "store_employee",
            storeId: TenantAwareWebApplicationFactory.StoreId.ToString());
        UseToken(employeeToken);

        (await _client.PostAsJsonAsync<object?>($"/tasks/{taskId}/claim", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Employee starts the task
        (await _client.PostAsJsonAsync<object?>($"/tasks/{taskId}/start", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify task is InProgress
        var db = await _factory.GetTenantDbAsync();
        var task = await db.TaskInstances.FindAsync(taskId);
        task!.Status.Should().Be("InProgress");
        task.AssignedToUserId.Should().Be(TenantAwareWebApplicationFactory.EmployeeUserId);

        // Employee completes the task (no required fields on this checklist)
        var completeResp = await _client.PostAsJsonAsync($"/tasks/{taskId}/complete", new
        {
            fieldValues = Array.Empty<object>(),
        });
        completeResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify Completed status
        db = await _factory.GetTenantDbAsync();
        task = await db.TaskInstances.FindAsync(taskId);
        task!.Status.Should().Be("Completed");

        // Admin verifies the task
        UseToken(adminToken);
        (await _client.PostAsJsonAsync<object?>($"/tasks/{taskId}/verify", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify final status
        db = await _factory.GetTenantDbAsync();
        task = await db.TaskInstances.FindAsync(taskId);
        task!.Status.Should().Be("Verified");
        task.VerifiedByUserId.Should().Be(TenantAwareWebApplicationFactory.AdminUserId);
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 4: Claiming an already-claimed task returns 500
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ClaimTask_AlreadyClaimed_Returns500()
    {
        var checklistId = await SeedChecklistAsync();
        var taskId = await SeedTaskAsync(checklistId, assignedTo: "someone-else");

        UseToken(_factory.MintToken(TenantAwareWebApplicationFactory.EmployeeUserId, "store_employee",
            storeId: TenantAwareWebApplicationFactory.StoreId.ToString()));

        var response = await _client.PostAsJsonAsync<object?>($"/tasks/{taskId}/claim", null);

        // ClaimTaskHandler throws InvalidOperationException → 500
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 5: Completing an already-completed task is idempotent
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CompleteTask_AlreadyCompleted_ReturnsIdempotentResult()
    {
        var checklistId = await SeedChecklistAsync();
        var taskId = await SeedTaskAsync(checklistId, status: "Completed");

        // Seed an existing completion
        var db = await _factory.GetTenantDbAsync();
        db.TaskCompletions.Add(new TaskCompletion
        {
            TenantId = TenantAwareWebApplicationFactory.TenantId,
            TaskInstanceId = taskId,
            CompletedByUserId = TenantAwareWebApplicationFactory.EmployeeUserId,
            FieldValuesJson = "[]",
            CorrectiveActionsJson = "[]",
        });
        await db.SaveChangesAsync();

        UseToken(_factory.MintToken(TenantAwareWebApplicationFactory.EmployeeUserId, "store_employee",
            storeId: TenantAwareWebApplicationFactory.StoreId.ToString()));

        // Completing again should return the existing record (idempotent)
        var response = await _client.PostAsJsonAsync($"/tasks/{taskId}/complete", new
        {
            fieldValues = Array.Empty<object>(),
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 6: Cannot complete a cancelled task
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CompleteTask_WhenCancelled_Returns500()
    {
        var checklistId = await SeedChecklistAsync();
        var taskId = await SeedTaskAsync(checklistId, status: "Cancelled");

        UseToken(_factory.MintToken(TenantAwareWebApplicationFactory.EmployeeUserId, "store_employee",
            storeId: TenantAwareWebApplicationFactory.StoreId.ToString()));

        var response = await _client.PostAsJsonAsync($"/tasks/{taskId}/complete", new
        {
            fieldValues = Array.Empty<object>(),
        });

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 7: Admin cancels a pending task
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CancelTask_AsAdmin_UpdatesStatusToCancelled()
    {
        var checklistId = await SeedChecklistAsync();
        var taskId = await SeedTaskAsync(checklistId);

        UseToken(_factory.MintToken(TenantAwareWebApplicationFactory.AdminUserId, "super_admin"));

        var response = await _client.PostAsJsonAsync($"/tasks/{taskId}/cancel", new
        {
            reason = "No longer needed for this shift",
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var db = await _factory.GetTenantDbAsync();
        var task = await db.TaskInstances.FindAsync(taskId);
        task!.Status.Should().Be("Cancelled");
        task.CancelReason.Should().Be("No longer needed for this shift");
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 8: Admin defers a task to a future date
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeferTask_AsAdmin_UpdatesStatusToDeferred()
    {
        var checklistId = await SeedChecklistAsync();
        var taskId = await SeedTaskAsync(checklistId);

        UseToken(_factory.MintToken(TenantAwareWebApplicationFactory.AdminUserId, "super_admin"));

        var deferredTo = DateTimeOffset.UtcNow.AddDays(1);
        var response = await _client.PostAsJsonAsync($"/tasks/{taskId}/defer", new
        {
            reason = "Store closure today",
            deferredTo,
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var db = await _factory.GetTenantDbAsync();
        var task = await db.TaskInstances.FindAsync(taskId);
        task!.Status.Should().Be("Deferred");
        task.DeferReason.Should().Be("Store closure today");
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 9: Cannot verify a task that is not yet Completed
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task VerifyTask_WhenPending_Returns500()
    {
        var checklistId = await SeedChecklistAsync();
        var taskId = await SeedTaskAsync(checklistId, status: "Pending");

        UseToken(_factory.MintToken(TenantAwareWebApplicationFactory.AdminUserId, "super_admin"));

        var response = await _client.PostAsJsonAsync<object?>($"/tasks/{taskId}/verify", null);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 10: Store manager creates task for their own store
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task StoreManagerCreatesTask_ForTheirStore_Returns201()
    {
        var checklistId = await SeedChecklistAsync("Manager-assigned Checklist");

        UseToken(_factory.MintToken(
            TenantAwareWebApplicationFactory.StoreManagerUserId, "store_manager",
            storeId: TenantAwareWebApplicationFactory.StoreId.ToString()));

        var response = await _client.PostAsJsonAsync("/tasks", new
        {
            checklistId,
            storeId = TenantAwareWebApplicationFactory.StoreId,
            dueAt = DateTimeOffset.UtcNow.AddHours(3),
        });

        // CreateTaskHandler checks if store_manager's profile.StoreId == cmd.StoreId
        // Since we seeded the manager's UserProfile with StoreId = StoreId, this should succeed
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // ────────────────────────────────────────────────────────────────
    // Scenario 11: Store manager cannot create task for a different store
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task StoreManagerCreatesTask_ForDifferentStore_Returns401()
    {
        var checklistId = await SeedChecklistAsync("Cross-Store Checklist");

        UseToken(_factory.MintToken(
            TenantAwareWebApplicationFactory.StoreManagerUserId, "store_manager",
            storeId: TenantAwareWebApplicationFactory.StoreId.ToString()));

        var response = await _client.PostAsJsonAsync("/tasks", new
        {
            checklistId,
            storeId = TenantAwareWebApplicationFactory.AltStoreId, // different store!
            dueAt = DateTimeOffset.UtcNow.AddHours(3),
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
