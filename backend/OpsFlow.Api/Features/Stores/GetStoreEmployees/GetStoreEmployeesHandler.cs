using MediatR;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Infrastructure;
using System.Security.Claims;

namespace OpsFlow.Api.Features.Stores.GetStoreEmployees;

internal sealed class GetStoreEmployeesHandler(
    TenantDbContextFactory factory,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<GetStoreEmployeesQuery, List<StoreEmployeeDto>>
{
    public async Task<List<StoreEmployeeDto>> Handle(GetStoreEmployeesQuery query, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext!.User;
        var role = user.FindFirstValue("role") ?? user.FindFirstValue(ClaimTypes.Role) ?? "";
        var userStoreId = user.FindFirstValue("storeId");
        var userRegionId = user.FindFirstValue("regionId");

        await using var db = await factory.CreateAsync(ct);

        // Authorization: admin sees all; supervisor checks region; store_manager checks store assignment
        if (role == "store_manager")
        {
            var isAssigned = userStoreId == query.StoreId.ToString()
                || await db.UserStoreAssignments.AnyAsync(
                    a => a.UserId == user.FindFirstValue(ClaimTypes.NameIdentifier)
                      && a.StoreId == query.StoreId, ct);
            if (!isAssigned)
                throw new UnauthorizedAccessException("Store Manager is not assigned to this store.");
        }
        else if (role == "supervisor")
        {
            if (!string.IsNullOrEmpty(userRegionId))
            {
                var storeInRegion = await db.Stores.AnyAsync(
                    s => s.Id == query.StoreId && s.Region.Id.ToString() == userRegionId, ct);
                if (!storeInRegion)
                    throw new UnauthorizedAccessException("Store is not in the Supervisor's region.");
            }
        }

        return await db.UserProfiles
            .Where(u => u.StoreId == query.StoreId && u.IsActive &&
                        (u.Role == "store_employee" || u.Role == "store_manager"))
            .OrderBy(u => u.DisplayName)
            .Select(u => new StoreEmployeeDto(
                u.UserId, u.Email, u.DisplayName, u.Role, u.IsActive, u.CreatedAt))
            .ToListAsync(ct);
    }
}
