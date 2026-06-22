#!/usr/bin/env python3
"""
Enrich OpsFlow demo with a busy, realistic dataset.

Adds (idempotent — safe to re-run):
  • 2 extra regions (South, East) + 4 extra stores
  • 10 extra Supabase users (manager + employee per new store)
  • 5 checklists (3 for store1, 2 for store2) with task templates
  • 40+ task instances for TODAY spread across store1 & store2 with varied statuses
  • 3 extra form templates (Health & Safety, Staff Handover, Waste Log)
  • 8 form submissions with varied approval states
"""

import json, uuid, requests, psycopg2
from datetime import date, datetime, timedelta, timezone
from pathlib import Path

ROOT = Path(__file__).parent.parent
env: dict[str, str] = {}
for line in (ROOT / ".env").read_text().splitlines():
    line = line.strip()
    if line and not line.startswith("#") and "=" in line:
        k, v = line.split("=", 1)
        env[k.strip()] = v.strip()

TENANT_ID    = "bajco-dev"
PASSWORD     = "Demo1234!"
SUPABASE_URL = env["SUPABASE_URL"]
SERVICE_KEY  = env["SUPABASE_SERVICE_ROLE_KEY"]

cs    = env["TENANT_DB_CONNECTION_STRING"]
parts = {k.strip(): v.strip() for k, v in (p.split("=", 1) for p in cs.split(";") if "=" in p)}
conn  = psycopg2.connect(
    host=parts["Host"], dbname=parts["Database"],
    user=parts["Username"], password=parts["Password"],
    port=int(parts["Port"]), sslmode="require",
)
conn.autocommit = False
cur = conn.cursor()

def uid() -> str:  return str(uuid.uuid4())
def now() -> str:  return datetime.now(timezone.utc).isoformat()
def utc(h: int, m: int = 0) -> str:
    t = date.today()
    return datetime(t.year, t.month, t.day, h, m, 0, tzinfo=timezone.utc).isoformat()

admin_hdrs = {
    "apikey": SERVICE_KEY,
    "Authorization": f"Bearer {SERVICE_KEY}",
    "Content-Type": "application/json",
}

def get_or_create_user(email: str, role: str, store_id=None, region_id=None) -> str:
    meta: dict = {"tenant_id": TENANT_ID, "role": role}
    if store_id:  meta["store_id"]  = str(store_id)
    if region_id: meta["region_id"] = str(region_id)
    r = requests.post(
        f"{SUPABASE_URL}/auth/v1/admin/users", headers=admin_hdrs,
        json={"email": email, "password": PASSWORD, "email_confirm": True, "user_metadata": meta},
    )
    if r.status_code in (200, 201):
        return r.json()["id"]
    r2 = requests.get(f"{SUPABASE_URL}/auth/v1/admin/users?per_page=1000", headers=admin_hdrs)
    users = r2.json().get("users", [])
    match = next((u for u in users if u["email"] == email), None)
    if match:
        requests.put(f"{SUPABASE_URL}/auth/v1/admin/users/{match['id']}", headers=admin_hdrs,
                     json={"user_metadata": meta})
        return match["id"]
    raise RuntimeError(f"Cannot create/find {email}: {r.text}")

# ── Fetch existing org fixtures we need to attach to ──────────────────────────
cur.execute('SELECT "Id" FROM "Regions" WHERE "TenantId"=%s AND "Name"=\'North Region\'', (TENANT_ID,))
row = cur.fetchone()
north_id = row[0] if row else uid()

cur.execute('SELECT "Id" FROM "Stores" WHERE "TenantId"=%s AND "Name"=\'Downtown Flagship\'', (TENANT_ID,))
row = cur.fetchone()
store1_id = row[0] if row else uid()

cur.execute('SELECT "Id" FROM "Stores" WHERE "TenantId"=%s AND "Name"=\'Westside Branch\'', (TENANT_ID,))
row = cur.fetchone()
store2_id = row[0] if row else uid()

# Look up the admin user profile
cur.execute('SELECT "UserId" FROM "UserProfiles" WHERE "Role"=\'admin\' LIMIT 1')
row = cur.fetchone()
admin_uid = row[0] if row else uid()

cur.execute('SELECT "UserId" FROM "UserProfiles" WHERE "Role"=\'store_manager\' AND "StoreId"=%s LIMIT 1', (store1_id,))
row = cur.fetchone()
mgr1_uid = row[0] if row else uid()

