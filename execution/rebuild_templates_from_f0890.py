#!/usr/bin/env python3
"""
Rebuilds all task templates from the F0890 daily ops sheet field map.
Decisions applied:
  - MDOG Action field: Text (employee types A/B/C)
  - Cash Management: standalone checklist + template covering Opening, Shift Change, Closing
  - MDOG fields: flat labelled (e.g. '10" — On Hand Amount')
  - Midday: keep existing invented templates, add real PDF sections alongside

Usage:
    python3 execution/rebuild_templates_from_f0890.py
"""

import uuid, json, psycopg2
from datetime import datetime, timezone, timedelta
from pathlib import Path

# ── DB connection ─────────────────────────────────────────────────────────────
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

def uid() -> str:
    return str(uuid.uuid4())

def f(label: str, ftype: str, required: bool = True,
      range_min=None, range_max=None, corrective=None, sub_items=None) -> dict:
    return {
        "id": uid(), "label": label, "type": ftype, "required": required,
        "rangeMin": range_min, "rangeMax": range_max,
        "correctiveActionText": corrective,
        "subItems": sub_items
    }

def sub(label: str, required: bool = True) -> dict:
    return {"id": uid(), "label": label, "required": required}

def update_template(template_id: str, fields: list[dict]) -> None:
    cur.execute(
        'UPDATE "TaskTemplates" SET "Fields" = %s WHERE "Id" = %s',
        (json.dumps(fields), template_id)
    )
    print(f"  updated {template_id[:8]}  ({len(fields)} fields)")

def create_template(name: str, fields: list[dict], category: str = "Operations") -> str:
    tid = uid()
    cur.execute(
        'INSERT INTO "TaskTemplates"'
        ' ("Id","TenantId","Name","Category","Scope","Fields","IsActive","CreatedByUserId","CreatedAt")'
        ' VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s)',
        (tid, TENANT_ID, name, category, "System", json.dumps(fields),
         True, "seed", datetime.now(timezone.utc))
    )
    print(f"  created  {tid[:8]}  {name}  ({len(fields)} fields)")
    return tid

def link_template(checklist_id: str, template_id: str, order: int) -> None:
    cur.execute(
        'INSERT INTO "ChecklistTemplateItems" ("ChecklistId","TemplateId","Order")'
        ' VALUES (%s,%s,%s) ON CONFLICT DO NOTHING',
        (checklist_id, template_id, order)
    )

def create_checklist(name: str) -> str:
    cid = uid()
    cur.execute(
        'INSERT INTO "Checklists"'
        ' ("Id","TenantId","Name","Scope","StoreId","IsActive","CreatedByUserId","CreatedAt")'
        ' VALUES (%s,%s,%s,%s,%s,%s,%s,%s)',
        (cid, TENANT_ID, name, "Store", STORE_ID, True, "seed", datetime.now(timezone.utc))
    )
    print(f"  checklist {cid[:8]}  {name}")
    return cid

def create_recurring_assignment(name: str, checklist_id: str) -> str:
    raid = uid()
    cur.execute(
        'INSERT INTO "RecurringAssignments"'
        ' ("Id","TenantId","Name","ChecklistId","StoreId","CronExpression","StartsAt","IsPaused","CreatedByUserId","CreatedAt")'
        ' VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s)',
        (raid, TENANT_ID, name, checklist_id, STORE_ID,
         "0 8 * * *", datetime.now(timezone.utc), False, "seed", datetime.now(timezone.utc))
    )
    return raid

# ─────────────────────────────────────────────────────────────────────────────
# FIELD DEFINITIONS  (exact PDF labels)
# ─────────────────────────────────────────────────────────────────────────────

# ── Section 1: Communications ──────────────────────────────────────────────
COMMUNICATIONS_FIELDS = [
    f("Communications – Blast, promotions, special tasks, training notes", "Text", required=False),
]

