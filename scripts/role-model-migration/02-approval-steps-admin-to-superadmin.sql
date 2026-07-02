-- ─────────────────────────────────────────────────────────────────────────────
-- Role-model v2, step 3: migrate form approval-step roles "admin" -> "super_admin".
-- Run against EACH tenant database. MANUAL — review the preview first.
--
-- Approval steps authored under the old model used "admin" to mean a full-access approver.
-- Re-point them at "super_admin" so the same people keep approving. New templates authored
-- after cutover may legitimately use "admin" (region-scoped) — but none exist yet at cutover.
-- ─────────────────────────────────────────────────────────────────────────────

-- Preview affected form templates:
select "Id", "Name", "ApprovalSteps"
from "FormTemplates"
where "ApprovalSteps" @> '[{"role":"admin"}]';

-- 1) Template definitions (ApprovalSteps is a jsonb array of {role, order}):
update "FormTemplates"
set "ApprovalSteps" = (
  select jsonb_agg(
    case when step->>'role' = 'admin'
         then jsonb_set(step, '{role}', '"super_admin"')
         else step end)
  from jsonb_array_elements("ApprovalSteps") as step
)
where "ApprovalSteps" @> '[{"role":"admin"}]';

-- 2) In-flight submission steps (Role is a text column):
update "FormSubmissionApprovalSteps"
set "Role" = 'super_admin'
where "Role" = 'admin';