cur.execute('SELECT "UserId" FROM "UserProfiles" WHERE "Role"=\'store_employee\' AND "StoreId"=%s LIMIT 1', (store1_id,))
row = cur.fetchone()
emp1_uid = row[0] if row else uid()

print(f"Attached to: store1={store1_id[:8]}… store2={store2_id[:8]}… admin={admin_uid[:8]}…")

# ── Extra regions & stores ────────────────────────────────────────────────────
print("\n── Extra regions & stores")

south_id = uid()
east_id  = uid()
cur.execute("""INSERT INTO "Regions" ("Id","TenantId","Name","IsActive","CreatedAt")
               VALUES (%s,%s,%s,true,%s) ON CONFLICT DO NOTHING""",
            (south_id, TENANT_ID, "South Region", now()))
cur.execute("""INSERT INTO "Regions" ("Id","TenantId","Name","IsActive","CreatedAt")
               VALUES (%s,%s,%s,true,%s) ON CONFLICT DO NOTHING""",
            (east_id, TENANT_ID, "East Region", now()))

extra_stores = [
    (uid(), south_id, "Southgate Mall",    "Unit 14, Southgate Shopping Centre, London N14 6PL"),
    (uid(), south_id, "Brixton Road",      "201 Brixton Road, London SW9 7DJ"),
    (uid(), east_id,  "Canary Wharf",      "Level 2, One Canada Square, London E14 5AA"),
    (uid(), east_id,  "Stratford City",    "The Arcade, Westfield Stratford, London E20 1EJ"),
]
for s_id, r_id, name, addr in extra_stores:
    cur.execute("""INSERT INTO "Stores" ("Id","TenantId","RegionId","Name","Address","IsActive","CreatedAt")
                   VALUES (%s,%s,%s,%s,%s,true,%s) ON CONFLICT DO NOTHING""",
                (s_id, TENANT_ID, r_id, name, addr, now()))
    cur.execute("""INSERT INTO "StoreSettings"
                   ("StoreId","TenantId","TimezoneId","OverdueGraceMinutes","DoughNeedTargets")
                   VALUES (%s,%s,'Europe/London',15,'[]'::jsonb) ON CONFLICT DO NOTHING""",
                (s_id, TENANT_ID))

# Add settings for store2 too
cur.execute("""INSERT INTO "StoreSettings"
               ("StoreId","TenantId","TimezoneId","OverdueGraceMinutes","DoughNeedTargets")
               VALUES (%s,%s,'Europe/London',15,'[]'::jsonb) ON CONFLICT DO NOTHING""",
            (store2_id, TENANT_ID))

print(f"  6 stores total across 3 regions")

# ── Extra Supabase users ──────────────────────────────────────────────────────
print("\n── Extra Supabase users")
store_names_for_email = {
    extra_stores[0][0]: "southgate",
    extra_stores[1][0]: "brixton",
    extra_stores[2][0]: "canary",
    extra_stores[3][0]: "stratford",
}
sup2_uid = get_or_create_user("supervisor2@bajco.net", "supervisor", region_id=south_id)
sup3_uid = get_or_create_user("supervisor3@bajco.net", "supervisor", region_id=east_id)

extra_users: list[tuple] = []   # (uid, email, name, role, store_id, region_id)
extra_users += [
    (sup2_uid, "supervisor2@bajco.net", "Sarah South",  "supervisor", None, south_id),
    (sup3_uid, "supervisor3@bajco.net", "Evan East",    "supervisor", None, east_id),
]
mgr2_uid = get_or_create_user("manager2@bajco.net",  "store_manager", store_id=store2_id, region_id=north_id)
extra_users.append((mgr2_uid, "manager2@bajco.net", "Marcus West", "store_manager", store2_id, north_id))

for s_id, r_id, sname, _ in extra_stores:
    slug = store_names_for_email[s_id]
    m_uid = get_or_create_user(f"manager.{slug}@bajco.net", "store_manager", store_id=s_id, region_id=r_id)
    e_uid = get_or_create_user(f"employee.{slug}@bajco.net", "store_employee", store_id=s_id)
    region_for_store = south_id if r_id == south_id else east_id
    extra_users += [
        (m_uid, f"manager.{slug}@bajco.net",  f"Manager {sname.split()[0]}", "store_manager",  s_id, region_for_store),
        (e_uid, f"employee.{slug}@bajco.net", f"Staff {sname.split()[0]}",   "store_employee", s_id, None),
    ]

