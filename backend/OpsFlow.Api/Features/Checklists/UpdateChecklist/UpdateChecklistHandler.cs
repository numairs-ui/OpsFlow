using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Security;
using OpsFlow.Domain.Authorization;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Checklists.UpdateChecklist;

internal sealed class UpdateChecklistHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<UpdateChecklistCommand>
{
    public async Task Handle(UpdateChecklistCommand cmd, CancellationToken ct)
    {
        httpContextAccessor.HttpContext!.User.ToCaller().Scope().AssertCanWriteScope(cmd.Scope, cmd.RegionId);

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
