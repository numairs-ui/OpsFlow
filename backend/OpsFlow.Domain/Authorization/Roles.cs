namespace OpsFlow.Domain.Authorization;

/// <summary>
/// Canonical role identifiers used across auth metadata, JWT claims, and handler authorization.
/// Six roles, scoped from network-wide down to a single store:
///   super_admin  — full system access (org structure, all regions/stores/users, tenant settings, System-scope templates)
///   admin        — region-scoped: manages stores/users/templates within an assigned SET of regions
///   supervisor   — single region: regional/store templates, monitors stores, reviews forms
///   store_manager— single store: recurring tasks, deposits, roster, forms
///   store_employee — single store: claim/start/complete/defer tasks, submit forms
///   store_kiosk  — single store, shared station: read today's board + claim-by-name only
/// </summary>
public static class Roles
{
    public const string SuperAdmin = "super_admin";
    public const string Admin = "admin";
    public const string Supervisor = "supervisor";
    public const string StoreManager = "store_manager";
    public const string StoreEmployee = "store_employee";
    public const string StoreKiosk = "store_kiosk";

    public static readonly string[] All =
        [SuperAdmin, Admin, Supervisor, StoreManager, StoreEmployee, StoreKiosk];

    /// <summary>Roles whose data access is scoped to a single assigned store.</summary>
    public static readonly string[] StoreScoped =
        [StoreManager, StoreEmployee, StoreKiosk];

    /// <summary>True for the unscoped, full-access role only.</summary>
    public static bool IsSuperAdmin(string? role) => role == SuperAdmin;

    /// <summary>True for the region-scoped admin role.</summary>
    public static bool IsAdmin(string? role) => role == Admin;

    /// <summary>True when the role's data access is bounded to a single store.</summary>
    public static bool IsStoreScoped(string? role) => Array.IndexOf(StoreScoped, role) >= 0;

    /// <summary>
    /// Region-set authorization: can a user acting as <paramref name="role"/> with the given
    /// <paramref name="regionIds"/> scope operate on the region <paramref name="targetRegionId"/>?
    /// super_admin: always. admin/supervisor: only when the target region is in their set.
    /// </summary>
    public static bool CanActOnRegion(string? role, IReadOnlyCollection<string> regionIds, string? targetRegionId)
    {
        if (IsSuperAdmin(role)) return true;
        if (targetRegionId is null) return false;
        return regionIds.Contains(targetRegionId);
    }
}
