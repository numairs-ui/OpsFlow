using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Features.DepositLog.RecordDeposit;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.DepositLog.GetDepositLog;

internal sealed class GetDepositLogHandler(
    TenantDbContextFactory factory) : IRequestHandler<GetDepositLogQuery, GetDepositLogResponse>
{
    public async Task<GetDepositLogResponse> Handle(GetDepositLogQuery query, CancellationToken ct)
    {
        await using var db = await factory.CreateAsync(ct);

        var q = db.DepositLogs
            .Where(d => d.StoreId == query.StoreId)
            .AsQueryable();

        if (query.From.HasValue)
        {
            var fromDto = query.From.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            q = q.Where(d => d.SubmittedAt >= fromDto);
        }
        if (query.To.HasValue)
        {
            var toDto = query.To.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            q = q.Where(d => d.SubmittedAt <= toDto);
        }

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(d => d.SubmittedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(d => new DepositLogDto(d.Id, d.StoreId, d.Amount, d.SubmittedByManagerId, d.SubmittedAt))
            .ToListAsync(ct);

        return new GetDepositLogResponse(items, total, query.Page, query.PageSize);
    }
}
