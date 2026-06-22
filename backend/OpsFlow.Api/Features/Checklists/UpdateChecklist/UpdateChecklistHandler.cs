using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Infrastructure;
using System.Security.Claims;

namespace OpsFlow.Api.Features.Checklists.UpdateChecklist;

internal sealed class UpdateChecklistHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<UpdateChecklistCommand>
{
    public async Task Handle(UpdateChecklistCommand cmd, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var role = user.FindFirstValue("role") ?? user.FindFirstValue(ClaimTypes.Role) ?? "";

        if (cmd.Scope == "System" && role != "admin")
            throw new UnauthorizedAccessException("Only admins can set System-scope checklists.");
        if (cmd.Scope == "Regional" && role != "admin" && role != "supervisor")
            throw new UnauthorizedAccessException("Regional checklists require supervisor or admin role.");

        await using var db = await factory.CreateAsync(ct);

        var checklist = await db.Checklists.FirstOrDefaultAsync(c => c.Id == cmd.Id, ct)
            ?? throw new KeyNotFoundException($"Checklist {cmd.Id} not found.");

        checklist.Name = cmd.Name;
        checklist.Description = cmd.Description;
        checklist.Scope = cmd.Scope;
        checklist.RegionId = cmd.RegionId;
        checklist.StoreId = cmd.StoreId;

        await db.SaveChangesAsync(ct);
    }
}
