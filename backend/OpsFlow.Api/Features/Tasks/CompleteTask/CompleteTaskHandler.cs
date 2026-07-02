using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Hubs;
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
                existingActions);
        }

        if (task.Status is "Cancelled" or "Deferred")
            throw new InvalidOperationException($"Cannot complete a task in status '{task.Status}'.");

        var storeSettings = await db.StoreSettings.FindAsync([task.StoreId], ct);
        var validation = TaskFieldValidator.Validate(task.Checklist?.Items ?? [], cmd.FieldValues, storeSettings);

        if (validation.Errors.Count > 0)
            throw new ValidationException(validation.Errors);

        var corrective = validation.CorrectiveActions;

        var completion = new TaskCompletion
        {
            TenantId = task.TenantId,
            TaskInstanceId = task.Id,
            CompletedByUserId = cmd.CompletedByVolunteerName != null ? null : userId,
            CompletedByVolunteerName = cmd.CompletedByVolunteerName,
            FieldValuesJson = JsonSerializer.Serialize(cmd.FieldValues, JsonOptions),
            CorrectiveActionsJson = JsonSerializer.Serialize(corrective, JsonOptions)
        };

        db.TaskCompletions.Add(completion);
        task.Status = "Completed";
        task.CompletedByUserId = completion.CompletedByUserId
            ?? (completion.CompletedByVolunteerName != null ? $"volunteer:{completion.CompletedByVolunteerName}" : null);
        task.CompletedAt = completion.CompletedAt;

        // Write inventory snapshots for Inventory-category templates (MDOG)
        await WriteInventorySnapshotsAsync(db, task.TenantId, task.StoreId, task.Checklist?.Items, cmd, completion.CompletedByUserId, ct);

        await db.SaveChangesAsync(ct);

        await hub.Clients.Group($"store-{task.StoreId}").SendAsync(
            "TaskUpdated",
            new { task.Id, task.Status, HasCorrectiveActions = corrective.Count > 0 },
            ct);

        return new CompleteTaskResponse(
            new TaskCompletionResultDto(completion.Id, completion.TaskInstanceId, completion.CompletedByUserId, completion.CompletedByVolunteerName, completion.CompletedAt),
            corrective);
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
