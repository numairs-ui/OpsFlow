#!/usr/bin/env python3
"""
1. Splits Cash Management into 3 separate templates (Opening, Shift Change, Closing).
2. Refreshes today's task instances for the demo employee.
3. Walks every checklist end-to-end via the live API, submitting realistic data.

Usage:
    python3 execution/test_full_checklist_flow.py
"""

import uuid, json, psycopg2, requests
from datetime import datetime, timezone, timedelta
from pathlib import Path

ROOT = Path(__file__).parent.parent
env: dict[str, str] = {}
for line in (ROOT / ".env").read_text().splitlines():
    line = line.strip()
    if line and not line.startswith("#") and "=" in line:
        k, v = line.split("=", 1); env[k.strip()] = v.strip()

cs = env["TENANT_DB_CONNECTION_STRING"]
parts = {k.strip(): v.strip() for k, v in (p.split("=", 1) for p in cs.split(";") if "=" in p)}
conn = psycopg2.connect(host=parts["Host"], dbname=parts["Database"], user=parts["Username"],
    password=parts["Password"], port=int(parts["Port"]), sslmode="require")
conn.autocommit = False
cur = conn.cursor()

TENANT_ID   = "bajco-dev"
EMPLOYEE_ID = "1afe7cda-a3a7-4fed-803e-02fd1ca3690a"
STORE_ID    = "d46c2f16-fd18-47a3-a9ec-f68263c0514f"
BASE_URL    = "http://localhost:5000"

def uid() -> str:
    return str(uuid.uuid4())

def f(label: str, ftype: str, required: bool = True, sub_items=None) -> dict:
    return {"id": uid(), "label": label, "type": ftype, "required": required,
            "rangeMin": None, "rangeMax": None, "correctiveActionText": None,
            "subItems": sub_items}

# ─────────────────────────────────────────────────────────────────────────────
# STEP 1: Split Cash Management into 3 templates
# ─────────────────────────────────────────────────────────────────────────────
print("═" * 65)
print("STEP 1  Split Cash Management → 3 templates")
print("═" * 65)

DENOMS  = ["Banks","100s/50s","20s","10s","5s","1s",
           "Quarters","Dimes","Nickels","Pennies","Total","Should Be","Variance"]
COLUMNS = ["Safe", "Till A", "Till B"]

def build_cash_fields(session: str) -> list[dict]:
    fields = []
    for col in COLUMNS:
        for denom in DENOMS:
            fields.append(f(f"{col} — {denom}", "Numeric"))
    fields.append(f(f"{session} Manager Signature", "Text", required=False))
    return fields  # 40 fields per session

# Find the Cash Management checklist
cur.execute('SELECT "Id" FROM "Checklists" WHERE "Name"=\'Cash Management\' AND "TenantId"=%s', (TENANT_ID,))
row = cur.fetchone()
if not row:
    raise RuntimeError("Cash Management checklist not found — run rebuild_templates_from_f0890.py first")
CASH_CL_ID = str(row[0])

# Check if the 3 split templates already exist
cur.execute("SELECT COUNT(*) FROM \"TaskTemplates\" WHERE \"Name\" IN ('Opening Cash Count','Shift Change Cash Count','Closing Cash Count') AND \"TenantId\"=%s", (TENANT_ID,))
already_split = cur.fetchone()[0] == 3

if already_split:
    print(f"  Templates already split — ensuring links are current")
    cur.execute('DELETE FROM "ChecklistTemplateItems" WHERE "ChecklistId"=%s', (CASH_CL_ID,))
    cur.execute("""
        SELECT "Id","Name" FROM "TaskTemplates"
        WHERE "Name" IN ('Opening Cash Count','Shift Change Cash Count','Closing Cash Count')
          AND "TenantId"=%s ORDER BY "Name"
    """, (TENANT_ID,))
    order_map = {"Closing Cash Count": 3, "Opening Cash Count": 1, "Shift Change Cash Count": 2}
    for tid, tname in cur.fetchall():
        cur.execute('INSERT INTO "ChecklistTemplateItems" ("ChecklistId","TemplateId","Order") VALUES (%s,%s,%s)',
                    (CASH_CL_ID, tid, order_map[tname]))
        print(f"  ✓ Linked {tname} (order {order_map[tname]})")
