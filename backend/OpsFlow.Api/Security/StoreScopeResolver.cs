using Microsoft.EntityFrameworkCore;
using OpsFlow.Domain.Authorization;
using OpsFlow.Infrastructure;

namespace OpsFlow.Api.Security;

/// <summary>
/// Bridges the pure <see cref="ScopeSpec.AssertCanWriteScope(string, System.Guid?, System.Guid?, System.Guid?)"/>
/// to the tenant DB: for a Store-scope write it resolves the target store's region so the spec can apply
/// the store-management rule. One place owns that resolution so every scoped-write handler stays a one-liner
/// and no handler can forget the Store branch (the gap that let store_employee/store_kiosk write foreign stores).
/// </summary>
internal static class StoreScopeResolver
{
    public static async Task AssertCanWriteScopeAsync(
        this ScopeSpec spec, TenantDbContext db, string scope, Guid? regionId, Guid? storeId, CancellationToken ct)
    {
        Guid? storeRegionId = null;
        if (scope == "Store")
        {
            if (storeId is not { } sid)
                throw new UnauthorizedAccessException("Store-scope writes require a store.");
            var store = await db.Stores
                .Where(s => s.Id == sid)
                .Select(s => new { s.RegionId })
                .FirstOrDefaultAsync(ct)
                ?? throw new KeyNotFoundException($"Store {sid} not found.");
            storeRegionId = store.RegionId;
        }

        spec.AssertCanWriteScope(scope, regionId, storeId, storeRegionId);
    }
}
