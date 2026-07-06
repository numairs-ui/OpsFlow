using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Features.DepositLog.RecordDeposit;
using OpsFlow.Api.Security;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.DepositLog.GetDepositByDate;

internal sealed class GetDepositByDateHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<GetDepositByDateQuery, DepositLogDto?>
{
    public async Task<DepositLogDto?> Handle(GetDepositByDateQuery query, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var spec = user.ToCaller().Scope();

        await using var db = await factory.CreateAsync(ct);

        var store = await db.Stores.FindAsync([query.StoreId], ct)
            ?? throw new KeyNotFoundException($"Store {query.StoreId} not found.");
        var assigned = spec.IsStoreScoped
            && await db.UserStoreAssignments.AnyAsync(a => a.UserId == user.GetUserId() && a.StoreId == query.StoreId, ct);
        spec.AssertCanViewStore(store.RegionId, store.Id, assigned);

        var dayStart = query.Date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dayEnd = query.Date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var deposit = await db.DepositLogs
            .Where(d => d.StoreId == query.StoreId && d.SubmittedAt >= dayStart && d.SubmittedAt <= dayEnd)
            .Select(d => new DepositLogDto(d.Id, d.StoreId, d.Amount, d.SubmittedByManagerId, d.SubmittedAt))
            .FirstOrDefaultAsync(ct);

        return deposit;
    }
}
