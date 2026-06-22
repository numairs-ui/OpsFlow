#!/usr/bin/env python3
"""
Seed real checklist template items from the F0890 daily operations PDF.
Replaces the empty placeholder templates on the main store's Morning Opening
and Evening Closing checklists with the actual Domino's-style daily ops fields.

Usage:
    python3 execution/seed_real_checklists.py
"""
import json, uuid, sys
import psycopg2
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).parent.parent
env: dict[str, str] = {}
for line in (ROOT / ".env").read_text().splitlines():
    line = line.strip()
    if line and not line.startswith("#") and "=" in line:
        k, v = line.split("=", 1)
        env[k.strip()] = v.strip()

TENANT_ID = "bajco-dev"
cs = env["TENANT_DB_CONNECTION_STRING"]
parts = {k.strip(): v.strip() for k, v in (p.split("=", 1) for p in cs.split(";") if "=" in p)}
conn = psycopg2.connect(
    host=parts["Host"], dbname=parts["Database"], user=parts["Username"],
    password=parts["Password"], port=int(parts["Port"]), sslmode="require"
)
conn.autocommit = False
cur = conn.cursor()

def uid() -> str:
    return str(uuid.uuid4())

def ts() -> str:
    return datetime.now(timezone.utc).isoformat()

def field(id: str, label: str, ftype: str, required: bool = True,
          rmin=None, rmax=None, corrective: str | None = None,
          sub_items: list | None = None) -> dict:
    return {
        "id": id,
        "label": label,
        "type": ftype,
        "required": required,
        "rangeMin": rmin,
        "rangeMax": rmax,
        "correctiveActionText": corrective,
        "subItems": sub_items,
    }

def check_item(id: str, label: str, required: bool = True) -> dict:
    return {"id": id, "label": label, "required": required}

# ── Resolve target checklists ────────────────────────────────────────────────
cur.execute("""
    SELECT "Id", "Name", "StoreId"
    FROM "Checklists"
    WHERE "TenantId" = %s AND "Name" IN ('Morning Opening', 'Evening Closing')
    ORDER BY "Name"
""", (TENANT_ID,))
rows = cur.fetchall()
checklists: dict[str, str] = {}
for row_id, name, store_id in rows:
    checklists[name] = row_id
    print(f"  Found checklist: {name} → {row_id[:8]}… store={str(store_id)[:8]}")

if "Morning Opening" not in checklists or "Evening Closing" not in checklists:
    print("ERROR: Could not find both Morning Opening and Evening Closing checklists.", file=sys.stderr)
    sys.exit(1)

morning_id = checklists["Morning Opening"]
evening_id = checklists["Evening Closing"]

# ── Real template definitions from F0890 daily ops PDF ──────────────────────

