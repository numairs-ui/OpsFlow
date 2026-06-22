#!/usr/bin/env python3
"""
Seed OpsFlow with a full demo dataset.
Creates: region, 2 stores, 4 users, checklists, task templates, recurring assignments,
         task instances for today (mixed statuses), and a form template.
"""
import json, uuid, requests, psycopg2
from datetime import date, datetime, timedelta, timezone
from pathlib import Path

ROOT = Path(__file__).parent.parent
env = {}
for line in (ROOT / ".env").read_text().splitlines():
    line = line.strip()
    if line and not line.startswith("#") and "=" in line:
        k, v = line.split("=", 1)
        env[k.strip()] = v.strip()

TENANT_ID    = "bajco-dev"
PASSWORD     = "Demo1234!"
SUPABASE_URL = env["SUPABASE_URL"]
SERVICE_KEY  = env["SUPABASE_SERVICE_ROLE_KEY"]

cs = env["TENANT_DB_CONNECTION_STRING"]
parts = {k.strip(): v.strip() for k, v in (p.split("=", 1) for p in cs.split(";") if "=" in p)}
conn = psycopg2.connect(host=parts["Host"], dbname=parts["Database"], user=parts["Username"],
    password=parts["Password"], port=int(parts["Port"]), sslmode="require")
conn.autocommit = False
cur = conn.cursor()

def uid():  return str(uuid.uuid4())
def now():  return datetime.now(timezone.utc).isoformat()

admin_hdrs = {"apikey": SERVICE_KEY, "Authorization": f"Bearer {SERVICE_KEY}", "Content-Type": "application/json"}

def create_or_get_supabase_user(email, role, store_id=None, region_id=None):
    meta = {"tenant_id": TENANT_ID, "role": role}
    if store_id:  meta["store_id"]  = str(store_id)
    if region_id: meta["region_id"] = str(region_id)
    r = requests.post(f"{SUPABASE_URL}/auth/v1/admin/users", headers=admin_hdrs,
        json={"email": email, "password": PASSWORD, "email_confirm": True, "user_metadata": meta})
    if r.status_code in (200, 201):
        return r.json()["id"]
    # duplicate — list users and find by email
    r2 = requests.get(f"{SUPABASE_URL}/auth/v1/admin/users?per_page=1000", headers=admin_hdrs)
    users = r2.json().get("users", [])
    match = next((u for u in users if u["email"] == email), None)
    if match:
        # update metadata so store/region IDs are current
        requests.put(f"{SUPABASE_URL}/auth/v1/admin/users/{match['id']}", headers=admin_hdrs,
            json={"user_metadata": meta})
        return match["id"]
    raise RuntimeError(f"Cannot create or find user {email}: {r.text}")

# ── Org structure ──────────────────────────────────────────────────────────────
print("── Org structure")
region_id = uid()
cur.execute("""INSERT INTO "Regions" ("Id","TenantId","Name","IsActive","CreatedAt")
               VALUES (%s,%s,%s,true,%s) ON CONFLICT DO NOTHING""",
            (region_id, TENANT_ID, "North Region", now()))

store1_id, store2_id = uid(), uid()
cur.execute("""INSERT INTO "Stores" ("Id","TenantId","RegionId","Name","Address","IsActive","CreatedAt")
               VALUES (%s,%s,%s,%s,%s,true,%s) ON CONFLICT DO NOTHING""",
            (store1_id, TENANT_ID, region_id, "Downtown Flagship", "12 High Street, London EC1A 1AA", now()))
cur.execute("""INSERT INTO "Stores" ("Id","TenantId","RegionId","Name","Address","IsActive","CreatedAt")
               VALUES (%s,%s,%s,%s,%s,true,%s) ON CONFLICT DO NOTHING""",
            (store2_id, TENANT_ID, region_id, "Westside Branch", "88 Western Ave, London W3 7TY", now()))
print(f"  Region: North Region | Store 1: Downtown Flagship | Store 2: Westside Branch")

# ── Supabase users ─────────────────────────────────────────────────────────────
print("── Supabase auth users")
admin_uid = create_or_get_supabase_user("admin@bajco.net",      "admin")
sup_uid   = create_or_get_supabase_user("supervisor@bajco.net", "supervisor",    region_id=region_id)
mgr_uid   = create_or_get_supabase_user("manager@bajco.net",    "store_manager", store_id=store1_id, region_id=region_id)
emp_uid   = create_or_get_supabase_user("employee@bajco.net",   "store_employee",store_id=store1_id)
print(f"  admin / supervisor / manager / employee  (password: {PASSWORD})")

