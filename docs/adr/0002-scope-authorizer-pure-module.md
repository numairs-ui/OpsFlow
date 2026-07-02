# ScopeAuthorizer: a pure scope module with key-selector filters

Authorization scope lives in one pure module in `OpsFlow.Domain/Authorization`: a `Caller` value object (`{ Role, StoreId, RegionIds }`) becomes a `ScopeSpec`, which exposes `IQueryable` extensions for list visibility and assertion methods for read/write access. The visibility extensions take **key selectors** (`t => t.StoreId`, `t => t.Store!.RegionId`, `t => t.Scope`) so they stay EF-translatable and entity-agnostic. The module has **no interface, no DI, and no database access** — handlers pass already-loaded store/region data into the assertions.

## Considered options

- **Marker interfaces on entities** (`IStoreOwned`, `IScoped`) with a generic `Where` — rejected: the region for store-rooted entities lives on the `.Store` navigation, which EF Core cannot translate through an interface property. The selector leaks back out, defeating the encapsulation.
- **One visibility method per entity** (`VisibleTasks`, `VisibleTemplates`, …) — rejected: the module grows a method per entity and drifts shallow/wide as the schema expands.
- **An `IScopeAuthorizer` interface** — rejected: there is exactly one implementation and tests exercise it directly against in-memory queryables. One adapter is a hypothetical seam, not a real one.

## Consequences

- ~24 handlers call the module instead of re-deriving role/region/store scope inline; the rules are unit-tested once.
- Two read shapes remain distinct: `AssertCanViewStore` (any store-scoped role for its own store) vs `AssertCanManageStore` (excludes store_employee/store_kiosk) — they are separate methods, not a flag.