for u_id, email, name, role, s_id, r_id in extra_users:
    cur.execute("""
        INSERT INTO "UserProfiles"
          ("UserId","Email","DisplayName","Role","StoreId","RegionId","IsActive","MustChangePassword","CreatedAt")
        VALUES (%s,%s,%s,%s,%s,%s,true,false,%s)
        ON CONFLICT ("UserId") DO UPDATE SET "Email"=EXCLUDED."Email","DisplayName"=EXCLUDED."DisplayName",
          "Role"=EXCLUDED."Role","StoreId"=EXCLUDED."StoreId","RegionId"=EXCLUDED."RegionId"
    """, (u_id, email, name, role, s_id, r_id, now()))
print(f"  {len(extra_users) + 4} total users")

# ── Helper: bulk-insert a checklist with tasks ────────────────────────────────
def make_checklist(name: str, scope: str, store_id: str, tasks: list[tuple]) -> tuple[str, list]:
    """Create a Checklist + TaskTemplates + ChecklistTemplateItems.
    tasks = list of (name, category, description).
    Returns (checklist_id, [template_id, …]).
    Handles the UNIQUE (TenantId, Name, Scope) constraint safely.
    """
    # Try to find existing checklist by the unique key
    cur.execute("""SELECT "Id" FROM "Checklists"
                   WHERE "TenantId"=%s AND "Name"=%s AND "Scope"=%s""",
                (TENANT_ID, name, scope))
    row = cur.fetchone()
    if row:
        c_id = row[0]
        # Only add templates/items if they're for this store; otherwise bail early.
        cur.execute('SELECT "StoreId" FROM "Checklists" WHERE "Id"=%s', (c_id,))
        existing_store = cur.fetchone()[0]
        if str(existing_store) != str(store_id):
            # Different store — caller must pass a unique name; skip silently.
            return c_id, []
    else:
        c_id = uid()
        cur.execute("""
            INSERT INTO "Checklists"
              ("Id","TenantId","Name","Scope","StoreId","IsActive","CreatedByUserId","CreatedAt")
            VALUES (%s,%s,%s,%s,%s,true,%s,%s)
        """, (c_id, TENANT_ID, name, scope, store_id, admin_uid, now()))

    tt_ids = []
    for order, (tname, cat, desc) in enumerate(tasks, 1):
        t_id = uid()
        tt_ids.append(t_id)
        # Task templates also have unique (TenantId, Name) — make name unique per store
        full_tname = tname
        cur.execute("""SELECT "Id" FROM "TaskTemplates"
                       WHERE "TenantId"=%s AND "Name"=%s AND "StoreId"=%s""",
                    (TENANT_ID, tname, store_id))
        if not cur.fetchone():
            cur.execute("""
                INSERT INTO "TaskTemplates"
                  ("Id","TenantId","Name","Description","Category","Scope","StoreId","Fields","IsActive","CreatedByUserId","CreatedAt")
                VALUES (%s,%s,%s,%s,%s,'Store',%s,'[]'::jsonb,true,%s,%s)
                ON CONFLICT DO NOTHING
            """, (t_id, TENANT_ID, full_tname, desc, cat, store_id, admin_uid, now()))
        cur.execute("""
            INSERT INTO "ChecklistTemplateItems" ("ChecklistId","TemplateId","Order")
            VALUES (%s,%s,%s) ON CONFLICT DO NOTHING
        """, (c_id, t_id, order))
    return c_id, tt_ids

def make_recurring(name: str, checklist_id: str, store_id: str, cron: str) -> str:
    today = date.today()
    starts = datetime(today.year, today.month, today.day, 0, 0, 0, tzinfo=timezone.utc).isoformat()
    ra_id = uid()
    cur.execute("""
        INSERT INTO "RecurringAssignments"
          ("Id","TenantId","Name","ChecklistId","StoreId","CronExpression","StartsAt","IsPaused","CreatedByUserId","CreatedAt")
        VALUES (%s,%s,%s,%s,%s,%s,%s,false,%s,%s) ON CONFLICT DO NOTHING
    """, (ra_id, TENANT_ID, name, checklist_id, store_id, cron, starts, admin_uid, now()))
    return ra_id