# ── Section 2: MDOG (flat labelled) ────────────────────────────────────────
SIZES = ['10"', '12"', '14"', '16"', 'Dia']
mdog_fields: list[dict] = []
for size in SIZES:
    mdog_fields += [
        f(f'{size} — On Hand Amount',             "Numeric"),
        f(f'{size} — Production Date',            "Text"),
        f(f'{size} — Expiration Date',            "Text"),
        f(f'{size} — Today\'s Need',              "Numeric"),
        f(f'{size} — Day 2\'s Need',              "Numeric"),
        f(f'{size} — Day 3\'s Need',              "Numeric"),
        f(f'{size} — Total Needed',               "Numeric"),
        f(f'{size} — Action to Be Taken (A/B/C)', "Text"),
    ]
mdog_fields += [
    f("Cheese — On Hand Amount",             "Numeric"),
    f("Cheese — 7-Day Date",                 "Text"),
    f("Cheese — Action to Be Taken (A/B/C)", "Text"),
    f("Makeline Temperature (°F)",           "Numeric"),
    f("Walk-in Temperature (°F)",            "Numeric"),
]
MDOG_FIELDS = mdog_fields  # 45 fields

# ── Section 3: Cash Management (standalone, all 3 sessions) ────────────────
DENOMS  = ["Banks","100s/50s","20s","10s","5s","1s",
           "Quarters","Dimes","Nickels","Pennies","Total","Should Be","Variance"]
COLUMNS = ["Safe", "Till A", "Till B"]
cash_fields: list[dict] = []
for session in ["Opening", "Shift Change", "Closing"]:
    for col in COLUMNS:
        for denom in DENOMS:
            cash_fields.append(f(f"{session} — {col} — {denom}", "Numeric"))
    cash_fields.append(f(f"{session} Manager Signature", "Text", required=False))
CASH_FIELDS = cash_fields  # 120 fields (13 denoms × 3 cols × 3 sessions + 3 sig fields)

# ── Section 4: Opening Tasks ────────────────────────────────────────────────
OPENING_TASKS_FIELDS = [
    f("Arrival time",                                                                    "Text"),
    f("Conduct security walk-through",                                                   "Boolean"),
    f("Check for delayed orders",                                                        "Boolean"),
    f("Check Red Book for mgr. communication",                                           "Boolean"),
    f("Set up make line – date with label system, note all product prepped for MDOG",    "Boolean"),
    f("Place portion cups in each product",                                              "Boolean"),
    f("Print MDOG",                                                                      "Boolean"),
    f("Pull dough for lunch usage, see MDOG",                                            "Boolean"),
    f("Make sure all dough sizes are pulled & properly proofed",                         "Boolean"),
    f("Check expiration dates and discard if necessary",                                 "Boolean"),
    f("Place working thermometers in all dough sizes",                                   "Boolean"),
    f("Prep thin crust",                                                                 "Boolean"),
    f("Fill three compartment sink",                                                     "Boolean"),
]

# ── Section 5: Set Up Cut Table ─────────────────────────────────────────────
CUT_TABLE_FIELDS = [
    f("Set up cutter rack – ensure cutters available for all sauces/desserts", "Boolean"),
    f("Prep garlic cups and pepperoncini",                                     "Boolean"),
    f("Garlic parm. bottle – date with label system",                          "Boolean"),
    f("White drizzle – date with label system",                                "Boolean"),
    f("Wings sauce bottles – date with label system",                          "Boolean"),
    f("Side cups full and in place. All cups except BBQ & buffalo must be refrigerated", "Boolean"),
]

# ── Section 6: Set Up Sauce Station ─────────────────────────────────────────
SAUCE_STATION_FIELDS = [
    f("Make fresh sauce – date with label system",                "Boolean"),
    f("BBQ sauce – date with label system (6 oz spoodle)",        "Boolean"),
    f("Garlic sauce bottle – date with label system",             "Boolean"),
    f("Fill sanitizer buckets",                                   "Boolean"),
]

