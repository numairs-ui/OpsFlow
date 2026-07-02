# OpsFlow

OpsFlow is a multi-tenant retail operations platform: regions contain stores, stores run recurring task checklists and submit forms, and work is reviewed up a role hierarchy. This file is the shared glossary — it fixes the language and carries no implementation detail.

## Language

### Roles

**Super Admin**:
Full, network-wide access across all regions, stores, users, and System-scope templates.
_Avoid_: admin (a narrower role here), owner, root.

**Admin**:
An administrator scoped to an assigned **set** of regions; manages stores, users, and templates only within those regions. Cannot create Super Admins or other Admins.
_Avoid_: regional admin, manager.

**Supervisor**:
Oversees a single region — monitors store progress and reviews/approves forms within that region.
_Avoid_: regional manager.

**Store Manager**:
Runs a single store's recurring tasks, deposits, roster, and forms.
_Avoid_: manager (ambiguous on its own).

**Store Employee**:
A field worker at a single store who claims, completes, and defers tasks and submits forms.
_Avoid_: staff, worker, user.

**Store Kiosk**:
A shared, always-logged-in station account at a single store; walk-up staff claim tasks by typing their name, with no individual login.
_Avoid_: shared account, terminal, device.

### Scope

**Scope** (of a template, checklist, or form template):
The breadth at which it applies — **System** (whole tenant), **Regional** (one region), or **Store** (one store).
_Avoid_: level, tier.

**Region scope**:
The set of regions a role may act within — all (Super Admin), several (Admin), or one (Supervisor).
_Avoid_: region access, territory.

**Store scope**:
The single store a role is bound to (Store Manager, Store Employee, Store Kiosk).
_Avoid_: store access.

### Structure

**Region**:
A grouping of stores; the unit of Supervisor and Admin scope.

**Store**:
A single physical location. The leaf the daily operational work hangs off.

**Checklist**:
An ordered set of task templates that becomes concrete tasks when scheduled.

**Recurring Assignment**:
A schedule that generates Task Instances for a store's checklist on a cadence.

**Task Instance**:
One concrete, dated unit of work at a store, claimed and completed by staff.

**Form Submission**:
A filled-in form template instance that moves through an approval workflow.

**Approval Step**:
One stage in a Form Submission's review, owned by a role and resolved in sequence or in parallel.

**Deposit Log**:
The immutable daily record of a store's cash bank deposit, used for financial compliance.