else:
    cur.execute('DELETE FROM "ChecklistTemplateItems" WHERE "ChecklistId"=%s', (CASH_CL_ID,))
    print(f"  Removed existing template links — creating 3 split templates")
    for order, (session, category) in enumerate([
        ("Opening Cash Count",     "Finance"),
        ("Shift Change Cash Count","Finance"),
        ("Closing Cash Count",     "Finance"),
    ], start=1):
        tid = uid()
        fields = build_cash_fields(session)
        cur.execute(
            'INSERT INTO "TaskTemplates"'
            ' ("Id","TenantId","Name","Category","Scope","Fields","IsActive","CreatedByUserId","CreatedAt")'
            ' VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s)',
            (tid, TENANT_ID, session, category, "System", json.dumps(fields),
             True, "seed", datetime.now(timezone.utc))
        )
        cur.execute('INSERT INTO "ChecklistTemplateItems" ("ChecklistId","TemplateId","Order") VALUES (%s,%s,%s)',
                    (CASH_CL_ID, tid, order))
        print(f"  ✓ {session} ({len(fields)} fields, order {order})")

# ─────────────────────────────────────────────────────────────────────────────
# STEP 2: Refresh today's task instances
# ─────────────────────────────────────────────────────────────────────────────
print("\n" + "═" * 65)
print("STEP 2  Refresh today's task instances")
print("═" * 65)

today_utc = datetime.now(timezone.utc).replace(hour=0, minute=0, second=0, microsecond=0)
today_end = today_utc + timedelta(days=1)

cur.execute("""
    DELETE FROM "TaskInstances"
    WHERE "StoreId"=%s AND "TenantId"=%s AND "DueAt">=%s AND "DueAt"<%s
""", (STORE_ID, TENANT_ID, today_utc, today_end))
print(f"  Deleted {cur.rowcount} stale instances")

cur.execute("""
    SELECT ra."Id", ra."ChecklistId", cl."Name"
    FROM "RecurringAssignments" ra
    JOIN "Checklists" cl ON cl."Id" = ra."ChecklistId"
    WHERE ra."StoreId"=%s AND ra."TenantId"=%s
""", (STORE_ID, TENANT_ID))
assignments = cur.fetchall()

SCHEDULE = {
    "Morning Opening":           (9,  0),
    "Cash Management":           (9, 15),
    "Midday Safety & Compliance":(12, 0),
    "Afternoon Stock Rotation":  (15, 0),
    "Pre-Close Manager Sign-Off":(16,30),
    "Evening Closing":           (17, 0),
}

task_ids: dict[str, str] = {}
for ra_id, cl_id, cl_name in assignments:
    hour, minute = SCHEDULE.get(cl_name, (9, 0))
    due_at = today_utc.replace(hour=hour, minute=minute)
    ti_id = uid()
    cur.execute("""
        INSERT INTO "TaskInstances"
            ("Id","TenantId","RecurringAssignmentId","ChecklistId","StoreId",
             "DueAt","Status","AssignedToUserId","CreatedByUserId","CreatedAt")
        VALUES (%s,%s,%s,%s,%s,%s,'Pending',%s,'seed',%s)
    """, (ti_id, TENANT_ID, ra_id, cl_id, STORE_ID,
          due_at, EMPLOYEE_ID, datetime.now(timezone.utc)))
    task_ids[cl_name] = ti_id
    bst = due_at + timedelta(hours=1)
    print(f"  ✓ {cl_name:<35} {ti_id[:8]}  due {bst.strftime('%H:%M')} BST")

conn.commit()
conn.close()

# ─────────────────────────────────────────────────────────────────────────────
# STEP 3: Login as employee
# ─────────────────────────────────────────────────────────────────────────────
print("\n" + "═" * 65)
print("STEP 3  Login as employee@bajco.net")
print("═" * 65)

session_http = requests.Session()
login_resp = session_http.post(f"{BASE_URL}/auth/login", json={
    "email": "employee@bajco.net",
    "password": "Demo1234!",
    "tenantId": "bajco-dev"
})
if login_resp.status_code != 200:
    print(f"  LOGIN FAILED {login_resp.status_code}: {login_resp.text}")
    raise SystemExit(1)
token = login_resp.json()["accessToken"]
headers = {"Authorization": f"Bearer {token}"}
print(f"  ✓ Logged in — token acquired")