def make_instances(ra_id: str, cl_id: str, store_id: str, due_hour: int,
                   statuses: list[str], assigned_uid: str, interval_mins: int = 15) -> int:
    due_base = datetime(*date.today().timetuple()[:3], due_hour, 0, 0, tzinfo=timezone.utc)
    count = 0
    for idx, status in enumerate(statuses):
        due_at      = (due_base + timedelta(minutes=idx * interval_mins)).isoformat()
        completed_at = None
        completed_by = None
        if status == "Completed":
            completed_at = (due_base + timedelta(minutes=idx * interval_mins + 8)).isoformat()
            completed_by = assigned_uid
        cur.execute("""
            INSERT INTO "TaskInstances"
              ("Id","TenantId","RecurringAssignmentId","ChecklistId","StoreId","DueAt",
               "Status","AssignedToUserId","CompletedByUserId","CompletedAt","CreatedByUserId","CreatedAt")
            VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s) ON CONFLICT DO NOTHING
        """, (uid(), TENANT_ID, ra_id, cl_id, store_id, due_at,
              status, assigned_uid, completed_by, completed_at, "system", now()))
        count += 1
    return count

# ══════════════════════════════════════════════════════════════════════════════
# STORE 1  — Downtown Flagship  (the employee's store — needs to be packed)
# ══════════════════════════════════════════════════════════════════════════════
print("\n── Store 1 — Downtown Flagship extra checklists")

# ── Midday Safety & Compliance (due 12:00) ───────────────────────────────────
mid_cl, _ = make_checklist("Midday Safety & Compliance", "Store", store1_id, [
    ("Spill & Hazard Walk",        "Safety",     "Walk all public-facing floor areas; clear spills and trip hazards."),
    ("Fire Exit Clear",            "Compliance", "Confirm all fire exits are unobstructed and signage is visible."),
    ("Customer Facility Check",    "Cleaning",   "Inspect and restock toilets; log any maintenance issues."),
    ("First Aid Kit Inventory",    "Compliance", "Verify first aid kit contents are complete and in-date."),
    ("Ambient Temperature Log",    "Compliance", "Record ambient store temperature to compliance sheet."),
])
mid_ra = make_recurring("Daily Midday Safety", mid_cl, store1_id, "0 0 12 * * ?")
n1 = make_instances(mid_ra, mid_cl, store1_id, due_hour=12,
                    statuses=["Completed","Completed","Completed","Pending","Pending"],
                    assigned_uid=emp1_uid, interval_mins=12)

# ── Afternoon Stock Rotation (due 14:30) ─────────────────────────────────────
aft_cl, _ = make_checklist("Afternoon Stock Rotation", "Store", store1_id, [
    ("Rotate Chilled Products",    "Retail",     "Pull forward older stock in all chilled fixtures; discard expired items."),
    ("Update Price Tags",          "Retail",     "Replace any incorrect or missing shelf-edge price labels."),
    ("Stockroom Tidy",             "Retail",     "Consolidate stockroom: break down cardboard and recycle."),
    ("Replenish Coffee Station",   "Hospitality","Restock coffee beans, cups, lids, stirrers, and sweeteners."),
])
aft_ra = make_recurring("Daily Stock Rotation", aft_cl, store1_id, "0 30 14 * * ?")
n2 = make_instances(aft_ra, aft_cl, store1_id, due_hour=14,
                    statuses=["Completed","Pending","Pending","Pending"],
                    assigned_uid=emp1_uid, interval_mins=20)

# ── Pre-Close Manager Sign-Off (due 20:30) ───────────────────────────────────
pre_close_cl, _ = make_checklist("Pre-Close Manager Sign-Off", "Store", store1_id, [
    ("Void & Refund Review",       "Finance",    "Review all POS voids and refunds processed today with receipts."),
    ("Safe Count & Drop",          "Finance",    "Count takings; complete cash drop form and seal bag."),
    ("Staff Departure Log",        "HR",         "Confirm all staff have clocked out and log any overtime."),
    ("CCTV System Check",          "Security",   "Verify CCTV is recording; flag any camera faults to facilities."),
    ("Waste Record",               "Compliance", "Complete end-of-day waste log for regulatory reporting."),
    ("Manager Handover Notes",     "Operations", "Write brief shift summary; flag open issues for opener."),
])
pre_ra = make_recurring("Pre-Close Sign-Off", pre_close_cl, store1_id, "0 30 20 * * ?")
n3 = make_instances(pre_ra, pre_close_cl, store1_id, due_hour=20,
                    statuses=["Pending","Pending","Pending","Pending","Pending","Pending"],
                    assigned_uid=mgr1_uid, interval_mins=10)