MORNING_TEMPLATES = [
    {
        "name": "Opening Tasks",
        "description": "Initial opening duties — must be completed by 10:00 AM",
        "category": "Operations",
        "fields": [
            field("arrival_time",       "Arrival Time (AM)",                       "Text"),
            field("security_walk",      "Conduct security walk-through",            "Boolean"),
            field("delayed_orders",     "Check for delayed orders",                 "Boolean", required=False),
            field("red_book",           "Check Red Book for manager communication", "Boolean"),
            field("makeline_setup",     "Set up make line — date & label all product (note MDOG prep)", "Boolean",
                  corrective="Make line not properly labelled — flag for manager review"),
            field("portion_cups",       "Place portion cups in each product",       "Boolean"),
            field("print_mdog",         "Print MDOG sheet",                         "Boolean"),
            field("pull_lunch_dough",   "Pull dough for lunch usage per MDOG",      "Boolean"),
            field("dough_proofed",      "All dough sizes pulled and properly proofed", "Boolean"),
            field("exp_dates",          "Check expiration dates and discard if necessary", "Boolean"),
            field("thermometers",       "Place working thermometers in all dough sizes", "Boolean"),
            field("prep_thin",          "Prep thin crust",                          "Boolean", required=False),
            field("fill_sink",          "Fill three-compartment sink",              "Boolean"),
        ]
    },
    {
        "name": "3-Day Dough & Cheese Management (MDOG)",
        "description": "Track on-hand amounts and 3-day needs per dough size",
        "category": "Inventory",
        "fields": [
            field("walkin_temp",   'Walk-in Temperature (°F)',   "Numeric", rmin=28, rmax=40,
                  corrective='Walk-in temp out of safe range — notify manager immediately'),
            field("makeline_temp", 'Makeline Temperature (°F)',  "Numeric", rmin=28, rmax=40,
                  corrective='Makeline temp out of range — check refrigeration, do not use product'),
            field("dough_10_on_hand",   '10" Dough — On Hand',         "Numeric", rmin=0),
            field("dough_10_today",     '10" Dough — Today\'s Need',    "Numeric", rmin=0, required=False),
            field("dough_10_day2",      '10" Dough — Day 2 Need',       "Numeric", rmin=0, required=False),
            field("dough_10_day3",      '10" Dough — Day 3 Need',       "Numeric", rmin=0, required=False),
            field("dough_12_on_hand",   '12" Dough — On Hand',          "Numeric", rmin=0),
            field("dough_12_today",     '12" Dough — Today\'s Need',    "Numeric", rmin=0, required=False),
            field("dough_12_day2",      '12" Dough — Day 2 Need',       "Numeric", rmin=0, required=False),
            field("dough_12_day3",      '12" Dough — Day 3 Need',       "Numeric", rmin=0, required=False),
            field("dough_14_on_hand",   '14" Dough — On Hand',          "Numeric", rmin=0),
            field("dough_14_today",     '14" Dough — Today\'s Need',    "Numeric", rmin=0, required=False),
            field("dough_14_day2",      '14" Dough — Day 2 Need',       "Numeric", rmin=0, required=False),
            field("dough_14_day3",      '14" Dough — Day 3 Need',       "Numeric", rmin=0, required=False),
            field("dough_16_on_hand",   '16" Dough — On Hand',          "Numeric", rmin=0),
            field("dough_16_today",     '16" Dough — Today\'s Need',    "Numeric", rmin=0, required=False),
            field("dough_16_day2",      '16" Dough — Day 2 Need',       "Numeric", rmin=0, required=False),
            field("dough_16_day3",      '16" Dough — Day 3 Need',       "Numeric", rmin=0, required=False),
            field("dough_dia_on_hand",  'Dia Dough — On Hand',           "Numeric", rmin=0, required=False),
            field("cheese_on_hand",     'Cheese — On Hand (lbs)',         "Numeric", rmin=0),
            field("mdog_action",        'MDOG Action (A = OK, B = Excess, C = Short)', "Text",
                  corrective='Action C/short dough — contact other stores and/or place emergency order'),
        ]
    },
    {
        "name": "Opening Cash Management",
        "description": "Count and record opening till amounts for Safe, Till A, and Till B",
        "category": "Finance",
        "fields": [
            field("safe_opening",       "Safe — Opening Amount ($)",    "Numeric", rmin=0,
                  corrective="Safe variance detected — do not proceed, contact supervisor"),
            field("till_a_opening",     "Till A — Opening Amount ($)",  "Numeric", rmin=0,
                  corrective="Till A variance — record reason and manager initials"),
            field("till_b_opening",     "Till B — Opening Amount ($)",  "Numeric", rmin=0,
                  corrective="Till B variance — record reason and manager initials"),
            field("opening_mgr",        "Opening Manager Initials",     "Text"),
            field("deposit_bank",       "Deposit taken to bank by 9:30 AM", "Boolean",
                  corrective="Deposit not made — supervisor must be notified by 10:00 AM"),
        ]
    },
    {
        "name": "Set Up Cut Table",
        "description": "Prepare cut table and sauce stations before opening",
        "category": "Operations",
        "fields": [
            field("cutter_rack",        "Set up cutter rack — ensure cutters available for all sauces/desserts", "Boolean"),
            field("garlic_cups",        "Prep garlic cups and pepperoncini",            "Boolean"),
            field("garlic_parm",        "Garlic parm. bottle — dated with label system","Boolean"),
            field("white_drizzle",      "White drizzle — dated with label system",      "Boolean"),
            field("wings_sauce",        "Wings sauce bottles — dated with label system","Boolean"),
            field("side_cups",          "Side cups full and in place (BBQ & buffalo must be refrigerated)", "Boolean"),
        ]
    },
    {
        "name": "Set Up Sauce Station",
        "description": "Prepare all sauces with proper dating",
        "category": "Operations",
        "fields": [
            field("fresh_sauce",        "Make fresh sauce — dated with label system",   "Boolean"),
            field("bbq_sauce",          "BBQ sauce — dated with label system (6 oz spoodle)", "Boolean"),
            field("garlic_sauce",       "Garlic sauce bottle — dated with label system","Boolean"),
            field("sanitizer_buckets",  "Fill sanitizer buckets",                       "Boolean"),
        ]
    },
    {
        "name": "Set Up Customer Lobby",
        "description": "Customer-facing area ready before opening — all complete by 10:00 AM",
        "category": "Cleaning",
        "fields": [
            field("floor_mat",          "Floor mat in place",                           "Boolean"),
            field("window_ledges",      "Clean window ledges inside and outside",       "Boolean"),
            field("pepsi_cooler",       "Pepsi cooler stocked, vents clean, labels forward", "Boolean"),
            field("baseboards",         "Baseboards free of buildup in lobby",          "Boolean"),
            field("chairs_benches",     "Chair bases, table legs, and benches clean",   "Boolean"),
            field("parking_lot",        "Parking lot swept — no cigarette butts or trash", "Boolean"),
            field("open_neon",          "Turn on open & neon signs — dust if needed",  "Boolean"),
            field("label_check",        "Every item in whole unit dated — no expired items", "Boolean",
                  corrective="Expired product found — discard immediately and record on waste log"),
            field("dough_trays",        "Empty dough trays placed outside and covered","Boolean"),
            field("pizza_academy",      "Check Pizza Academy — print and post #",       "Boolean", required=False),
        ]
    },
]