# ── UserProfiles ───────────────────────────────────────────────────────────────
print("── UserProfiles")
for u_id, email, name, role, s_id, r_id in [
    (admin_uid, "admin@bajco.net",      "Alex Admin",      "admin",          None,      None),
    (sup_uid,   "supervisor@bajco.net",  "Sam Supervisor",  "supervisor",     None,      region_id),
    (mgr_uid,   "manager@bajco.net",     "Morgan Manager",  "store_manager",  store1_id, region_id),
    (emp_uid,   "employee@bajco.net",    "Ellis Employee",  "store_employee", store1_id, None),
]:
    cur.execute("""
        INSERT INTO "UserProfiles"
          ("UserId","Email","DisplayName","Role","StoreId","RegionId","IsActive","MustChangePassword","CreatedAt")
        VALUES (%s,%s,%s,%s,%s,%s,true,false,%s)
        ON CONFLICT ("UserId") DO UPDATE SET "Email"=EXCLUDED."Email","DisplayName"=EXCLUDED."DisplayName",
          "Role"=EXCLUDED."Role","StoreId"=EXCLUDED."StoreId","RegionId"=EXCLUDED."RegionId"
    """, (u_id, email, name, role, s_id, r_id, now()))
print("  4 profiles upserted")

# ── StoreSettings ──────────────────────────────────────────────────────────────
cur.execute('SELECT COUNT(*) FROM "StoreSettings" WHERE "StoreId"=%s', (store1_id,))
if cur.fetchone()[0] == 0:
    cur.execute("""INSERT INTO "StoreSettings"
        ("StoreId","TenantId","TimezoneId","OverdueGraceMinutes","DoughNeedTargets")
        VALUES (%s,%s,'Europe/London',15,'[]'::jsonb)""",
        (store1_id, TENANT_ID))

# ── Checklists ─────────────────────────────────────────────────────────────────
print("── Checklists & task templates")
cl1_id, cl2_id = uid(), uid()
for c_id, name, scope in [
    (cl1_id, "Morning Opening", "Store"),
    (cl2_id, "Evening Closing", "Store"),
]:
    cur.execute("""
        INSERT INTO "Checklists"
          ("Id","TenantId","Name","Scope","StoreId","IsActive","CreatedByUserId","CreatedAt")
        VALUES (%s,%s,%s,%s,%s,true,%s,%s) ON CONFLICT DO NOTHING
    """, (c_id, TENANT_ID, name, scope, store1_id, admin_uid, now()))

morning = [
    ("Unlock & Disarm",     "Cleaning",  "Unlock all entry doors and disarm the security alarm."),
    ("Lights & HVAC",       "Facilities","Switch on all floor lighting and set HVAC to operating temperature."),
    ("Temperature Log",     "Compliance","Record refrigeration unit temperatures in the daily compliance log."),
    ("Sanitise Surfaces",   "Cleaning",  "Wipe down all counters, handles, and high-touch points."),
    ("Restock Displays",    "Retail",    "Top-up front-of-store product displays from the stockroom."),
    ("Cash Till Opening",   "Finance",   "Count opening float and record in the register log."),
]
evening = [
    ("Balance Register",    "Finance",   "Reconcile the till against the POS sales report for the day."),
    ("Clean Floors",        "Cleaning",  "Sweep and mop all floor areas including the stockroom."),
    ("Secure Stock",        "Security",  "Return all loose stock to the locked storage area."),
    ("Refrigeration Check", "Compliance","Confirm all refrigeration temperatures are within permitted range."),
    ("Alarm & Lock-up",     "Security",  "Arm the security system and lock all exits."),
]

tt_rows = []  # (tt_id, cl_id, name)
for cl_id, tasks in [(cl1_id, morning), (cl2_id, evening)]:
    for name, cat, desc in tasks:
        t_id = uid()
        tt_rows.append((t_id, cl_id, name))
        cur.execute("""
            INSERT INTO "TaskTemplates"
              ("Id","TenantId","Name","Description","Category","Scope","StoreId","Fields","IsActive","CreatedByUserId","CreatedAt")
            VALUES (%s,%s,%s,%s,%s,'Store',%s,'[]'::jsonb,true,%s,%s) ON CONFLICT DO NOTHING
        """, (t_id, TENANT_ID, name, desc, cat, store1_id, admin_uid, now()))
        cur.execute("""
            INSERT INTO "ChecklistTemplateItems" ("ChecklistId","TemplateId","Order")
            VALUES (%s,%s,%s) ON CONFLICT DO NOTHING
        """, (cl_id, t_id, len([r for r in tt_rows if r[1] == cl_id])))

