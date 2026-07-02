# Six-role, multi-region authorization model

OpsFlow uses six roles — **super_admin** (global), **admin** (an assigned *set* of regions), **supervisor** (one region), and **store_manager / store_employee / store_kiosk** (one store). We split full network access (super_admin) from region-scoped administration (admin) so regional managers can administer their own regions without holding network-wide power. Region scope is modelled as a **set**: emitted as one repeated `regionId` JWT claim per region and persisted as `RegionIdsCsv`. **store_kiosk** is a shared, always-logged-in store account with no personal identity — walk-up staff claim tasks by typing their name (`CompletedByVolunteerName`).

## Considered options

- **A single `admin` role** (the original model) — rejected: no way to delegate administration of specific regions without granting everything.
- **A single `region_id` per user** — rejected: an Admin must span several regions, so scope had to become a set, not a scalar.
- **Kiosk as just a view any field user opens** (the original behaviour) — rejected: the PRD calls for a shared station account distinct from individual logins.

## Consequences

- Existing `admin` accounts were migrated to `super_admin`, and form approval-step roles `admin` → `super_admin`, at cutover (see `scripts/role-model-migration/`).
- Every scope check must read a region *set*, not a single id (see ADR-0002).
