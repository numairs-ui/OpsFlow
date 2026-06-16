using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Infrastructure;
using System.Security.Claims;

namespace OpsFlow.Api.Features.Tasks.GetTask;

internal sealed class GetTaskHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<GetTaskQuery, TaskDetailDto>
{
    public async Task<TaskDetailDto> Handle(GetTaskQuery query, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var role = user.FindFirstValue("role") ?? "";
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub")!;

        await using var db = await factory.CreateAsync(ct);

        var task = await db.TaskInstances
            .Include(t => t.Checklist)
                .ThenInclude(c => c!.Items.OrderBy(i => i.Order))
                    .ThenInclude(i => i.Template)
            .Include(t => t.Store)
            .Include(t => t.RecurringAssignment)
            .FirstOrDefaultAsync(t => t.Id == query.TaskId, ct)
            ?? throw new KeyNotFoundException($"Task {query.TaskId} not found.");

        // Auth: verify user has access to this store
        if (role == "store_manager" || role == "store_employee")
        {
            var up = await db.UserProfiles.FindAsync([userId], ct);
            var isAssigned = up?.StoreId == task.StoreId
                || await db.UserStoreAssignments.AnyAsync(a => a.UserId == userId && a.StoreId == task.StoreId, ct);
            if (!isAssigned)
                throw new UnauthorizedAccessException("You do not have access to this task.");
        }

        var templates = task.Checklist?.Items
            .Select(i => new TaskTemplateItemDto(
                i.TemplateId,
                i.Template?.Name ?? "",
                i.Order,
                i.Template?.FieldsJson ?? "[]"
            ))
            .ToList() ?? [];

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
            task.Checklist?.Name ?? "",
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
