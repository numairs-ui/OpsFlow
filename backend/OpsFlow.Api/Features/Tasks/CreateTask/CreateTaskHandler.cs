using MediatR;
using OpsFlow.Domain.Entities;
using OpsFlow.Infrastructure;
using System.Security.Claims;

namespace OpsFlow.Api.Features.Tasks.CreateTask;

internal sealed class CreateTaskHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<CreateTaskCommand, Guid>
{
    public async Task<Guid> Handle(CreateTaskCommand cmd, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var tenantId = user.FindFirstValue("tenantId")!;
        var role = user.FindFirstValue("role") ?? "";
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub")!;

        await using var db = await factory.CreateAsync(ct);

        // store_manager can only create tasks for their own store
        if (role == "store_manager")
        {
            var up = await db.UserProfiles.FindAsync([userId], ct);
            if (up?.StoreId != cmd.StoreId)
                throw new UnauthorizedAccessException("Store managers can only create tasks for their own store.");
        }

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