EVENING_TEMPLATES = [
    {
        "name": "Pre-Close Walk Through",
        "description": "Pre-rush and dinner line prep — must be complete by 3:30 PM",
        "category": "Operations",
        "fields": [
            field("makeline_stocked",   "Make line stocked for dinner sales",           "Boolean"),
            field("sweep_restaurant",   "Sweep entire restaurant",                      "Boolean"),
            field("empty_trash",        "Empty all trash cans",                         "Boolean"),
            field("paper_towels",       "Paper towels stocked",                         "Boolean"),
            field("wipe_tables",        "Wipe down kitchen tables and make line",       "Boolean"),
            field("first_dough_pull",   "Pull first dough pull",                        "Boolean"),
            field("prep_clean",         "Prep area is swept and clean",                 "Boolean"),
            field("dishes_done",        "Dishes done and prep table clean",             "Boolean"),
            field("blaster_checked",    "Blaster printer checked",                      "Boolean", required=False),
            field("talk_staff",         "Talk staff into position",                     "Boolean"),
            field("promo_briefing",     "Staff aware of current promotions and phone promo", "Boolean"),
        ]
    },
    {
        "name": "Deployment Guide",
        "description": "End-of-shift deployment tasks — assigned before opening manager leaves",
        "category": "Cleaning",
        "fields": [
            field("pepsi_stocked",      "Pepsi cooler stocked (FIFO — face labels out)","Boolean"),
            field("trash_lined",        "Trash and trash cans replaced with new liner", "Boolean"),
            field("dishes_clean",       "All dishes caught up — washed, rinsed, sanitised", "Boolean"),
            field("sink_drained",       "Drain water, clean out sink, no food particles", "Boolean"),
            field("floor_drain",        "Floor drain cleaned out, free of all food particles", "Boolean"),
            field("bathroom",           "Bathroom cleaned (mirror, sink, toilet, trash, sweep & mop)", "Boolean"),
            field("walkin_mopped",      "Walk-in swept and mopped with pink floor cleaner", "Boolean"),
            field("dough_trays_stored", "Empty dough trays stacked neatly or stored outside in 25s with cover", "Boolean"),
            field("oven_clean",         "Oven wiped down (top, sides, chamber, gas lines, catch trays)", "Boolean"),
            field("monitors_clean",     "Order taking monitors wiped down (front, back, cables)", "Boolean"),
            field("phones_clean",       "Phones wiped down — handset, base, number pad","Boolean"),
            field("undershelves",       "All undershelves cleared, wiped, and restocked for next day", "Boolean"),
            field("pre_sweep",          "Pre-sweep: lobby, production, driver dispatch area free of debris", "Boolean"),
            field("food_lexans",        "Food lexans pulled from top well, cleaned, no ice or food", "Boolean"),
            field("exterior_clean",     "Exterior wiped down — especially next to sauce station", "Boolean"),
        ]
    },
    {
        "name": "Closing Checklist",
        "description": "Full closing sweep and clean — lobby, driver area, production surfaces",
        "category": "Cleaning",
        "fields": [
            field("lobby_cleaned",      "Lobby cleaned",                                "Boolean"),
            field("mat_swept",          "Mat swept off",                                "Boolean"),
            field("window_ledges",      "All interior window ledges wiped down",        "Boolean"),
            field("complete_sweep",     "Complete sweep, including corners",            "Boolean"),
            field("lobby_mopped",       "Lobby mopped (wet + dry mop, pink floor cleaner)", "Boolean"),
            field("detail_walls",       "Spot-clean walls/counters with marks, dirt, or food", "Boolean"),
            field("chairs_clean",       "Chairs and benches free of dustinator and dirt","Boolean"),
            field("pepsi_wiped",        "Pepsi cooler wiped down (top, door tracks, fan, dust vent)", "Boolean"),
            field("driver_monitors",    "Driver area — monitors and keyboards wiped",   "Boolean"),
            field("driver_table",       "Driver table and undershelves wiped and organised", "Boolean"),
            field("driver_sweep",       "Driver area — complete detailed sweep",        "Boolean"),
            field("front_counter",      "Front counter: monitors, phones, boxes, shelves wiped", "Boolean"),
            field("slap_station",       "Slap station: counters, laminates, shelves, cross bars", "Boolean"),
            field("sauce_station",      "Sauce station: walls, table, laminates, make line side, undershelves", "Boolean"),
            field("makeline_lids",      "Make line: lid hinges clean, handles wiped",   "Boolean"),
            field("dish_area",          "Dish area: sinks clean, walls wiped, floor drain free", "Boolean"),
            field("trash_cans",         "Trash cans wiped down (late night trash out next morning)", "Boolean"),
            field("toss_expiring",      "Toss all food products expiring next day",     "Boolean",
                  corrective="Expired product found on close — record on waste log and flag for AM"),
            field("under_counter_sweep","Thorough sweep under counters and open spaces","Boolean"),
        ]
    },
    {
        "name": "Closing Admin & Cash",
        "description": "End-of-day cash reconciliation, deposit, and system close",
        "category": "Finance",
        "fields": [
            field("count_cash",         "Count cash",                                   "Boolean"),
            field("till_a_close",       "Till A at target ($50 or $75)",               "Numeric", rmin=0,
                  corrective="Till A variance at close — document discrepancy and manager initials required"),
            field("till_b_close",       "Till B at target ($50 or $75)",               "Numeric", rmin=0,
                  corrective="Till B variance at close — document discrepancy and manager initials required"),
            field("store_cash_safe",    "Remaining store cash ($450/$500) locked in time delay safe", "Boolean",
                  corrective="Store cash not locked in safe — do not leave until resolved"),
            field("deposit_slip",       "Final deposit counted, deposit slip filled, locked in safe", "Boolean",
                  corrective="Deposit not secured — supervisor must be notified before close"),
            field("deposit_log",        "Deposit Log completed",                        "Boolean"),
            field("closing_inventory",  "Closing inventory entered in Profit System and verified", "Boolean"),
            field("nightly_numbers",    "Nightly Numbers sheet completed",              "Boolean"),
            field("bad_order_log",      "Bad Order Log completed",                      "Boolean"),
            field("top5_variances",     "Top 5 variances highlighted on Target Inventory Cost report", "Boolean", required=False),
            field("checklist_review",   "Closing Checklist reviewed with closing driver staff", "Boolean"),
            field("office_clean",       "Office cleaned and organised",                 "Boolean"),
            field("till_in_safe",       "Till in safe, drawers left open",              "Boolean"),
            field("backup_discs",       "Switch out back-up discs in CPU & safe",       "Boolean", required=False),
            field("instant_pay",        "Verify Instant Pay posted",                   "Boolean"),
            field("clock_out",          "Clock out — system closed",                   "Boolean"),
            field("closing_mgr",        "Closing Manager Initials",                    "Text"),
        ]
    },
]