# ── Sections 7+8: Set Up Customer Lobby (both pages merged) ─────────────────
CUSTOMER_LOBBY_FIELDS = [
    # Section 7 — deadline 10:00 AM
    f("Floor mat in place",                                                                                              "Boolean"),
    f("Clean window ledges inside and outside",                                                                          "Boolean"),
    f("Make sure Pepsi cooler is stocked, vents and door rails clean and dust free, labels facing forward",              "Boolean"),
    f("Clean all baseboards, make sure they are free of buildup in lobby",                                               "Boolean"),
    f("Clean base of chairs, table legs & benches",                                                                      "Boolean"),
    f("Sweep parking lot – no cig. butts, trash & debris",                                                               "Boolean"),
    f("Go to bank at 9:30 AM – deposit. If unable to make deposit, supervisor must be notified by 10:00 AM",             "Boolean"),
    # Section 8 — deadline 11:00 AM
    f("Fill out deposit log and attach bank deposit slips to DOR",                                                       "Boolean"),
    f("Post deployment chart using guide",                                                                               "Boolean"),
    f("Note what to prep on MDOG",                                                                                       "Boolean"),
    f("Complete 3-day dough and hourly dough pulls",                                                                     "Boolean"),
    f("Count tills and store cash",                                                                                      "Boolean"),
    f("Turn on the open & neon signs – dust if needed, top and bottom",                                                  "Boolean"),
    f("Every item in whole unit dated with new label system – check no items are expired!",                              "Boolean"),
    f("Received, open and expiration dates – timed and initialed",                                                       "Boolean"),
    f("Place empty dough trays outside and cover",                                                                       "Boolean"),
    f("Dates checked on top and bottom of make line",                                                                    "Boolean"),
    f("Check Pizza Academy, print and post #",                                                                           "Boolean"),
]

# ── Section 9: Ongoing Duties During Lunch (Midday) ─────────────────────────
ONGOING_DUTIES_FIELDS = [
    f("Clean as you go – keep until clean and tight",                                    "Boolean"),
    f("Complete daily beautification checklist",                                         "Boolean"),
    f("Manage dough by moving back and forth to walk-in",                               "Boolean"),
    f("Down stack and patty place within 24 hrs of delivery",                           "Boolean"),
    f("When dough reaches 56°F it must be placed back into refrigeration",              "Boolean"),
]

# ── Section 10: Pre-Rush Walk Through (Midday) ──────────────────────────────
PRE_RUSH_FIELDS = [
    f("Make line stocked for dinner sales",                                                            "Boolean"),
    f("Sweep entire restaurant",                                                                       "Boolean"),
    f("Empty all trash cans",                                                                          "Boolean"),
    f("Paper towels stocked",                                                                          "Boolean"),
    f("Wipe down kitchen tables and make line",                                                        "Boolean"),
    f("Pull first dough pull",                                                                         "Boolean"),
    f("Prep area is swept and clean",                                                                  "Boolean"),
    f("Dishes done & prep table is clean",                                                             "Boolean"),
    f("Blaster printer checked",                                                                       "Boolean"),
    f("Talk staff into position",                                                                      "Boolean"),
    f("Ensure staff is aware of current promotion and answer phone with promo and up sells",           "Boolean"),
]

# ── Section 11: Deployment Guide (Evening Closing) ──────────────────────────
DEPLOYMENT_GUIDE_FIELDS = [
    f("Stocking Pepsi Cooler",                                                           "Boolean"),
    f("Use F.I.F.O. to rotate. FACE LABELS OUT",                                         "Boolean"),
    f("Trash and trash cans replaced with new liner",                                    "Boolean"),
    f("All dishes caught up, washed, rinsed and sanitized",                              "Boolean"),
    f("Drain water, clean out sink, no food particles left, replace water",              "Boolean"),
    f("Floor drain cleaned out, free of all food particles",                             "Boolean"),
    f("Bathroom cleaned", "Checklist", sub_items=[
        sub("Mirror wiped down"),
        sub("Sink wiped out"),
        sub("Toilet cleaned"),
        sub("Trash taken out and liner replaced in can"),
        sub("Sweep and mop floor"),
    ]),
    f("Walk-in swept and mopped",                                                        "Boolean"),
    f("Floor swept and mopped including underneath stacks of dough",                     "Boolean"),
    f("Mop with pink floor cleaner, wet mop then dry mop entire floor",                  "Boolean"),
    f("Empty dough trays stacked neatly in designated area",                             "Boolean"),
    f("Oven wiped down", "Checklist", sub_items=[
        sub("Wipe entire oven down with warm soapy water"),
        sub("Top, sides, oven chamber, gas lines, catch trays, legs, and wheels"),
    ]),
    f("Order taking monitors wiped down", "Checklist", sub_items=[
        sub("Wipe down monitor fronts"),
        sub("Wipe down back of monitors and data cable jacks"),
    ]),
    f("Phones wiped down", "Checklist", sub_items=[
        sub("Handset cleaned"),
        sub("Wipe down base and number pad"),
    ]),
    f("All undershelves cleaned", "Checklist", sub_items=[
        sub("Remove all boxes and supplies from undershelves, wipe down"),
        sub("Restock all boxes and supplies for following day"),
    ]),
    f("Pre sweep of store", "Checklist", sub_items=[
        sub("Lobby, production area and driver dispatch area free of paper debris and dustinator"),
    ]),
    f("Staff clocked out and sent home as per MDOG / business dictates",                 "Boolean"),
    f("Food lexans pulled from top well, cleaned, all ice or food removed from inside well", "Boolean"),
    f("Bottom well emptied and thoroughly wiped out inside on bottom and inside walls",  "Boolean"),
    f("Exterior wiped down – all sides especially near sauce station, top and front with warm soapy water only", "Boolean"),
]

