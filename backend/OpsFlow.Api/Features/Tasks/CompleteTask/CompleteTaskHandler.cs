using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Hubs;
using OpsFlow.Domain.Checklists;
using OpsFlow.Domain.Entities;
using OpsFlow.Infrastructure;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpsFlow.Api.Features.Tasks.CompleteTask;

internal sealed class CompleteTaskHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor,
    IHubContext<TaskBoardHub> hub) : IRequestHandler<CompleteTaskCommand, CompleteTaskResponse>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<CompleteTaskResponse> Handle(CompleteTaskCommand cmd, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");

        await using var db = await factory.CreateAsync(ct);

        var task = await db.TaskInstances
            .Include(t => t.Checklist)
                .ThenInclude(c => c!.Items.OrderBy(i => i.Order))
                    .ThenInclude(i => i.Template)
            .Include(t => t.AdHocTaskTemplate)
            .Include(t => t.Completions)
            .FirstOrDefaultAsync(t => t.Id == cmd.TaskId, ct)
            ?? throw new KeyNotFoundException($"Task {cmd.TaskId} not found.");

        // Idempotency: already completed — return existing record
        if (task.Completions.Count > 0)
        {
            var existing = task.Completions.OrderBy(c => c.CompletedAt).First();
            var existingActions = JsonSerializer.Deserialize<List<CorrectiveActionDto>>(existing.CorrectiveActionsJson, JsonOptions) ?? [];
            return new CompleteTaskResponse(
                new TaskCompletionResultDto(existing.Id, existing.TaskInstanceId, existing.CompletedByUserId, existing.CompletedByVolunteerName, existing.CompletedAt),
                existingActions,
                existing.CompositeScorePercent);
        }

        if (task.Status is "Cancelled" or "Deferred")
            throw new InvalidOperationException($"Cannot complete a task in status '{task.Status}'.");

        var storeSettings = await db.StoreSettings.FindAsync([task.StoreId], ct);
        // Checklist-backed tasks validate every item's template; standalone tasks validate their single
        // ad-hoc template (or, for notes-only tasks, nothing).
        var validation = task.ChecklistId is not null
            ? TaskFieldValidator.Validate(task.Checklist?.Items ?? [], cmd.FieldValues, storeSettings)
            : TaskFieldValidator.ValidateAdHoc(task.AdHocTaskTemplate, cmd.FieldValues, storeSettings);

        if (validation.Errors.Count > 0)
            throw new ValidationException(validation.Errors);

        var corrective = validation.CorrectiveActions;

        // Checklist-session scoring (A3): for any scored items, enforce photo-required, compute the
        // composite score, and surface item failures alongside the field-based corrective actions.
        var scoredItems = task.Checklist?.Items.Where(ChecklistScoring.IsScored).ToList() ?? [];
        decimal? compositeScore = null;
        var itemScoresJson = "[]";
        var failures = new List<ChecklistFailure>();
        if (scoredItems.Count > 0)
        {
            var submitted = cmd.ItemScores ?? [];

            var photoErrors = scoredItems
                .Where(i => i.PhotoRequired)
                .Where(i => string.IsNullOrWhiteSpace(
                    submitted.FirstOrDefault(s => s.TemplateId == i.TemplateId)?.PhotoUrl))
                .Select(i => new ValidationFailure($"ItemScores[{i.TemplateId}]",
                    $"A photo is required for '{i.Template?.Name}'."))
                .ToList();
            if (photoErrors.Count > 0) throw new ValidationException(photoErrors);

            var scores = submitted.Select(s => new ItemScore(s.TemplateId, s.Score)).ToList();
            compositeScore = ChecklistScoring.ComputeCompositeScore(scoredItems, scores);
            failures = ChecklistScoring.DetermineFailures(scoredItems, scores);

            foreach (var failure in failures)
            {
                var name = scoredItems.First(i => i.TemplateId == failure.TemplateId).Template?.Name ?? "Checklist item";
                corrective.Add(new CorrectiveActionDto(name, failure.CorrectiveActionText));
            }

            itemScoresJson = JsonSerializer.Serialize(submitted, JsonOptions);
        }

        var completion = new TaskCompletion
        {
            TenantId = task.TenantId,
            TaskInstanceId = task.Id,
            CompletedByUserId = cmd.CompletedByVolunteerName != null ? null : userId,
            CompletedByVolunteerName = cmd.CompletedByVolunteerName,
            FieldValuesJson = JsonSerializer.Serialize(cmd.FieldValues, JsonOptions),
            CorrectiveActionsJson = JsonSerializer.Serialize(corrective, JsonOptions),
            CompositeScorePercent = compositeScore,
            ItemScoresJson = itemScoresJson,
        };

        db.TaskCompletions.Add(completion);
        task.Status = "Completed";
        task.CompletedByUserId = completion.CompletedByUserId
            ?? (completion.CompletedByVolunteerName != null ? $"volunteer:{completion.CompletedByVolunteerName}" : null);
        task.CompletedAt = completion.CompletedAt;

        // Auto-corrective tasks (A4): each failed scored item spawns a standalone, claimable follow-up
        // task on the store board, linked back to this session. Inserted in the same transaction.
        var spawnedCorrectiveTaskIds = new List<Guid>();
        foreach (var failure in failures)
        {
            var itemName = scoredItems.First(i => i.TemplateId == failure.TemplateId).Template?.Name ?? "Checklist item";
            var corrTask = new TaskInstance
            {
                TenantId = task.TenantId,
                ChecklistId = null,
                SourceTaskInstanceId = task.Id,
                StoreId = task.StoreId,
                DueAt = completion.CompletedAt.AddHours(24),
                Status = "Pending",
                AssignedToUserId = null, // unassigned / claimable
                Notes = $"Corrective action for \"{itemName}\": {failure.CorrectiveActionText}",
                CreatedByUserId = "system",
            };
            db.TaskInstances.Add(corrTask);
            spawnedCorrectiveTaskIds.Add(corrTask.Id);
        }

        // Write inventory snapshots for Inventory-category templates (MDOG)
        await WriteInventorySnapshotsAsync(db, task.TenantId, task.StoreId, task.Checklist?.Items, cmd, completion.CompletedByUserId, ct);

        await db.SaveChangesAsync(ct);

        // Announce each spawned corrective task so open boards pick it up (same hub/group as TaskUpdated).
        foreach (var id in spawnedCorrectiveTaskIds)
        {
            await hub.Clients.Group($"store-{task.StoreId}").SendAsync(
                "TaskCreated", new { Id = id, task.StoreId }, ct);
        }

        await hub.Clients.Group($"store-{task.StoreId}").SendAsync(
            "TaskUpdated",
            new { task.Id, task.Status, HasCorrectiveActions = corrective.Count > 0 },
            ct);

        return new CompleteTaskResponse(
            new TaskCompletionResultDto(completion.Id, completion.TaskInstanceId, completion.CompletedByUserId, completion.CompletedByVolunteerName, completion.CompletedAt),
            corrective,
            compositeScore,
            spawnedCorrectiveTaskIds.Count > 0 ? spawnedCorrectiveTaskIds : null);
    }

    private static async Task WriteInventorySnapshotsAsync(
        TenantDbContext db,
        string tenantId,
        Guid storeId,
        IEnumerable<OpsFlow.Domain.Entities.ChecklistTemplateItem>? items,
        CompleteTaskCommand cmd,
        string? submittedByUserId,
        CancellationToken ct)
    {
        if (items == null) return;

        var inventoryItems = items.Where(i => i.Template?.Category == "Inventory").ToList();
        if (inventoryItems.Count == 0) return;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var item in inventoryItems)
        {
            var specs = JsonSerializer.Deserialize<List<TaskFieldValidator.TemplateFieldSpec>>(item.Template!.FieldsJson, JsonOptions) ?? [];
            foreach (var spec in specs.Where(s => s.Type == "Numeric"))
            {
                var submitted = cmd.FieldValues.FirstOrDefault(f => f.TemplateId == item.TemplateId && f.FieldId == spec.Id);
                if (submitted == null || !double.TryParse(submitted.Value, out var count)) continue;

                // Upsert: update if exists for today, otherwise insert
                var existing = await db.InventorySnapshots
                    .FirstOrDefaultAsync(s => s.StoreId == storeId && s.Date == today && s.ItemKey == spec.Id, ct);

                if (existing != null)
                {
                    existing.OnHandCount = count;
                    existing.UpdatedAt = DateTimeOffset.UtcNow;
                    existing.SubmittedByUserId = submittedByUserId;
                }
                else
                {
                    db.InventorySnapshots.Add(new OpsFlow.Domain.Entities.InventorySnapshot
                    {
                        TenantId = tenantId,
                        StoreId = storeId,
                        Date = today,
                        ItemKey = spec.Id,
                        OnHandCount = count,
                        SubmittedByUserId = submittedByUserId
                    });
                }
            }
        }
    }
}
