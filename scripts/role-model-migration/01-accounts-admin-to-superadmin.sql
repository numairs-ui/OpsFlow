-- ─────────────────────────────────────────────────────────────────────────────
-- Role-model v2, step 2: promote existing full-access admins to super_admin.
-- Run in the Supabase SQL editor (edits auth.users metadata). MANUAL — review first.
--
-- Old model: "admin" = full system access.
-- New model: "super_admin" = full access; "admin" = region-scoped (assigned via the app).
-- Every PRE-EXISTING admin therefore becomes super_admin.
-- ─────────────────────────────────────────────────────────────────────────────

-- Preview what will change:
select id, email, raw_user_meta_data->>'tenant_id' as tenant, raw_user_meta_data->>'role' as role
from auth.users
where raw_user_meta_data->>'role' = 'admin';

-- Apply (optionally scope to one tenant by uncommenting the tenant_id filter):
update auth.users
set raw_user_meta_data = jsonb_set(raw_user_meta_data, '{role}', '"super_admin"', true)
where raw_user_meta_data->>'role' = 'admin'
  -- and raw_user_meta_data->>'tenant_id' = 'bajco-dev'
;

-- Mirror the change into the tenant DB's UserProfiles (run against each tenant DB):
--   update "UserProfiles" set "Role" = 'super_admin' where "Role" = 'admin' and "RegionIdsCsv" is null and "RegionId" is null;
-- Note: only profiles with NO region scope were old-style full admins; any profile that already
-- has a region was created as a new region-scoped admin and must be left as 'admin'.
