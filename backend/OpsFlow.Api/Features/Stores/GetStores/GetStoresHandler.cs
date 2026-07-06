using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Security;
using OpsFlow.Domain.Authorization;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Stores.GetStores;

internal sealed class GetStoresHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<GetStoresQuery, List<StoreDto>>
{
    public async Task<List<StoreDto>> Handle(GetStoresQuery query, CancellationToken ct)
    {
        var spec = httpContextAccessor.HttpContext!.User.ToCaller().Scope();

        await using var db = await factory.CreateAsync(ct);
        return await db.Stores
            .Include(s => s.Region)
            .Where(s => (!query.ActiveOnly || s.IsActive)
                     && (query.RegionId == null || s.RegionId == query.RegionId))
            .WhereStoreInScope(spec, s => s.Id, s => s.RegionId)
            .OrderBy(s => s.Region.Name).ThenBy(s => s.Name)
            .Select(s => new StoreDto(s.Id, s.Name, s.Address, s.RegionId, s.Region.Name, s.IsActive, s.CreatedAt))
            .ToListAsync(ct);
    }
}
