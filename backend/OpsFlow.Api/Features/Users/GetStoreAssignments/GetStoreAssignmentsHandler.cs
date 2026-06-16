using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Users.GetStoreAssignments;

internal sealed class GetStoreAssignmentsHandler(TenantDbContextFactory factory)
    : IRequestHandler<GetStoreAssignmentsQuery, List<StoreAssignmentDto>>
{
    public async Task<List<StoreAssignmentDto>> Handle(GetStoreAssignmentsQuery query, CancellationToken ct)
    {
        await using var db = await factory.CreateAsync(ct);
        return await db.UserStoreAssignments
            .Include(a => a.Store).ThenInclude(s => s.Region)
            .Where(a => a.UserId == query.UserId)
            .OrderBy(a => a.Store.Name)
            .Select(a => new StoreAssignmentDto(
                a.StoreId, a.Store.Name, a.Store.Region.Name, a.AssignedAt))
            .ToListAsync(ct);
    }
}