print(f"  +{n1+n2+n3} task instances for store1 ({n1} midday / {n2} afternoon / {n3} pre-close)")

# ══════════════════════════════════════════════════════════════════════════════
# STORE 2  — Westside Branch
# ══════════════════════════════════════════════════════════════════════════════
print("\n── Store 2 — Westside Branch checklists")

w_open_cl, _ = make_checklist("Westside: Morning Opening", "Store", store2_id, [
    ("Westside Unlock & Alarm Off",     "Security",   "Disable alarm and unlock front entrance."),
    ("Westside Lights & Equipment On",  "Facilities", "Power on lighting, refrigeration units, and POS terminals."),
    ("Westside Cold Chain Temp Log",    "Compliance", "Record all chilled and frozen unit temperatures."),
    ("Westside Counter Sanitise",       "Cleaning",   "Disinfect all service counters and prep surfaces."),
    ("Westside Till Opening Count",     "Finance",    "Count and record opening float in register log."),
    ("Westside Specials Board",         "Marketing",  "Update chalkboard with today's specials and promotions."),
])
w_open_ra = make_recurring("Westside Daily Morning Opening", w_open_cl, store2_id, "0 0 7 * * ?")
n4 = make_instances(w_open_ra, w_open_cl, store2_id, due_hour=7,
                    statuses=["Completed","Completed","Completed","Completed","InProgress","Pending"],
                    assigned_uid=mgr2_uid, interval_mins=15)

w_close_cl, _ = make_checklist("Westside: Evening Closing", "Store", store2_id, [
    ("Westside Till Balance",           "Finance",    "Run POS Z-report; verify balance matches cash in till."),
    ("Westside Deep Clean",             "Cleaning",   "Degrease surfaces, clean equipment, and sanitise prep zones."),
    ("Westside Waste & Recycling",      "Compliance", "Bag all waste; separate recyclables; complete waste log."),
    ("Westside Fridge Final Log",       "Compliance", "Record final temperature readings before lock-up."),
    ("Westside Lock & Alarm",           "Security",   "Secure all doors, windows, and storage areas; set alarm."),
])
w_close_ra = make_recurring("Westside Daily Evening Closing", w_close_cl, store2_id, "0 0 18 * * ?")
n5 = make_instances(w_close_ra, w_close_cl, store2_id, due_hour=18,
                    statuses=["Pending","Pending","Pending","Pending","Pending"],
                    assigned_uid=mgr2_uid, interval_mins=15)

print(f"  +{n4+n5} task instances for store2 ({n4} morning / {n5} closing)")

# ══════════════════════════════════════════════════════════════════════════════
# Extra stores — lightweight data so dashboard looks active
# ══════════════════════════════════════════════════════════════════════════════
print("\n── Extra stores — lightweight task data")
total_extra = 0