# ── Section 12: Closing Checklist (Evening Closing) ─────────────────────────
CLOSING_CHECKLIST_FIELDS = [
    # Lobby
    f("Lobby cleaned",                                                                   "Boolean"),
    f("Mat swept off",                                                                   "Boolean"),
    f("All interior window ledges wiped down",                                           "Boolean"),
    f("Complete sweep, including corners",                                               "Boolean"),
    f("Mop entire Lobby – wet mop then dry mop, no water left in cracks, warm water and pink floor cleaner", "Boolean"),
    f("Target trouble areas for detailing", "Checklist", sub_items=[
        sub("Wipe walls or counters with marks, visible dirt, dust or food using soapy water"),
        sub("Make sure chairs or benches are clean and free of dustinator and dirt"),
    ]),
    f("Pepsi cooler wiped down – top, door tracks, interior fan, dust vent at bottom of cooler", "Boolean"),
    # Driver Area
    f("Wipe down monitors and keyboards",                                                "Boolean"),
    f("Wipe down and organize driver table and undershelves",                            "Boolean"),
    f("Complete and detailed sweep",                                                     "Boolean"),
    f("Spot clean walls with soapy water – any dirt, food, dark marks or dustinator",   "Boolean"),
    # Front Counter
    f("Monitors wiped down",                                                             "Boolean"),
    f("Phones wiped down",                                                               "Boolean"),
    f("Boxes restocked",                                                                 "Boolean"),
    f("Wipe down shelves and organize",                                                  "Boolean"),
    # Slap Station
    f("Wipe down counters",                                                              "Boolean"),
    f("Laminates and walls wiped down",                                                  "Boolean"),
    f("Dust off and wipe down shelves and cross bars under counters",                    "Boolean"),
    # Sauce Station
    f("Wipe down walls and sauce table",                                                 "Boolean"),
    f("Wipe down wall laminates above and near table",                                   "Boolean"),
    f("Clean side of make line, removing sauce, dustinator and food debris",             "Boolean"),
    f("Wipe down undershelves, cross bars free of dustinator and sauce",                 "Boolean"),
    # Make Line
    f("Lid hinges clean and free of buildup of food and dirt",                           "Boolean"),
    f("Handles wiped down and clean on make line lids and doors",                        "Boolean"),
    # Dish Area
    f("Sinks cleaned out, free of soap and food debris",                                 "Boolean"),
    f("Walls wiped down above dish sinks (and prep sinks if apply)",                     "Boolean"),
    f("Floor drain cleaned and free of food",                                            "Boolean"),
    # Miscellaneous
    f("Trash cans wiped down (late night trash goes out next morning for safety and security reasons)", "Boolean"),
    f("Toss all food products that will expire the next day",                            "Boolean"),
    f("Thorough and complete sweep under counters and open spaces of production, prep and dry storage area", "Boolean"),
]

