using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Security;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Users.GetStoreAssignments;

internal sealed class GetStoreAssignmentsHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<GetStoreAssignmentsQuery, List<StoreAssignmentDto>>
{
    public async Task<List<StoreAssignmentDto>> Handle(GetStoreAssignmentsQuery query, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var spec = user.ToCaller().Scope();
        var callerId = user.GetUserId();

        await using var db = await factory.CreateAsync(ct);

        // Authorize: anyone may read their own store assignments. Reading another user's is a
        // management function — same rule as GetUserActivityHandler/GetUserHandler.
        if (query.UserId != callerId)
        {
            if (spec.IsStoreScoped)
                throw new UnauthorizedAccessException("You can only view your own store assignments.");

            var target = await db.UserProfiles
                .Where(p => p.UserId == query.UserId)
                .Select(p => new { p.StoreId, p.RegionId })
                .FirstOrDefaultAsync(ct)
                ?? throw new KeyNotFoundException($"User {query.UserId} not found.");

            if (target.StoreId is { } storeId)
            {
                var storeRegionId = await db.Stores
                    .Where(s => s.Id == storeId)
                    .Select(s => (Guid?)s.RegionId)
                    .FirstOrDefaultAsync(ct);
                spec.AssertCanViewStore(storeRegionId ?? Guid.Empty, storeId);
            }
            else if (target.RegionId is { } regionId)
                spec.AssertCanViewRegion(regionId);
            else
                spec.AssertGlobal();
        }

        return await db.UserStoreAssignments
            .Include(a => a.Store).ThenInclude(s => s.Region)
            .Where(a => a.UserId == query.UserId)
            .OrderBy(a => a.Store.Name)
            .Select(a => new StoreAssignmentDto(
                a.StoreId, a.Store.Name, a.Store.Region.Name, a.AssignedAt))
            .ToListAsync(ct);
    }
}