# Get the extra store users we just created
for s_id, r_id, sname, _ in extra_stores:
    slug = store_names_for_email[s_id]
    cur.execute('SELECT "UserId" FROM "UserProfiles" WHERE "Email"=%s', (f"manager.{slug}@bajco.net",))
    row = cur.fetchone()
    if not row:
        continue
    s_mgr_uid = row[0]

    cur.execute('SELECT "UserId" FROM "UserProfiles" WHERE "Email"=%s', (f"employee.{slug}@bajco.net",))
    row = cur.fetchone()
    s_emp_uid = row[0] if row else s_mgr_uid

    # Two checklists per store
    prefix = sname.split()[0]   # e.g. "Southgate", "Brixton", "Canary", "Stratford"
    op_cl, _ = make_checklist(f"{prefix}: Morning Opening", "Store", s_id, [
        (f"{prefix} Unlock & Alarm",    "Security",   "Open up the store."),
        (f"{prefix} Equipment On",      "Facilities", "Power on all equipment."),
        (f"{prefix} Temp Check",        "Compliance", "Log refrigeration temperatures."),
        (f"{prefix} Till Opening",      "Finance",    "Count and record opening float."),
        (f"{prefix} Floor Walk",        "Operations", "Check for hazards and presentation."),
    ])
    op_ra = make_recurring(f"{prefix} Daily Morning Opening", op_cl, s_id, "0 0 8 * * ?")

    cl_cl, _ = make_checklist(f"{prefix}: Evening Closing", "Store", s_id, [
        (f"{prefix} Till Reconciliation", "Finance",  "Balance register against POS report."),
        (f"{prefix} Floors Clean",        "Cleaning", "Sweep, mop, and sanitise all areas."),
        (f"{prefix} Stock Secured",       "Security", "Lock all stockroom and display cases."),
        (f"{prefix} Alarm & Lock-up",     "Security", "Set alarm and lock all exits."),
    ])
    cl_ra = make_recurring(f"{prefix} Daily Evening Closing", cl_cl, s_id, "0 0 17 * * ?")

    seed_val = sum(ord(c) for c in sname)
    completed_count = (seed_val % 3) + 2
    open_statuses = (
        ["Completed"] * completed_count +
        ["InProgress"] +
        ["Pending"] * (5 - completed_count)
    )[:5]
    n_op = make_instances(op_ra, op_cl, s_id, due_hour=8,
                          statuses=open_statuses, assigned_uid=s_emp_uid)
    n_cl = make_instances(cl_ra, cl_cl, s_id, due_hour=17,
                          statuses=["Pending","Pending","Pending","Pending"],
                          assigned_uid=s_mgr_uid)
    total_extra += n_op + n_cl
    print(f"  {sname}: {n_op} morning + {n_cl} closing")

print(f"  +{total_extra} instances across extra stores")

# ══════════════════════════════════════════════════════════════════════════════
# Form Templates
# ══════════════════════════════════════════════════════════════════════════════
print("\n── Extra form templates")

form_templates = [
    (
        "Daily Health & Safety Inspection",
        "H&S",
        store1_id,
        [
            {"id":"f1","label":"Inspected by","type":"Text","required":True},
            {"id":"f2","label":"All fire exits unobstructed?","type":"Boolean","required":True},
            {"id":"f3","label":"No slip/trip hazards observed?","type":"Boolean","required":True},
            {"id":"f4","label":"First aid kit complete?","type":"Boolean","required":True},
            {"id":"f5","label":"Any faults to report?","type":"Text","required":False},
        ],
    ),
    (
        "Staff Handover Report",
        "HR",
        store1_id,
        [
            {"id":"f1","label":"Outgoing manager","type":"Text","required":True},
            {"id":"f2","label":"Incoming manager","type":"Text","required":True},
            {"id":"f3","label":"Outstanding tasks","type":"Text","required":False},
            {"id":"f4","label":"Incidents or escalations","type":"Text","required":False},
            {"id":"f5","label":"Handover accepted?","type":"Boolean","required":True},
        ],
    ),
    (
        "Waste & Spoilage Log",
        "Compliance",
        store1_id,
        [
            {"id":"f1","label":"Date","type":"Text","required":True},
            {"id":"f2","label":"Total waste (kg)","type":"Numeric","required":True},
            {"id":"f3","label":"Chilled waste (kg)","type":"Numeric","required":True},
            {"id":"f4","label":"Reason","type":"Text","required":True},
            {"id":"f5","label":"Manager sign-off","type":"Text","required":True},
        ],
    ),
]

ft_ids = []
steps = json.dumps([{"role": "supervisor", "order": 1}])
for ft_name, scope_cat, s_id, fields in form_templates:
    ft_id = uid()
    ft_ids.append(ft_id)
    cur.execute("""
        INSERT INTO "FormTemplates"
          ("Id","TenantId","Name","Description","Scope","PropagationType",
           "ApprovalSteps","Fields","StoreId","IsActive","CreatedByUserId","CreatedAt")
        VALUES (%s,%s,%s,%s,'Store','Sequential',%s::jsonb,%s::jsonb,%s,true,%s,%s)
        ON CONFLICT DO NOTHING
    """, (ft_id, TENANT_ID, ft_name,
          f"{ft_name} for operational compliance tracking.",
          steps, json.dumps(fields), s_id, admin_uid, now()))