# ── Section 13: Closing Admin. Checklist (Evening Closing) ──────────────────
CLOSING_ADMIN_FIELDS = [
    f("Count cash",                                                                                      "Boolean"),
    f("Ensure both till A and B are at $50 or $75 each",                                                 "Boolean"),
    f("Count out remaining $450 or $500 for store cash and lock in time delay safe",                     "Boolean"),
    f("Count out final deposit, fill out deposit slip, post and lock in time delay safe, complete Deposit Log", "Boolean"),
    f("Enter closing inventory in Profit System and verify against target for variances and finalize",   "Boolean"),
    f("Complete Nightly Numbers sheet",                                                                  "Boolean"),
    f("Complete Bad Order Log",                                                                          "Boolean"),
    f("Highlight top 5 variances on Target Inventory Cost report",                                       "Boolean"),
    f("Review Closing Checklist with closing driver staff to verify detailed completion",                "Boolean"),
    f("Clean and organize office",                                                                       "Boolean"),
    f("Till in safe, drawers left open",                                                                 "Boolean"),
    f("Switch out back up discs in CPU & safe",                                                          "Boolean"),
    f("Verify Instant Pay posted",                                                                       "Boolean"),
    f("Clock out, system closed",                                                                        "Boolean"),
]

# ─────────────────────────────────────────────────────────────────────────────
# EXISTING TEMPLATE IDs  (from DB inspection)
# ─────────────────────────────────────────────────────────────────────────────
MORNING_OPENING_CL   = "7841651b-1cc2-49ac-a50b-540c1800d255"
EVENING_CLOSING_CL   = "77f71213-2bad-48eb-9d87-43c8502607b4"
MIDDAY_CL            = "01efbb6e-10a5-4aa5-bf24-c9a60400103d"

T_OPENING_TASKS      = "9efa87e9-53f1-438b-8c19-ed8e4b1490c5"
T_MDOG               = "c6d03dff-5811-46a3-805c-b833bf23c57a"
T_OPENING_CASH       = "9fffd689-28d3-4bd9-a316-b2052e3354ef"   # to be removed
T_CUT_TABLE          = "e732a3ba-e9dd-4ae3-aa80-fa0308c432dc"
T_SAUCE_STATION      = "432317b3-4b27-4da8-93ad-97a883abc318"
T_LOBBY              = "15ce1f9b-3d92-4ff6-b710-5b85bfb2d2dc"

T_PRE_CLOSE_WT       = "c33875ff-55e9-4dda-bb1c-c8d8c0ae37a0"   # invented — leave as-is
T_DEPLOYMENT         = "cb6c627e-f4f6-49fe-abf8-0ef16d1d6488"
T_CLOSING_CL         = "2ba8a6f4-3a26-493a-b4e8-94d1b98da9be"
T_CLOSING_ADMIN      = "93e699d3-7357-4bf1-a173-5c709038ba03"

