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
    // Scenario 1: Admin creates a checklist via the API
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AdminCreatesChecklist_Returns201WithId()
    {
        await _factory.SeedCommonDataAsync();
        UseToken(_factory.MintToken(TenantAwareWebApplicationFactory.AdminUserId, "admin"));

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
    // Scenario 2: Admin creates a task pointing to a checklist
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AdminCreatesTask_Returns201WithId()
    {
        var checklistId = await SeedChecklistAsync("Closing Checklist");
        UseToken(_factory.MintToken(TenantAwareWebApplicationFactory.AdminUserId, "admin"));

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
        var adminToken = _factory.MintToken(TenantAwareWebApplicationFactory.AdminUserId, "admin");
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

        UseToken(_factory.MintToken(TenantAwareWebApplicationFactory.AdminUserId, "admin"));

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

        UseToken(_factory.MintToken(TenantAwareWebApplicationFactory.AdminUserId, "admin"));

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

        UseToken(_factory.MintToken(TenantAwareWebApplicationFactory.AdminUserId, "admin"));

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