print(f"  +{len(form_templates)} form templates")

# ══════════════════════════════════════════════════════════════════════════════
# Form Submissions  (cash count form from the original seed)
# ══════════════════════════════════════════════════════════════════════════════
print("\n── Form submissions")
cur.execute("""SELECT "Id" FROM "FormTemplates"
               WHERE "TenantId"=%s AND "Name"='Cash Count Report'
               LIMIT 1""", (TENANT_ID,))
row = cur.fetchone()
cash_ft_id = row[0] if row else None

# Grab supervisor uid for approvals
cur.execute('SELECT "UserId" FROM "UserProfiles" WHERE "Role"=\'supervisor\' LIMIT 1')
row = cur.fetchone()
sup_uid = row[0] if row else admin_uid

sub_count = 0
if cash_ft_id:
    # Check FormSubmissions table schema
    cur.execute("""
        SELECT column_name FROM information_schema.columns
        WHERE table_name='FormSubmissions' ORDER BY ordinal_position
    """)
    fs_cols = [r[0] for r in cur.fetchall()]
    print(f"  FormSubmissions columns: {fs_cols}")

    if fs_cols:
        sample_data = [
            # (submitted_by, status, days_ago, cash_total, variance)
            (emp1_uid, "Approved",   3, 1250.00,  0.00),
            (emp1_uid, "Approved",   2, 1318.50, -0.50),
            (emp1_uid, "Pending",    1,  987.00,  2.00),
            (emp1_uid, "Draft",      0,  None,    None),
            (mgr1_uid, "Approved",   4, 2100.75,  0.00),
            (mgr1_uid, "Rejected",   5,  845.00,  5.50),
            (mgr2_uid, "Pending",    1, 1100.00,  0.00),
            (mgr2_uid, "Approved",   6,  990.25, -1.25),
        ]
        for (sub_by, status, days_ago, cash, variance) in sample_data:
            submitted_at = (datetime.now(timezone.utc) - timedelta(days=days_ago)).isoformat()
            fields_data = {}
            if cash is not None:
                fields_data = {
                    "f1": str(cash), "f2": str(variance or 0),
                    "f3": str(abs(variance or 0) < 1.0).lower(), "f4": "",
                }
            reviewed_by  = sup_uid if status in ("Approved","Rejected") else None
            reviewed_at  = submitted_at if reviewed_by else None

            # Build insert based on available columns
            base_cols = ["Id","TenantId","FormTemplateId","StoreId","SubmittedByUserId",
                         "Status","FieldValues","CreatedAt"]
            base_vals = [uid(), TENANT_ID, cash_ft_id, store1_id, sub_by,
                         status, json.dumps(fields_data), submitted_at]

            # Optional columns that might not exist
            extra_pairs = [
                ("ReviewedByUserId", reviewed_by),
                ("ReviewedAt",       reviewed_at),
                ("SubmittedAt",      submitted_at if status != "Draft" else None),
            ]
            for col, val in extra_pairs:
                if col in fs_cols:
                    base_cols.append(col)
                    base_vals.append(val)

            placeholders = ",".join(["%s"] * len(base_cols))
            col_str      = ",".join(f'"{c}"' for c in base_cols)
            cur.execute(f"""
                INSERT INTO "FormSubmissions" ({col_str})
                VALUES ({placeholders}) ON CONFLICT DO NOTHING
            """, base_vals)
            sub_count += 1

print(f"  +{sub_count} form submissions")

# ── Commit ─────────────────────────────────────────────────────────────────────
conn.commit()
conn.close()

print()
print("═" * 55)
print("  ENRICH COMPLETE")
print("═" * 55)
print()
print("  Regions:          North · South · East")
print("  Stores:           6 total")
print("  Users:            14 total  (password: Demo1234!)")
print()
print("  Log in as:")
print("  employee@bajco.net    → Field-PWA  (store1 tasks)")
print("  manager@bajco.net     → Field-PWA  (store1, sees FAB)")
print("  supervisor@bajco.net  → Dashboard  (north region)")
print("  admin@bajco.net       → Dashboard  (all stores)")
print()
print("  Field-PWA  → http://localhost:4201")
print("  Dashboard  → http://localhost:4200")
print("═" * 55)