print(f"  2 checklists, {len(tt_rows)} task templates")

# ── Recurring assignments ──────────────────────────────────────────────────────
print("── Recurring assignments")
today = date.today()
starts_at = datetime(today.year, today.month, today.day, 0, 0, 0, tzinfo=timezone.utc).isoformat()
ra_rows = []  # (ra_id, cl_id)
for cl_id, cron, tasks in [
    (cl1_id, "0 0 6 * * ?", morning),
    (cl2_id, "0 0 17 * * ?", evening),
]:
    ra_id = uid()
    ra_rows.append((ra_id, cl_id))
    cl_name = "Morning Opening" if cl_id == cl1_id else "Evening Closing"
    cur.execute("""
        INSERT INTO "RecurringAssignments"
          ("Id","TenantId","Name","ChecklistId","StoreId","CronExpression","StartsAt",
           "IsPaused","CreatedByUserId","CreatedAt")
        VALUES (%s,%s,%s,%s,%s,%s,%s,false,%s,%s) ON CONFLICT DO NOTHING
    """, (ra_id, TENANT_ID, f"Daily {cl_name}", cl_id, store1_id, cron, starts_at, admin_uid, now()))
print(f"  {len(ra_rows)} recurring assignments")

# ── Task instances for today ───────────────────────────────────────────────────
print("── Task instances for today")
statuses_morning = ["Completed","Completed","Completed","InProgress","Pending","Pending"]
statuses_evening = ["Pending","Pending","Pending","Pending","Pending"]
instance_count = 0

for (ra_id, cl_id), statuses, due_hour in [
    (ra_rows[0], statuses_morning, 8),
    (ra_rows[1], statuses_evening, 18),
]:
    due_base = datetime(today.year, today.month, today.day, due_hour, 0, 0, tzinfo=timezone.utc)
    for idx, status in enumerate(statuses):
        due_at = (due_base + timedelta(minutes=idx * 20)).isoformat()
        completed_at = None
        completed_by = None
        if status == "Completed":
            completed_at = (due_base + timedelta(minutes=idx * 20 + 10)).isoformat()
            completed_by = emp_uid

        cur.execute("""
            INSERT INTO "TaskInstances"
              ("Id","TenantId","RecurringAssignmentId","ChecklistId","StoreId","DueAt",
               "Status","AssignedToUserId","CompletedByUserId","CompletedAt","CreatedByUserId","CreatedAt")
            VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s) ON CONFLICT DO NOTHING
        """, (uid(), TENANT_ID, ra_id, cl_id, store1_id, due_at,
              status, emp_uid, completed_by, completed_at, "system", now()))
        instance_count += 1

print(f"  {instance_count} instances (3 Completed / 1 InProgress / 7 Pending)")

# ── Form template ──────────────────────────────────────────────────────────────
print("── Form template")
fields = json.dumps([
    {"id":"f1","label":"Opening float (£)","type":"Numeric","required":True},
    {"id":"f2","label":"Register variance (£)","type":"Numeric","required":True},
    {"id":"f3","label":"Float matches expected?","type":"Boolean","required":True},
    {"id":"f4","label":"Supervisor notes","type":"Text","required":False},
])
steps = json.dumps([{"role":"supervisor","order":1}])
cur.execute("""
    INSERT INTO "FormTemplates"
      ("Id","TenantId","Name","Description","Scope","PropagationType","ApprovalSteps","Fields",
       "StoreId","IsActive","CreatedByUserId","CreatedAt")
    VALUES (%s,%s,%s,%s,'Store','Sequential',%s::jsonb,%s::jsonb,%s,true,%s,%s) ON CONFLICT DO NOTHING
""", (uid(), TENANT_ID, "Cash Count Report",
      "End-of-day cash reconciliation for supervisor review.",
      steps, fields, store1_id, admin_uid, now()))

conn.commit()
conn.close()

print()
print("═══════════════════════════════════════════════════")
print("  SEED COMPLETE — log in with any account below")
print("═══════════════════════════════════════════════════")
print(f"  Password (all): {PASSWORD}")
print()
print("  admin@bajco.net       Admin      → Dashboard")
print("  supervisor@bajco.net  Supervisor → Dashboard")
print("  manager@bajco.net     Manager    → Dashboard or Field-PWA")
print("  employee@bajco.net    Employee   → Field-PWA")
print()
print("  Tenant ID (for login form): bajco-dev")
print("  Dashboard  → http://localhost:4200")
print("  Field-PWA  → http://localhost:4201")
print("═══════════════════════════════════════════════════")
