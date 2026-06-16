using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Features.DepositLog.RecordDeposit;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.DepositLog.GetDepositByDate;

internal sealed class GetDepositByDateHandler(
    TenantDbContextFactory factory) : IRequestHandler<GetDepositByDateQuery, DepositLogDto?>
{
    public async Task<DepositLogDto?> Handle(GetDepositByDateQuery query, CancellationToken ct)
    {
        await using var db = await factory.CreateAsync(ct);

        var dayStart = query.Date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dayEnd = query.Date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var deposit = await db.DepositLogs
            .Where(d => d.StoreId == query.StoreId && d.SubmittedAt >= dayStart && d.SubmittedAt <= dayEnd)
            .Select(d => new DepositLogDto(d.Id, d.StoreId, d.Amount, d.SubmittedByManagerId, d.SubmittedAt))
            .FirstOrDefaultAsync(ct);

        return deposit;
    }
}