try:
    # ── 1. Morning Opening: remove stale Opening Cash Management link ─────────
    print("\n── Morning Opening ──────────────────────────────────────────────────")
    cur.execute('DELETE FROM "ChecklistTemplateItems" WHERE "TemplateId" = %s', (T_OPENING_CASH,))
    print(f"  removed Opening Cash Management from Morning Opening")

    # Add Communications template at order 0
    t_comms = create_template("Communications", COMMUNICATIONS_FIELDS, "Operations")
    link_template(MORNING_OPENING_CL, t_comms, 0)

    # Shift existing templates up by 1 to make room
    for tid, new_order in [
        (T_OPENING_TASKS, 1),
        (T_MDOG,          2),
        (T_CUT_TABLE,     3),
        (T_SAUCE_STATION, 4),
        (T_LOBBY,         5),
    ]:
        cur.execute(
            'UPDATE "ChecklistTemplateItems" SET "Order"=%s WHERE "ChecklistId"=%s AND "TemplateId"=%s',
            (new_order, MORNING_OPENING_CL, tid)
        )

    # Update all Morning Opening templates with exact PDF fields
    update_template(T_OPENING_TASKS, OPENING_TASKS_FIELDS)
    update_template(T_MDOG,          MDOG_FIELDS)
    update_template(T_CUT_TABLE,     CUT_TABLE_FIELDS)
    update_template(T_SAUCE_STATION, SAUCE_STATION_FIELDS)
    update_template(T_LOBBY,         CUSTOMER_LOBBY_FIELDS)

    # ── 2. Cash Management (standalone checklist) ─────────────────────────────
    print("\n── Cash Management (new standalone checklist) ───────────────────────")
    cash_cl_id = create_checklist("Cash Management")
    t_cash = create_template("Cash Management", CASH_FIELDS, "Finance")
    link_template(cash_cl_id, t_cash, 1)
    cash_ra_id = create_recurring_assignment("Daily Cash Management", cash_cl_id)
    print(f"  recurring assignment {cash_ra_id[:8]}")

    # ── 3. Midday: add two real PDF sections alongside existing ───────────────
    print("\n── Midday Safety & Compliance (adding PDF sections) ─────────────────")
    t_ongoing = create_template("Ongoing Duties During Lunch", ONGOING_DUTIES_FIELDS, "Operations")
    t_prerush  = create_template("Pre-Rush Walk Through",       PRE_RUSH_FIELDS,       "Operations")
    link_template(MIDDAY_CL, t_ongoing, 6)
    link_template(MIDDAY_CL, t_prerush, 7)

    # ── 4. Evening Closing: update all templates with exact PDF fields ─────────
    print("\n── Evening Closing ───────────────────────────────────────────────────")
    update_template(T_DEPLOYMENT,   DEPLOYMENT_GUIDE_FIELDS)
    update_template(T_CLOSING_CL,   CLOSING_CHECKLIST_FIELDS)
    update_template(T_CLOSING_ADMIN, CLOSING_ADMIN_FIELDS)
    print(f"  Pre-Close Walk Through ({T_PRE_CLOSE_WT[:8]}) left as-is (invented)")

    # ── 5. Fresh task instances for today ─────────────────────────────────────
    print("\n── Refreshing today's task instances ────────────────────────────────")
    today_utc = datetime.now(timezone.utc).replace(hour=0, minute=0, second=0, microsecond=0)
    today_end = today_utc + timedelta(days=1)

    cur.execute("""
        DELETE FROM "TaskInstances"
        WHERE "StoreId"=%s AND "TenantId"=%s AND "DueAt">=%s AND "DueAt"<%s
    """, (STORE_ID, TENANT_ID, today_utc, today_end))
    print(f"  deleted {cur.rowcount} stale instances")

    # Look up all recurring assignments for this store
    cur.execute("""
        SELECT ra."Id", ra."ChecklistId", cl."Name"
        FROM "RecurringAssignments" ra
        JOIN "Checklists" cl ON cl."Id" = ra."ChecklistId"
        WHERE ra."StoreId"=%s AND ra."TenantId"=%s
    """, (STORE_ID, TENANT_ID))
    assignments = cur.fetchall()

    SCHEDULE = {
        "Morning Opening":          (9,  0),
        "Cash Management":          (9, 15),   # shortly after opening
        "Midday Safety & Compliance":(12, 0),
        "Afternoon Stock Rotation": (15, 0),
        "Pre-Close Manager Sign-Off":(16,30),
        "Evening Closing":          (17, 0),
    }

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
        bst = due_at + timedelta(hours=1)
        print(f"  ✓ {cl_name:<35} due {bst.strftime('%H:%M')} BST")

    conn.commit()
    print("\n── Verification ──────────────────────────────────────────────────────")

    cur.execute("""
        SELECT cl."Name", COUNT(ci."TemplateId"), SUM(jsonb_array_length(tt."Fields"))
        FROM "Checklists" cl
        JOIN "ChecklistTemplateItems" ci ON ci."ChecklistId" = cl."Id"
        JOIN "TaskTemplates" tt ON tt."Id" = ci."TemplateId"
        WHERE cl."StoreId"=%s
        GROUP BY cl."Name" ORDER BY cl."Name"
    """, (STORE_ID,))
    print(f"\n  {'Checklist':<35} {'Templates':>9} {'Total Fields':>12}")
    print("  " + "-"*60)
    for r in cur.fetchall():
        print(f"  {r[0]:<35} {r[1]:>9} {r[2]:>12}")

    print("\n✓ Done. All templates rebuilt from F0890 field map.")

except Exception as e:
    conn.rollback()
    import traceback; traceback.print_exc()
    raise
finally:
    conn.close()
