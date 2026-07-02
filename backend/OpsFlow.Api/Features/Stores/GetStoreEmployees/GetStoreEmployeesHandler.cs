using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Api.Security;
using OpsFlow.Domain.Authorization;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Features.Stores.GetStoreEmployees;

internal sealed class GetStoreEmployeesHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<GetStoreEmployeesQuery, List<StoreEmployeeDto>>
{
    public async Task<List<StoreEmployeeDto>> Handle(GetStoreEmployeesQuery query, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var spec = user.ToCaller().Scope();
        var userId = user.GetUserId();

        await using var db = await factory.CreateAsync(ct);

        // The caller must be able to view this store's roster (region set, or own/assigned store).
        var store = await db.Stores.FindAsync([query.StoreId], ct)
            ?? throw new KeyNotFoundException($"Store {query.StoreId} not found.");
        var assigned = spec.IsStoreScoped
            && await db.UserStoreAssignments.AnyAsync(a => a.UserId == userId && a.StoreId == query.StoreId, ct);
        spec.AssertCanViewStore(store.RegionId, store.Id, assigned);

        return await db.UserProfiles
            .Where(u => u.StoreId == query.StoreId && u.IsActive &&
                        (u.Role == "store_employee" || u.Role == "store_manager"))
            .OrderBy(u => u.DisplayName)
            .Select(u => new StoreEmployeeDto(
                u.UserId, u.Email, u.DisplayName, u.Role, u.IsActive, u.CreatedAt))
            .ToListAsync(ct);
    }
}
