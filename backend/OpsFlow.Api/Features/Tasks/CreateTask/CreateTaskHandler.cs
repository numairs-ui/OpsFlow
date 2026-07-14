using MediatR;
using OpsFlow.Api.Security;
using OpsFlow.Domain.Authorization;
using OpsFlow.Domain.Entities;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Tasks.CreateTask;

internal sealed class CreateTaskHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<CreateTaskCommand, Guid>
{
    public async Task<Guid> Handle(CreateTaskCommand cmd, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var tenantId = user.GetTenantId();
        var userId = user.GetUserId();

        await using var db = await factory.CreateAsync(ct);

        // Authorize the target store by role scope (super_admin unrestricted; employee/kiosk denied)
        var store = await db.Stores.FindAsync([cmd.StoreId], ct)
            ?? throw new KeyNotFoundException($"Store {cmd.StoreId} not found.");
        user.ToCaller().Scope().AssertCanManageStore(store.RegionId, store.Id);

        // Branch on creation mode. Only the checklist-backed path needs the active-checklist check.
        if (cmd.ChecklistId is { } checklistId)
        {
            var checklist = await db.Checklists.FindAsync([checklistId], ct)
                ?? throw new KeyNotFoundException($"Checklist {checklistId} not found.");
            if (!checklist.IsActive)
                throw new InvalidOperationException("Cannot create a task for an inactive checklist.");
        }
        else if (cmd.TaskTemplateId is { } templateId)
        {
            var template = await db.TaskTemplates.FindAsync([templateId], ct)
                ?? throw new KeyNotFoundException($"Template {templateId} not found.");
            if (!template.IsActive)
                throw new InvalidOperationException("Cannot create a task for an inactive template.");
        }
        // else: notes-only task — no structured source to validate.

        var instance = new TaskInstance
        {
            TenantId = tenantId,
            ChecklistId = cmd.ChecklistId,
            AdHocTaskTemplateId = cmd.TaskTemplateId,
            StoreId = cmd.StoreId,
            DueAt = cmd.DueAt,
            Notes = cmd.Notes,
            AssignedToUserId = cmd.AssignedToUserId,
            CreatedByUserId = userId,
        };

        db.TaskInstances.Add(instance);
        await db.SaveChangesAsync(ct);
        return instance.Id;
    }
}
