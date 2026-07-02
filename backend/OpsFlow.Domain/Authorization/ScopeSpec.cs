namespace OpsFlow.Domain.Authorization;

/// <summary>
/// A caller's authorization reach, derived purely from their <see cref="Caller"/>.
/// One place owns "what can this role see and act on"; handlers ask through the
/// assertions here and the query extensions in <see cref="ScopeQueryExtensions"/>.
/// </summary>
public sealed class ScopeSpec
{
    public string Role { get; }
    /// <summary>super_admin — unrestricted.</summary>
    public bool IsGlobal { get; }
    /// <summary>store_manager / store_employee / store_kiosk — bound to one store.</summary>
    public bool IsStoreScoped { get; }
    /// <summary>admin / supervisor — bound to a set of regions.</summary>
    public bool IsRegionScoped { get; }
    public Guid? StoreId { get; }
    public IReadOnlyList<Guid> RegionIds { get; }

    private ScopeSpec(string role, bool isGlobal, bool isStoreScoped, bool isRegionScoped,
        Guid? storeId, IReadOnlyList<Guid> regionIds)
    {
        Role = role;
        IsGlobal = isGlobal;
        IsStoreScoped = isStoreScoped;
        IsRegionScoped = isRegionScoped;
        StoreId = storeId;
        RegionIds = regionIds;
    }

    public static ScopeSpec From(Caller caller) => new(
        caller.Role,
        isGlobal: Roles.IsSuperAdmin(caller.Role),
        isStoreScoped: Roles.IsStoreScoped(caller.Role),
        isRegionScoped: caller.Role is Roles.Admin or Roles.Supervisor,
        storeId: caller.StoreId,
        regionIds: caller.RegionIds);

    /// <summary>True when the caller may operate on the given region (super_admin always; admin/supervisor when in set).</summary>
    public bool CanActOnRegion(Guid? regionId) =>
        IsGlobal || (regionId is { } r && RegionIds.Contains(r));

    /// <summary>Read access to a region's rollup (super_admin, or a region role assigned to it).</summary>
    public void AssertCanViewRegion(Guid regionId)
    {
        if (!CanActOnRegion(regionId)) throw Denied("You do not have access to this region.");
    }

    /// <summary>Network-wide access (the system dashboard, tenant settings, System-scope writes).</summary>
    public void AssertGlobal()
    {
        if (!IsGlobal) throw Denied("Super admin role required.");
    }

    /// <summary>
    /// Read access to a store's data: any store-scoped role for its own (or an explicitly assigned)
    /// store, a region role when the store's region is in its set, super_admin always.
    /// <paramref name="assignedToStore"/> lets a caller pass a resolved UserStoreAssignment match
    /// (managers can hold several stores); ignored for non-store roles.
    /// </summary>
    public bool CanViewStore(Guid storeRegionId, Guid storeId, bool assignedToStore = false)
    {
        if (IsGlobal) return true;
        if (IsStoreScoped) return StoreId == storeId || assignedToStore;
        if (IsRegionScoped) return RegionIds.Contains(storeRegionId);
        return false;
    }

    public void AssertCanViewStore(Guid storeRegionId, Guid storeId, bool assignedToStore = false)
    {
        if (!CanViewStore(storeRegionId, storeId, assignedToStore))
            throw Denied("You do not have access to this store.");
    }

    /// <summary>
    /// Write access at a store (create/assign tasks, recurring assignments): store_manager for its
    /// own (or an explicitly assigned) store, a region role when the store's region is in its set,
    /// super_admin always. store_employee and store_kiosk are denied.
    /// </summary>
    public void AssertCanManageStore(Guid storeRegionId, Guid storeId, bool assignedToStore = false)
    {
        if (IsGlobal) return;
        if (Role == Roles.StoreManager)
        {
            if (StoreId == storeId || assignedToStore) return;
            throw Denied("You can only manage your own store.");
        }
        if (IsRegionScoped)
        {
            if (RegionIds.Contains(storeRegionId)) return;
            throw Denied("Store is not in your region.");
        }
        throw Denied("You are not allowed to manage this store.");
    }

    /// <summary>
    /// Write access to a scoped resource (template, checklist, form template). System requires
    /// super_admin; Regional requires super_admin or a region role assigned to that region; Store
    /// requires store-management rights at that store (store_manager for its own/assigned store, a
    /// region role whose set contains the store's region, super_admin always). store_employee and
    /// store_kiosk are denied Store-scope writes. Supply <paramref name="storeId"/>/
    /// <paramref name="storeRegionId"/> for Store scope (the handler resolves the store's region).
    /// </summary>
    public void AssertCanWriteScope(string scope, Guid? regionId, Guid? storeId = null, Guid? storeRegionId = null)
    {
        if (scope == "System" && !IsGlobal)
            throw Denied("Only super admins can write System-scope resources.");
        if (scope == "Regional" && !CanActOnRegion(regionId))
            throw Denied("Regional resources require super_admin, or an admin/supervisor assigned to that region.");
        if (scope == "Store")
        {
            if (storeId is not { } sid || storeRegionId is not { } srid)
                throw Denied("Store-scope writes require the target store.");
            AssertCanManageStore(srid, sid);
        }
    }

    private static UnauthorizedAccessException Denied(string message) => new(message);
}
