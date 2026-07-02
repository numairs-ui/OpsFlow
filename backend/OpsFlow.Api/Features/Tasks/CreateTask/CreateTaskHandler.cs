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

        var checklist = await db.Checklists.FindAsync([cmd.ChecklistId], ct)
            ?? throw new KeyNotFoundException($"Checklist {cmd.ChecklistId} not found.");
        if (!checklist.IsActive)
            throw new InvalidOperationException("Cannot create a task for an inactive checklist.");

        var instance = new TaskInstance
        {
            TenantId = tenantId,
            ChecklistId = cmd.ChecklistId,
            StoreId = cmd.StoreId,
            DueAt = cmd.DueAt,
            Notes = cmd.Notes,
            CreatedByUserId = userId,
        };

        db.TaskInstances.Add(instance);
        await db.SaveChangesAsync(ct);
        return instance.Id;
    }
}