def upsert_template(name: str, description: str, category: str, fields: list) -> str:
    """Insert a new TaskTemplate and return its ID. Existing ones with the same name are reused."""
    cur.execute("""
        SELECT "Id" FROM "TaskTemplates"
        WHERE "TenantId" = %s AND "Name" = %s AND "Category" = %s
        LIMIT 1
    """, (TENANT_ID, name, category))
    row = cur.fetchone()
    if row:
        tmpl_id = row[0]
        # Update fields
        cur.execute("""UPDATE "TaskTemplates" SET "Fields" = %s::jsonb, "Description" = %s WHERE "Id" = %s""",
                    (json.dumps(fields), description, tmpl_id))
        print(f"    ↻ Updated template: {name} [{tmpl_id[:8]}…]")
        return tmpl_id

    tmpl_id = uid()
    cur.execute("""
        INSERT INTO "TaskTemplates"
            ("Id","TenantId","Name","Description","Category","Scope","Fields","IsActive","CreatedByUserId","CreatedAt")
        VALUES (%s, %s, %s, %s, %s, 'System', %s::jsonb, true, 'seed', %s)
    """, (tmpl_id, TENANT_ID, name, description, category, json.dumps(fields), ts()))
    print(f"    + Created template: {name} [{tmpl_id[:8]}…]")
    return tmpl_id

