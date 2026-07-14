using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Security;
using OpsFlow.Domain.Authorization;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Tasks.GetTask;

internal sealed class GetTaskHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<GetTaskQuery, TaskDetailDto>
{
    public async Task<TaskDetailDto> Handle(GetTaskQuery query, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var spec = user.ToCaller().Scope();
        var userId = user.GetUserId();

        await using var db = await factory.CreateAsync(ct);

        var task = await db.TaskInstances
            .Include(t => t.Checklist)
                .ThenInclude(c => c!.Items.OrderBy(i => i.Order))
                    .ThenInclude(i => i.Template)
            .Include(t => t.AdHocTaskTemplate)
            .Include(t => t.Store)
            .Include(t => t.RecurringAssignment)
            .FirstOrDefaultAsync(t => t.Id == query.TaskId, ct)
            ?? throw new KeyNotFoundException($"Task {query.TaskId} not found.");

        // Auth: verify the caller may view the task's store (region set, or own/assigned store)
        var assigned = spec.IsStoreScoped
            && await db.UserStoreAssignments.AnyAsync(a => a.UserId == userId && a.StoreId == task.StoreId, ct);
        spec.AssertCanViewStore(task.Store!.RegionId, task.StoreId, assigned);

        // Checklist-backed tasks expose every item's template; a standalone task exposes its single
        // ad-hoc template (mode a); a notes-only task exposes none.
        List<TaskTemplateItemDto> templates;
        if (task.Checklist is not null)
        {
            templates = task.Checklist.Items
                .Select(i => new TaskTemplateItemDto(
                    i.TemplateId,
                    i.Template?.Name ?? "",
                    i.Order,
                    i.Template?.FieldsJson ?? "[]",
                    i.ScoringType,
                    i.PhotoRequired,
                    i.FailScoreThreshold
                ))
                .ToList();
        }
        else if (task.AdHocTaskTemplate is { } adHoc)
        {
            templates = [new TaskTemplateItemDto(adHoc.Id, adHoc.Name, 0, adHoc.FieldsJson)];
        }
        else
        {
            templates = [];
        }

        var isMdog = task.Checklist?.Items.Any(i => i.Template?.Category == "Inventory") ?? false;

        var previousValues = new Dictionary<string, double>();
        if (isMdog)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var snapshots = await db.InventorySnapshots
                .Where(s => s.StoreId == task.StoreId && s.Date < today)
                .GroupBy(s => s.ItemKey)
                .Select(g => g.OrderByDescending(s => s.Date).First())
                .ToListAsync(ct);

            foreach (var s in snapshots)
                previousValues[s.ItemKey] = s.OnHandCount;
        }

        return new TaskDetailDto(
            task.Id,
            task.RecurringAssignmentId,
            task.RecurringAssignment?.Name,
            task.ChecklistId,
            task.Checklist?.Name ?? task.AdHocTaskTemplate?.Name ?? "Standalone Task",
            task.Checklist?.Description,
            task.StoreId,
            task.Store?.Name ?? "",
            task.DueAt,
            task.Status,
            task.AssignedToUserId,
            task.Notes,
            task.RecurringAssignmentId == null,
            templates,
            task.CreatedAt,
            isMdog,
            previousValues
        );
    }
}
