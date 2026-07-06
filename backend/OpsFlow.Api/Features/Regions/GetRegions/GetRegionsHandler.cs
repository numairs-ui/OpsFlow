using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Security;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Regions.GetRegions;

internal sealed class GetRegionsHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<GetRegionsQuery, List<RegionDto>>
{
    public async Task<List<RegionDto>> Handle(GetRegionsQuery query, CancellationToken ct)
    {
        var spec = httpContextAccessor.HttpContext!.User.ToCaller().Scope();

        await using var db = await factory.CreateAsync(ct);
        return await db.Regions
            .Where(r => !query.ActiveOnly || r.IsActive)
            .Where(r => spec.IsGlobal || spec.RegionIds.Contains(r.Id))
            .OrderBy(r => r.Name)
            .Select(r => new RegionDto(r.Id, r.Name, r.Description, r.IsActive, r.CreatedAt))
            .ToListAsync(ct);
    }
}