def replace_checklist_templates(checklist_id: str, checklist_name: str, templates: list[dict]):
    """Remove all existing ChecklistTemplateItems and replace with the real ones."""
    cur.execute('DELETE FROM "ChecklistTemplateItems" WHERE "ChecklistId" = %s', (checklist_id,))
    print(f"\n  Cleared existing templates for: {checklist_name}")

    for order, tmpl_def in enumerate(templates, start=1):
        tmpl_id = upsert_template(
            tmpl_def["name"],
            tmpl_def["description"],
            tmpl_def["category"],
            tmpl_def["fields"]
        )
        cur.execute("""
            INSERT INTO "ChecklistTemplateItems" ("ChecklistId","TemplateId","Order")
            VALUES (%s, %s, %s)
            ON CONFLICT DO NOTHING
        """, (checklist_id, tmpl_id, order))

    print(f"  ✓ Linked {len(templates)} templates to {checklist_name}")

# ── Execute ──────────────────────────────────────────────────────────────────
try:
    print(f"\nSeeding real checklist templates from F0890 daily ops PDF...")
    replace_checklist_templates(morning_id, "Morning Opening", MORNING_TEMPLATES)
    replace_checklist_templates(evening_id, "Evening Closing", EVENING_TEMPLATES)
    conn.commit()
    print(f"\n✓ Done. Both checklists now have real fields from the daily ops PDF.")

    # Verification summary
    cur.execute("""
        SELECT t."Name", COUNT(f) AS field_count
        FROM "ChecklistTemplateItems" ci
        JOIN "TaskTemplates" t ON t."Id" = ci."TemplateId"
        CROSS JOIN LATERAL jsonb_array_elements(t."Fields") AS f
        WHERE ci."ChecklistId" IN (%s, %s)
        GROUP BY t."Name"
        ORDER BY t."Name"
    """, (morning_id, evening_id))
    print("\nVerification — template field counts:")
    for row in cur.fetchall():
        print(f"  {row[0]}: {row[1]} fields")

except Exception as e:
    conn.rollback()
    print(f"ERROR: {e}", file=sys.stderr)
    sys.exit(1)
finally:
    conn.close()
