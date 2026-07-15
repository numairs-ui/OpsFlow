using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Hubs;
using OpsFlow.Api.Security;
using OpsFlow.Infrastructure;
using System.Security.Claims;

namespace OpsFlow.Api.Features.DepositLog.RecordDeposit;

internal sealed class RecordDepositHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor,
    IHubContext<TaskBoardHub> hub) : IRequestHandler<RecordDepositCommand, DepositLogDto>
{
    public async Task<DepositLogDto> Handle(RecordDepositCommand cmd, CancellationToken ct)
    {
        if (cmd.Amount <= 0)
            throw new ArgumentException("Deposit amount must be greater than zero.");

        var user = httpContextAccessor.HttpContext!.User;
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("User not authenticated.");
        var tenantId = user.FindFirstValue("tenantId")
            ?? httpContextAccessor.HttpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault()
            ?? throw new UnauthorizedAccessException("tenantId claim missing.");

        var spec = user.ToCaller().Scope();

        await using var db = await factory.CreateAsync(ct);

        var store = await db.Stores.FindAsync([cmd.StoreId], ct)
            ?? throw new KeyNotFoundException($"Store {cmd.StoreId} not found.");
        var assigned = spec.IsStoreScoped
            && await db.UserStoreAssignments.AnyAsync(a => a.UserId == userId && a.StoreId == cmd.StoreId, ct);
        spec.AssertCanManageStore(store.RegionId, store.Id, assigned);

        var today = DateTimeOffset.UtcNow.Date;
        var alreadyExists = await db.DepositLogs
            .AnyAsync(d => d.StoreId == cmd.StoreId && d.SubmittedAt.Date == today, ct);

        if (alreadyExists)
            throw new InvalidOperationException("A deposit has already been recorded for this store today.");

        var deposit = new OpsFlow.Domain.Entities.DepositLog
        {
            TenantId = tenantId,
            StoreId = cmd.StoreId,
            Amount = cmd.Amount,
            SubmittedByManagerId = userId
        };

        db.DepositLogs.Add(deposit);
        await db.SaveChangesAsync(ct);

        await hub.Clients.Group($"store-{cmd.StoreId}").SendAsync(
            "DepositRecorded",
            new { deposit.Id, deposit.StoreId, deposit.Amount, deposit.SubmittedAt },
            ct);

        return new DepositLogDto(deposit.Id, deposit.StoreId, deposit.Amount, deposit.SubmittedByManagerId, deposit.SubmittedAt);
    }
}
