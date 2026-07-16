#!/usr/bin/env python3
"""
Seed OPEN task instances (Pending / InProgress), due today, for the two North Region
stores (Downtown Flagship + Westside Branch) so the region-scoped admin (admin@bajco.net)
sees non-zero "Open Tasks" on the dashboard and rows on the /admin/tasks page.

Idempotent: rows are tagged with a marker in Notes and any prior run's rows are removed
before re-inserting, so re-running does not stack duplicates.

Usage:  python execution/seed_open_tasks.py
"""
import uuid
import psycopg2
from datetime import datetime, timezone, timedelta
from pathlib import Path

ROOT = Path(__file__).parent.parent
env = {}
for line in (ROOT / ".env").read_text().splitlines():
    line = line.strip()
    if line and not line.startswith("#") and "=" in line:
        k, v = line.split("=", 1)
        env[k.strip()] = v.strip()

TENANT_ID = "bajco-dev"
MARKER = "[seed:open-demo]"

cs = env["TENANT_DB_CONNECTION_STRING"]
parts = {k.strip(): v.strip() for k, v in (p.split("=", 1) for p in cs.split(";") if "=" in p)}
conn = psycopg2.connect(host=parts["Host"], dbname=parts["Database"], user=parts["Username"],
    password=parts["Password"], port=int(parts["Port"]), sslmode="require")
conn.autocommit = False
cur = conn.cursor()

def uid():
    return str(uuid.uuid4())

def now_iso():
    return datetime.now(timezone.utc).isoformat()

# --- Due-time window --------------------------------------------------------------
# A task only stays "open" (Pending/InProgress) while DueAt is in the FUTURE: the
# OverduePromotionJob sweeps any past-due open task to Overdue (then CorrectiveActionRaised).
# It must ALSO fall within the viewer's LOCAL calendar day, because the /admin/tasks
# "due today" filter windows to the browser's local midnight-to-midnight (while the
# dashboard open-count windows to UTC). This machine's local tz == the viewer's tz here.
# So schedule every task between (now + buffer) and (a little before local midnight),
# spread evenly across whatever runway remains in the local day.
now_utc = datetime.now(timezone.utc)
now_local = now_utc.astimezone()                       # machine local tz (= viewer's)
local_day_end = now_local.replace(hour=23, minute=59, second=0, microsecond=0)
window_start = now_utc + timedelta(minutes=15)         # buffer so the sweep can't catch them
window_end = local_day_end.astimezone(timezone.utc) - timedelta(minutes=10)

runway_min = (window_end - window_start).total_seconds() / 60
if runway_min < 15:
    print(f"WARNING: only {runway_min:.0f} min of runway left in the local day "
          f"({now_local:%H:%M %Z}). Seeded tasks will show only briefly before going overdue "
          f"or rolling out of 'today'. Re-run during local daytime for a full-day demo.")

STORE_DOWNTOWN = "d46c2f16-fd18-47a3-a9ec-f68263c0514f"
STORE_WESTSIDE = "c3c8cefa-3cca-45bd-a285-007ff4142752"

# (checklistId, storeId, status) — checklists confirmed active + belonging to each store.
TASK_DEFS = [
    ("7841651b-1cc2-49ac-a50b-540c1800d255", STORE_DOWNTOWN, "InProgress"),  # Morning Opening
    ("01efbb6e-10a5-4aa5-bf24-c9a60400103d", STORE_DOWNTOWN, "Pending"),     # Midday Safety & Compliance
    ("27e1959c-bcb9-4f55-892c-f0858c0160dd", STORE_DOWNTOWN, "Pending"),     # Afternoon Stock Rotation
    ("f7d4a6d8-35f4-45bd-9a78-2fa3f86c6aad", STORE_DOWNTOWN, "Pending"),     # Cash Management
    ("f8e6d671-91f5-4462-988e-77d11f08e089", STORE_DOWNTOWN, "Pending"),     # Pre-Close Manager Sign-Off
    ("77f71213-2bad-48eb-9d87-43c8502607b4", STORE_DOWNTOWN, "Pending"),     # Evening Closing
    ("1acaedb8-cb3a-466f-a495-c778f087fb13", STORE_WESTSIDE, "InProgress"),  # Westside: Morning Opening
    ("2fd3ab2b-1490-480c-a3b3-8539ff2dcef8", STORE_WESTSIDE, "Pending"),     # Westside: Evening Closing
]

# Spread the due times evenly across the remaining runway.
n = len(TASK_DEFS)
step = (window_end - window_start) / max(n - 1, 1)
TASKS = [
    (cid, sid, status, (window_start + step * i).isoformat())
    for i, (cid, sid, status) in enumerate(TASK_DEFS)
]
print(f"Scheduling {n} tasks between {window_start:%H:%M} and {window_end:%H:%M} UTC "
      f"({window_start.astimezone():%H:%M}–{window_end.astimezone():%H:%M} local).")

# Pick a real assignee per store (a store_employee/store_manager) when one exists, else NULL.
def assignee_for(store_id):
    cur.execute(
        '''SELECT "UserId" FROM "UserProfiles"
           WHERE "StoreId"=%s AND "IsActive"
                 AND "Role" IN ('store_employee','store_manager')
           ORDER BY "Role" DESC LIMIT 1''',
        (store_id,),
    )
    row = cur.fetchone()
    return row[0] if row else None

assignee = {
    STORE_DOWNTOWN: assignee_for(STORE_DOWNTOWN),
    STORE_WESTSIDE: assignee_for(STORE_WESTSIDE),
}
print(f"Assignees — Downtown: {assignee[STORE_DOWNTOWN]}, Westside: {assignee[STORE_WESTSIDE]}")

# Idempotency: clear any rows this script previously inserted.
cur.execute('DELETE FROM "TaskInstances" WHERE "TenantId"=%s AND "Notes"=%s', (TENANT_ID, MARKER))
removed = cur.rowcount
print(f"Cleared {removed} prior seeded open task(s).")

inserted = 0
for checklist_id, store_id, status, due_at in TASKS:
    cur.execute(
        '''INSERT INTO "TaskInstances"
             ("Id","TenantId","RecurringAssignmentId","ChecklistId","StoreId","DueAt",
              "Status","AssignedToUserId","CompletedByUserId","CompletedAt","Notes","CreatedByUserId","CreatedAt")
           VALUES (%s,%s,NULL,%s,%s,%s,%s,%s,NULL,NULL,%s,%s,%s)''',
        (uid(), TENANT_ID, checklist_id, store_id, due_at, status,
         assignee[store_id], MARKER, "system", now_iso()),
    )
    inserted += 1

conn.commit()
print(f"Inserted {inserted} open task instances due today "
      f"({sum(1 for t in TASKS if t[2]=='InProgress')} InProgress / "
      f"{sum(1 for t in TASKS if t[2]=='Pending')} Pending) "
      f"across Downtown Flagship + Westside Branch.")

cur.close()
conn.close()
