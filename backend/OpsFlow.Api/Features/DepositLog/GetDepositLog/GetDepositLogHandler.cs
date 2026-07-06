using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Features.DepositLog.RecordDeposit;
using OpsFlow.Api.Security;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.DepositLog.GetDepositLog;

internal sealed class GetDepositLogHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<GetDepositLogQuery, GetDepositLogResponse>
{
    public async Task<GetDepositLogResponse> Handle(GetDepositLogQuery query, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var spec = user.ToCaller().Scope();

        await using var db = await factory.CreateAsync(ct);

        var store = await db.Stores.FindAsync([query.StoreId], ct)
            ?? throw new KeyNotFoundException($"Store {query.StoreId} not found.");
        var assigned = spec.IsStoreScoped
            && await db.UserStoreAssignments.AnyAsync(a => a.UserId == user.GetUserId() && a.StoreId == query.StoreId, ct);
        spec.AssertCanViewStore(store.RegionId, store.Id, assigned);

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