# ─────────────────────────────────────────────────────────────────────────────
# STEP 4: Walk every task — fetch detail, build answer payload, submit
# ─────────────────────────────────────────────────────────────────────────────
def sample_value(field: dict) -> str:
    label = field["label"].lower()
    ftype = field["type"]

    if ftype == "Boolean":
        return "true"

    if ftype == "Text":
        if "time" in label or "arrival" in label:
            return "09:00 AM"
        if "date" in label or "expir" in label or "prod" in label or "7-day" in label:
            return "12/28/2022"
        if "signature" in label or "mgr" in label or "initials" in label:
            return "JD"
        if "action" in label and "a/b/c" in label:
            return "A"
        if "comms" in label or "blast" in label or "promo" in label:
            return "No special promotions today."
        return "OK"

    if ftype == "Numeric":
        if "temperature" in label or "temp" in label:
            return "38"
        if "on hand" in label:
            return "24"
        if "need" in label:
            return "12"
        if "total needed" in label:
            return "36"
        if "banks" in label:
            return "200"
        if "100" in label:
            return "100"
        if "20" in label:
            return "60"
        if "10" in label:
            return "30"
        if "5" in label:
            return "20"
        if "1" in label:
            return "10"
        if "quarter" in label:
            return "10"
        if "dime" in label:
            return "5"
        if "nickel" in label:
            return "2"
        if "penni" in label:
            return "1"
        if "total" in label:
            return "438"
        if "should be" in label:
            return "438"
        if "variance" in label:
            return "0"
        if "safe" in label or "count" in label or "amount" in label:
            return "450"
        return "10"

    if ftype == "Checklist" and field.get("subItems"):
        # Return all sub-item IDs (required ones get checked)
        return ",".join(s["id"] for s in field["subItems"])

    return "OK"

PASS  = "✓"
FAIL  = "✗"
results = []

# Determine order to walk checklists
ordered_checklists = [
    "Morning Opening",
    "Cash Management",
    "Midday Safety & Compliance",
    "Afternoon Stock Rotation",
    "Pre-Close Manager Sign-Off",
    "Evening Closing",
]

for cl_name in ordered_checklists:
    ti_id = task_ids.get(cl_name)
    if not ti_id:
        print(f"\n  SKIP {cl_name} — no task instance")
        continue

    print(f"\n{'─'*65}")
    print(f"  CHECKLIST: {cl_name}")
    print(f"{'─'*65}")

    # Fetch task detail
    detail_resp = session_http.get(f"{BASE_URL}/tasks/{ti_id}", headers=headers)
    if detail_resp.status_code != 200:
        print(f"  {FAIL} GET /tasks/{ti_id[:8]} → {detail_resp.status_code}: {detail_resp.text[:200]}")
        results.append((cl_name, False, f"detail fetch failed {detail_resp.status_code}"))
        continue

    detail = detail_resp.json()
    templates = detail.get("templates", [])
    print(f"  Status: {detail.get('status')}  |  Templates: {len(templates)}")

    if not templates:
        print(f"  ⚠ No templates — submitting empty payload")

    # Build field values from real field IDs in the template
    field_values = []
    field_count = 0
    for tmpl in templates:
        tmpl_id = tmpl["templateId"]
        try:
            fields = json.loads(tmpl.get("fieldsJson", "[]"))
        except Exception:
            fields = []

        print(f"    [{tmpl['order']}] {tmpl['templateName']} — {len(fields)} fields")
        for field in fields:
            value = sample_value(field)
            field_values.append({
                "templateId": tmpl_id,
                "fieldId": field["id"],
                "value": value
            })
            field_count += 1

    print(f"  Submitting {field_count} field answers...")

    complete_resp = session_http.post(
        f"{BASE_URL}/tasks/{ti_id}/complete",
        headers=headers,
        json={"completedByVolunteerName": None, "fieldValues": field_values}
    )

    if complete_resp.status_code == 200:
        body = complete_resp.json()
        corrective = body.get("triggeredCorrectiveActions", [])
        if corrective:
            print(f"  {PASS} COMPLETED (with {len(corrective)} corrective action(s))")
            for ca in corrective:
                print(f"       ⚠  {ca['fieldLabel']}: {ca['text']}")
        else:
            print(f"  {PASS} COMPLETED — clean, no corrective actions")
        results.append((cl_name, True, f"{field_count} fields, {len(corrective)} corrective actions"))
    else:
        print(f"  {FAIL} FAILED → {complete_resp.status_code}")
        try:
            err = complete_resp.json()
            errors = err.get("errors", err)
            if isinstance(errors, dict):
                for k, v in list(errors.items())[:5]:
                    print(f"       {k}: {v}")
            else:
                print(f"       {str(errors)[:400]}")
        except Exception:
            print(f"       {complete_resp.text[:400]}")
        results.append((cl_name, False, f"HTTP {complete_resp.status_code}"))

# ─────────────────────────────────────────────────────────────────────────────
# STEP 5: Summary
# ─────────────────────────────────────────────────────────────────────────────
print(f"\n{'═'*65}")
print("SUMMARY")
print("═" * 65)
passed = sum(1 for _, ok, _ in results if ok)
print(f"  {passed}/{len(results)} checklists completed successfully\n")
for cl_name, ok, note in results:
    icon = PASS if ok else FAIL
    print(f"  {icon} {cl_name:<35} {note}")
