#!/usr/bin/env python3
"""
Reset today's Downtown Flagship tasks to Pending with correct due times,
assign them all to the demo employee (employee@bajco.net), and ensure the
task instances correctly reference the real PDF checklist templates.

Usage:
    python3 execution/assign_employee_tasks.py
"""
import uuid, psycopg2
from datetime import datetime, timezone, timedelta
from pathlib import Path

ROOT = Path(__file__).parent.parent
env: dict[str, str] = {}
for line in (ROOT / ".env").read_text().splitlines():
    line = line.strip()
    if line and not line.startswith("#") and "=" in line:
        k, v = line.split("=", 1)
        env[k.strip()] = v.strip()

TENANT_ID   = "bajco-dev"
EMPLOYEE_ID = "1afe7cda-a3a7-4fed-803e-02fd1ca3690a"   # employee@bajco.net
STORE_ID    = "d46c2f16-fd18-47a3-a9ec-f68263c0514f"    # Downtown Flagship

cs = env["TENANT_DB_CONNECTION_STRING"]
parts = {k.strip(): v.strip() for k, v in (p.split("=", 1) for p in cs.split(";") if "=" in p)}
conn = psycopg2.connect(host=parts["Host"], dbname=parts["Database"], user=parts["Username"],
    password=parts["Password"], port=int(parts["Port"]), sslmode="require")
conn.autocommit = False
cur = conn.cursor()

def uid() -> str:
    return str(uuid.uuid4())

# ── Build today's UTC timestamps for each checklist due time ─────────────────
# Treating store as UK-based: UTC+1 (BST). Due times are local → convert to UTC.
# Morning Opening: 10:00 BST = 09:00 UTC
# Midday Safety:   13:00 BST = 12:00 UTC
# Stock Rotation:  16:00 BST = 15:00 UTC
# Evening Closing: 18:00 BST = 17:00 UTC
# Pre-Close:       17:30 BST = 16:30 UTC

now_utc   = datetime.now(timezone.utc)
today_utc = now_utc.replace(hour=0, minute=0, second=0, microsecond=0)

SCHEDULE = [
    # (checklist_name,              hour_utc, min_utc)
    ("Morning Opening",              9,  0),
    ("Midday Safety & Compliance",  12,  0),
    ("Afternoon Stock Rotation",    15,  0),
    ("Pre-Close Manager Sign-Off",  16, 30),
    ("Evening Closing",             17,  0),
]

# Resolve checklist IDs
cur.execute("""
    SELECT "Id","Name" FROM "Checklists"
    WHERE "StoreId" = %s AND "TenantId" = %s
""", (STORE_ID, TENANT_ID))
checklist_map = {name: cid for cid, name in cur.fetchall()}
print("Checklists found:", list(checklist_map.keys()))

# Resolve recurring assignment IDs (so we keep the FK intact)
cur.execute("""
    SELECT "Id","ChecklistId" FROM "RecurringAssignments"
    WHERE "StoreId" = %s AND "TenantId" = %s
""", (STORE_ID, TENANT_ID))
ra_by_checklist = {str(cl_id): ra_id for ra_id, cl_id in cur.fetchall()}

# ── Delete today's existing task instances for this store ────────────────────
today_start = today_utc
today_end   = today_utc + timedelta(days=1)

cur.execute("""
    DELETE FROM "TaskInstances"
    WHERE "StoreId" = %s AND "TenantId" = %s
      AND "DueAt" >= %s AND "DueAt" < %s
""", (STORE_ID, TENANT_ID, today_start, today_end))
deleted = cur.rowcount
print(f"\nDeleted {deleted} stale task instance(s) for today.")

# ── Create fresh task instances assigned to the demo employee ─────────────────
print("\nCreating fresh task instances:")
for cl_name, hour, minute in SCHEDULE:
    cl_id = checklist_map.get(cl_name)
    if not cl_id:
        print(f"  SKIP — checklist not found: {cl_name}")
        continue

    due_at = today_utc.replace(hour=hour, minute=minute)
    # If due time already passed today, keep today's date (employee should still complete)
    ra_id  = ra_by_checklist.get(cl_id)
    task_id = uid()

    cur.execute("""
        INSERT INTO "TaskInstances"
            ("Id","TenantId","RecurringAssignmentId","ChecklistId","StoreId",
             "DueAt","Status","AssignedToUserId","CreatedByUserId","CreatedAt")
        VALUES (%s,%s,%s,%s,%s,%s,'Pending',%s,'seed',%s)
    """, (
        task_id, TENANT_ID, ra_id, cl_id, STORE_ID,
        due_at, EMPLOYEE_ID, datetime.now(timezone.utc)
    ))

    local_time = due_at + timedelta(hours=1)  # display as BST
    print(f"  ✓ {cl_name} — due {local_time.strftime('%H:%M')} BST — assigned to employee@bajco.net")

# ── Verify ───────────────────────────────────────────────────────────────────
try:
    conn.commit()

    cur.execute("""
        SELECT ti."Id", cl."Name", ti."Status", ti."DueAt", ti."AssignedToUserId",
               COUNT(ci."TemplateId") as template_count
        FROM "TaskInstances" ti
        JOIN "Checklists" cl ON cl."Id" = ti."ChecklistId"
        JOIN "ChecklistTemplateItems" ci ON ci."ChecklistId" = cl."Id"
        WHERE ti."StoreId" = %s AND ti."DueAt" >= %s AND ti."DueAt" < %s
        GROUP BY ti."Id", cl."Name", ti."Status", ti."DueAt", ti."AssignedToUserId"
        ORDER BY ti."DueAt"
    """, (STORE_ID, today_start, today_end))

    print("\n── Final state — today's tasks for employee@bajco.net ──")
    for r in cur.fetchall():
        local = r[3] + timedelta(hours=1)
        print(f"  {r[0][:8]}  {r[1]:<35} {r[2]:<8}  {local.strftime('%H:%M')} BST  {r[5]} templates")

    print("\n✓ Done. Log in as employee@bajco.net at http://localhost:4201")

except Exception as e:
    conn.rollback()
    print(f"ERROR: {e}")
    raise
finally:
    conn.close()
