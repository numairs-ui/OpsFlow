# Role-model v2 cutover migration

Adds `super_admin` (full access) + a region-scoped `admin`, and `store_kiosk`. Existing
full-access `admin` accounts and the approval steps that reference them must be migrated
so behaviour is preserved.

**Run these once, in order, at deploy time. Nothing here runs automatically — every step is manual.**
Take a backup of both databases first.

## 1. Schema migration (tenant database)

Adds `RefreshTokens.RegionIdsCsv` (renamed from `RegionId`) and `UserProfiles.RegionIdsCsv`.
Uses the existing migration runner:

```bash
# from repo root, with MASTER_DB_CONNECTION_STRING set (see .env)
dotnet run --project backend/OpsFlow.Migrations -- --migrate-all
# or a single tenant:
dotnet run --project backend/OpsFlow.Migrations -- --migrate-tenant bajco-dev
```

Migration: `20260627183136_AddMultiRegionScope`.

## 2. Promote existing admins to super_admin (Supabase auth)

Old `admin` = full access. The new model reserves full access for `super_admin`, so every
existing `admin` account must become `super_admin`. Run `01-accounts-admin-to-superadmin.sql`
in the Supabase SQL editor (it edits `auth.users` metadata).

After this, the new region-scoped `admin` role is only ever assigned through the Users admin
screen (which writes `region_ids` to user metadata).

## 3. Migrate approval-step roles (tenant database)

Form templates and in-flight submissions store approver roles as strings. Steps that say
`admin` were authored under the old model (full access) and must become `super_admin` to keep
the same approvers. Run `02-approval-steps-admin-to-superadmin.sql` against each tenant DB.

## Verify

- Log in as a migrated account → lands on `/admin`, sees the full nav (System Templates,
  Regions, Tenant Settings).
- Create a new `admin` via Users, assign 2+ regions, log in → `/admin` with org-wide items
  hidden; can only see/act within those regions.
- Create a `store_kiosk` account for a store → lands on `/kiosk`, can claim by name, cannot
  reach `/tasks` or another store's board.
