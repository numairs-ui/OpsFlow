using FluentAssertions;
using OpsFlow.Domain.Authorization;
using Xunit;

namespace OpsFlow.Tests.Unit.Authorization;

/// <summary>
/// The scope module is pure, so it is tested directly against in-memory queryables and values —
/// no DbContext, no ClaimsPrincipal. LINQ-to-objects runs the very expressions EF would translate.
/// </summary>
public sealed class ScopeSpecTests
{
    private static readonly Guid R1 = Guid.NewGuid();
    private static readonly Guid R2 = Guid.NewGuid();
    private static readonly Guid S1 = Guid.NewGuid(); // in R1
    private static readonly Guid S2 = Guid.NewGuid(); // in R2

    private sealed record StoreRow(Guid StoreId, Guid RegionId);
    private sealed record ScopedRow(string Scope, Guid? RegionId, Guid? StoreId);

    private static ScopeSpec Spec(string role, Guid? storeId = null, params Guid[] regionIds) =>
        new Caller(role, storeId, regionIds).Scope();

    private static readonly StoreRow[] Stores = [new(S1, R1), new(S2, R2)];

    private static List<Guid> VisibleStores(ScopeSpec spec) =>
        Stores.AsQueryable()
            .WhereStoreInScope(spec, x => x.StoreId, x => x.RegionId)
            .Select(x => x.StoreId).ToList();

    // ── WhereStoreInScope ─────────────────────────────────────────────────────

    [Fact]
    public void SuperAdmin_sees_all_stores() =>
        VisibleStores(Spec(Roles.SuperAdmin)).Should().BeEquivalentTo([S1, S2]);

    [Fact]
    public void Admin_sees_only_stores_in_its_region_set()
    {
        VisibleStores(Spec(Roles.Admin, null, R1)).Should().BeEquivalentTo([S1]);
        VisibleStores(Spec(Roles.Admin, null, R1, R2)).Should().BeEquivalentTo([S1, S2]);
    }

    [Fact]
    public void Supervisor_sees_only_its_region() =>
        VisibleStores(Spec(Roles.Supervisor, null, R1)).Should().BeEquivalentTo([S1]);

    [Theory]
    [InlineData(Roles.StoreManager)]
    [InlineData(Roles.StoreEmployee)]
    [InlineData(Roles.StoreKiosk)]
    public void StoreScoped_roles_see_only_their_own_store(string role) =>
        VisibleStores(Spec(role, S1)).Should().BeEquivalentTo([S1]);

    // ── WhereScopedVisible ────────────────────────────────────────────────────

    private static readonly ScopedRow[] Scoped =
    [
        new("System", null, null),
        new("Regional", R1, null),
        new("Regional", R2, null),
        new("Store", null, S1),
        new("Store", null, S2),
    ];

    private static List<ScopedRow> Visible(ScopeSpec spec) =>
        Scoped.AsQueryable()
            .WhereScopedVisible(spec, x => x.Scope, x => x.RegionId, x => x.StoreId)
            .ToList();

    [Fact]
    public void SuperAdmin_sees_every_scoped_resource() =>
        Visible(Spec(Roles.SuperAdmin)).Should().HaveCount(5);

    [Fact]
    public void Admin_sees_system_plus_its_regional()
    {
        var v = Visible(Spec(Roles.Admin, null, R1));
        v.Should().BeEquivalentTo([new ScopedRow("System", null, null), new ScopedRow("Regional", R1, null)]);
    }

    [Fact]
    public void Supervisor_sees_system_plus_its_regional() =>
        Visible(Spec(Roles.Supervisor, null, R1)).Should().HaveCount(2);

    [Fact]
    public void StoreManager_sees_system_plus_its_store()
    {
        var v = Visible(Spec(Roles.StoreManager, S1));
        v.Should().BeEquivalentTo([new ScopedRow("System", null, null), new ScopedRow("Store", null, S1)]);
    }

    // ── AssertCanViewStore ────────────────────────────────────────────────────

    [Fact]
    public void View_super_admin_anywhere() =>
        FluentActions.Invoking(() => Spec(Roles.SuperAdmin).AssertCanViewStore(R2, S2)).Should().NotThrow();

    [Fact]
    public void View_admin_in_region_ok_out_of_region_denied()
    {
        FluentActions.Invoking(() => Spec(Roles.Admin, null, R1).AssertCanViewStore(R1, S1)).Should().NotThrow();
        FluentActions.Invoking(() => Spec(Roles.Admin, null, R1).AssertCanViewStore(R2, S2))
            .Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void View_employee_own_store_ok_other_denied()
    {
        FluentActions.Invoking(() => Spec(Roles.StoreEmployee, S1).AssertCanViewStore(R1, S1)).Should().NotThrow();
        FluentActions.Invoking(() => Spec(Roles.StoreEmployee, S1).AssertCanViewStore(R2, S2))
            .Should().Throw<UnauthorizedAccessException>();
    }

    // ── AssertCanManageStore (employee/kiosk denied even for own store) ────────

    [Theory]
    [InlineData(Roles.StoreEmployee)]
    [InlineData(Roles.StoreKiosk)]
    public void Manage_denied_for_employee_and_kiosk(string role) =>
        FluentActions.Invoking(() => Spec(role, S1).AssertCanManageStore(R1, S1))
            .Should().Throw<UnauthorizedAccessException>();

    [Fact]
    public void Manage_store_manager_own_store_ok()
    {
        FluentActions.Invoking(() => Spec(Roles.StoreManager, S1).AssertCanManageStore(R1, S1)).Should().NotThrow();
        FluentActions.Invoking(() => Spec(Roles.StoreManager, S1).AssertCanManageStore(R2, S2))
            .Should().Throw<UnauthorizedAccessException>();
    }

    // ── AssertCanWriteScope ───────────────────────────────────────────────────

    [Fact]
    public void Write_system_scope_super_admin_only()
    {
        FluentActions.Invoking(() => Spec(Roles.SuperAdmin).AssertCanWriteScope("System", null)).Should().NotThrow();
        FluentActions.Invoking(() => Spec(Roles.Admin, null, R1).AssertCanWriteScope("System", null))
            .Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void Write_regional_scope_requires_region_membership()
    {
        FluentActions.Invoking(() => Spec(Roles.Admin, null, R1).AssertCanWriteScope("Regional", R1)).Should().NotThrow();
        FluentActions.Invoking(() => Spec(Roles.Supervisor, null, R1).AssertCanWriteScope("Regional", R1)).Should().NotThrow();
        FluentActions.Invoking(() => Spec(Roles.Admin, null, R1).AssertCanWriteScope("Regional", R2))
            .Should().Throw<UnauthorizedAccessException>();
    }
}
