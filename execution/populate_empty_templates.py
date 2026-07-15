#!/usr/bin/env python3
"""
Populates all empty template shells with real operational fields,
then resets today's task instances to Pending for employee@bajco.net.
"""

import uuid, json, psycopg2
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

def uid() -> str:
    return str(uuid.uuid4())

def f(label, ftype, required=True, range_min=None, range_max=None, corrective=None, sub_items=None):
    return {"id": uid(), "label": label, "type": ftype, "required": required,
            "rangeMin": range_min, "rangeMax": range_max,
            "correctiveActionText": corrective, "subItems": sub_items}

def sub(label, required=True):
    return {"id": uid(), "label": label, "required": required}

def explode_field(field: dict) -> dict:
    """Mirrors execution/migrate_flat_walks_to_checklists.py's explode_flat_template rule, so
    every seed/migration path produces the same scored-checklist shape:
      * Boolean field -> PassFail-scored item; correctiveActionText -> FailCorrectiveActionText
        (the item's own field set is emptied — the Pass/Fail toggle replaces the boolean).
      * anything else -> unscored item (ScoringType = null) that keeps the original field
        for data capture."""
    ftype = (field.get("type") or "").lower()
    if ftype == "boolean":
        return {
            "fields_json": "[]",
            "scoring_type": "PassFail",
            "corrective": field.get("correctiveActionText"),
        }
    return {
        "fields_json": json.dumps([field]),
        "scoring_type": None,
        "corrective": None,
    }

# ─────────────────────────────────────────────────────────────────────────────
# FIELD DEFINITIONS  — all 15 empty templates
# ─────────────────────────────────────────────────────────────────────────────

TEMPLATES = {

    # ── Afternoon Stock Rotation ───────────────────────────────────────────────

    "8d28f8fd": {   # Rotate Chilled Products
        "name": "Rotate Chilled Products",
        "fields": [
            f("All chilled products rotated FIFO — oldest stock at front",              "Boolean"),
            f("Walk-in cooler temperature (°F)",                                        "Numeric",
              range_min=33, range_max=40, corrective="Walk-in temp out of safe range — contact manager immediately"),
            f("Makeline cooler temperature (°F)",                                       "Numeric",
              range_min=33, range_max=41, corrective="Makeline temp out of safe range — pull all product and notify manager"),
            f("All products checked for expiry dates",                                  "Boolean"),
            f("Expired or near-expired products discarded and logged",                  "Boolean"),
            f("Dough pulled from walk-in as per MDOG schedule",                        "Boolean"),
            f("Cheese and toppings restocked on makeline",                              "Boolean"),
        ]
    },

    "f5eef7f8": {   # Update Price Tags
        "name": "Update Price Tags",
        "fields": [
            f("All product price labels current and clearly visible",                   "Boolean"),
            f("Promotional pricing posted correctly on menu board",                     "Boolean"),
            f("Promo end dates checked — no expired promotions displayed",             "Boolean"),
            f("Digital menu boards updated and displaying correctly",                   "Boolean"),
            f("Any price discrepancies reported to manager",                            "Boolean"),
        ]
    },

    "54ce2ca0": {   # Stockroom Tidy
        "name": "Stockroom Tidy",
        "fields": [
            f("Stockroom swept and organized",                                          "Boolean"),
            f("All items stored at least 6 inches off the floor",                      "Boolean"),
            f("Delivery items logged, dated and stored in correct location",            "Boolean"),
            f("FIFO rotation applied to all dry goods",                                "Boolean"),
            f("No open bags or unsealed containers on shelves",                         "Boolean"),
            f("Box breaker area clear and accessible",                                  "Boolean"),
            f("Stockroom door closed and secured",                                      "Boolean"),
        ]
    },

    "06f4720c": {   # Replenish Coffee Station
        "name": "Replenish Coffee Station",
        "fields": [
            f("Beverage station cleaned and sanitized",                                 "Boolean"),
            f("Drinks stock level checked and replenished",                             "Boolean"),
            f("Cups, lids, and sleeves restocked",                                      "Boolean"),
            f("Straws and napkins restocked",                                            "Boolean"),
            f("Cooler temperature confirmed (°F)",                                      "Numeric",
              range_min=33, range_max=41, corrective="Cooler out of safe temp range — remove product and notify manager"),
            f("Expiry dates on all beverage stock checked",                             "Boolean"),
        ]
    },

    # ── Midday Safety & Compliance ─────────────────────────────────────────────

    "9be7e76a": {   # Spill & Hazard Walk
        "name": "Spill & Hazard Walk",
        "fields": [
            f("Full floor walk completed — no slip or trip hazards identified",        "Boolean"),
            f("All spills cleaned immediately and area fully dried",                    "Boolean"),
            f("Wet floor signs deployed where floors are wet",                          "Boolean"),
            f("Kitchen floor dry and free of grease build-up",                         "Boolean"),
            f("All cables and cords secured and not creating trip hazards",            "Boolean"),
            f("Any hazards found and actioned — details if applicable", "Text", required=False),
        ]
    },

    "9cbc1867": {   # Fire Exit Clear
        "name": "Fire Exit Clear",
        "fields": [
            f("Front exit clear and completely unobstructed",                           "Boolean",
              corrective="Front exit blocked — clear immediately, do not open store until resolved"),
            f("Back exit clear and completely unobstructed",                            "Boolean",
              corrective="Back exit blocked — clear immediately"),
            f("Fire extinguisher accessible, sealed, and in-date",                     "Boolean",
              corrective="Fire extinguisher issue — notify manager and log with FM team"),
            f("Fire exit signage illuminated and clearly visible",                     "Boolean"),
            f("Emergency contact list posted and up to date",                          "Boolean"),
        ]
    },

    "3ce8d093": {   # Customer Facility Check
        "name": "Customer Facility Check",
        "fields": [
            f("Customer bathroom clean and fully stocked",                             "Boolean"),
            f("Soap dispenser filled and working",                                      "Boolean"),
            f("Paper towels or hand dryer working",                                     "Boolean"),
            f("Toilet operational and flushing correctly",                              "Boolean"),
            f("Customer waiting area clean, tidy and free of litter",                 "Boolean"),
            f("Seating wiped down and chairs properly positioned",                      "Boolean"),
        ]
    },

    "0ca9511a": {   # First Aid Kit Inventory
        "name": "First Aid Kit Inventory",
        "fields": [
            f("First aid kit located, accessible and unlocked",                        "Boolean"),
            f("Plasters / adhesive bandages stocked",                                   "Boolean"),
            f("Sterile gloves stocked (multiple sizes)",                                "Boolean"),
            f("Burns treatment gel or dressings stocked",                               "Boolean"),
            f("Incident log book up to date",                                           "Boolean"),
            f("Any items requiring restock — list here", "Text", required=False),
        ]
    },

    "e9d7e688": {   # Ambient Temperature Log
        "name": "Ambient Temperature Log",
        "fields": [
            f("Walk-in cooler temperature (°F)",    "Numeric",
              range_min=33, range_max=40, corrective="Walk-in cooler out of safe range — escalate immediately"),
            f("Freezer temperature (°F)",           "Numeric",
              range_min=-10, range_max=0, corrective="Freezer out of safe range — check seal and notify manager"),
            f("Makeline cooler temperature (°F)",   "Numeric",
              range_min=33, range_max=41, corrective="Makeline cooler out of safe range — pull product and notify manager"),
            f("Ambient store temperature (°F)",     "Numeric"),
            f("All temperatures within safe range", "Boolean",
              corrective="One or more temperatures out of range — document in corrective action log"),
            f("Out-of-range corrective actions taken — details", "Text", required=False),
        ]
    },

    # ── Pre-Close Manager Sign-Off ─────────────────────────────────────────────

    "5db41462": {   # Void & Refund Review
        "name": "Void & Refund Review",
        "fields": [
            f("Total voids for shift ($)",                                              "Numeric"),
            f("Total refunds for shift ($)",                                            "Numeric"),
            f("All voids reviewed and have valid documented reasons",                   "Boolean",
              corrective="Undocumented voids found — complete void log before close"),
            f("All refunds reviewed and properly approved",                             "Boolean",
              corrective="Unapproved refunds found — escalate to area manager"),
            f("Unusual voids or refunds escalated to area manager",                    "Boolean"),
            f("Manager sign-off initials",                                              "Text"),
        ]
    },

    "cbf13b46": {   # Safe Count & Drop
        "name": "Safe Count & Drop",
        "fields": [
            f("Safe counted and balanced",                                              "Boolean"),
            f("Till A counted and balanced",                                            "Boolean"),
            f("Till B counted and balanced",                                            "Boolean"),
            f("Deposit drop amount ($)",                                                "Numeric"),
            f("Drop envelope sealed and deposit logged",                                "Boolean"),
            f("Deposit drop slip completed and filed",                                  "Boolean"),
            f("Manager initials",                                                       "Text"),
        ]
    },

    "066781fa": {   # Staff Departure Log
        "name": "Staff Departure Log",
        "fields": [
            f("All staff clocked out correctly in system",                              "Boolean"),
            f("Total staff on shift",                                                   "Numeric"),
            f("Break times recorded correctly for all staff",                           "Boolean"),
            f("Any clock-out discrepancies corrected and initialled",                   "Boolean"),
            f("Any staffing issues to flag for next manager", "Text", required=False),
        ]
    },

    "1fe93a0a": {   # CCTV System Check
        "name": "CCTV System Check",
        "fields": [
            f("All cameras operational and actively recording",                        "Boolean",
              corrective="Camera fault — log fault reference and notify area manager"),
            f("No camera obstructions — all views clear",                               "Boolean"),
            f("Recording storage has sufficient space (not full)",                      "Boolean"),
            f("Any incidents during shift recorded and flagged",                       "Boolean"),
            f("CCTV notice sign posted and visible to customers",                      "Boolean"),
        ]
    },

    "f6bfdc6c": {   # Waste Record
        "name": "Waste Record",
        "fields": [
            f("Total food waste for shift (units)",                                     "Numeric"),
            f("Dough waste recorded in MDOG",                                           "Boolean"),
            f("Waste within acceptable threshold for shift",                            "Boolean",
              corrective="Waste above threshold — document root cause and notify manager"),
            f("Waste log submitted in Profit System",                                   "Boolean"),
            f("High-waste items identified — root cause noted", "Text", required=False),
        ]
    },

    "73c518f9": {   # Manager Handover Notes
        "name": "Manager Handover Notes",
        "fields": [
            f("Shift summary",                                                          "Text"),
            f("Outstanding tasks for incoming manager", "Text", required=False),
            f("Staff performance or conduct notes", "Text", required=False),
            f("Equipment issues to report", "Text", required=False),
            f("Next manager briefed verbally or via Red Book",                         "Boolean"),
        ]
    },
}

# ─────────────────────────────────────────────────────────────────────────────
# Apply updates
# ─────────────────────────────────────────────────────────────────────────────
print("Populating empty templates:\n")
total_items = 0

for id_prefix, tmpl in TEMPLATES.items():
    # Find the full UUID by prefix
    cur.execute('SELECT "Id","Category" FROM "TaskTemplates" WHERE "Id"::text LIKE %s AND "TenantId"=%s',
                (f"{id_prefix}%", TENANT_ID))
    row = cur.fetchone()
    if not row:
        print(f"  SKIP {id_prefix} — not found")
        continue
    full_id, category = str(row[0]), row[1]

    cur.execute('SELECT "ChecklistId","Order" FROM "ChecklistTemplateItems" WHERE "TemplateId"=%s', (full_id,))
    link_row = cur.fetchone()
    if not link_row:
        print(f"  SKIP {tmpl['name']} — not linked to any checklist")
        continue
    checklist_id, order_start = link_row

    # Each "shell" was a single checklist item standing in for a whole section of checks.
    # Replace it with one atomic, scored ChecklistTemplateItem per field (Boolean -> PassFail,
    # everything else -> unscored data-capture) instead of cramming every check into one
    # template's Fields array — the scored-checklist shape, mirroring
    # execution/migrate_flat_walks_to_checklists.py's validated transform rule.
    cur.execute('DELETE FROM "ChecklistTemplateItems" WHERE "ChecklistId"=%s AND "TemplateId"=%s',
                (checklist_id, full_id))

    fields = tmpl["fields"]
    for i, field in enumerate(fields):
        exploded = explode_field(field)
        label = field.get("label") or field["id"]
        new_id = uid()
        cur.execute("""
            INSERT INTO "TaskTemplates"
                ("Id","TenantId","Name","Category","Scope","Fields","IsActive","CreatedByUserId","CreatedAt")
            VALUES (%s,%s,%s,%s,'System',%s,true,'seed',%s)
        """, (new_id, TENANT_ID, label, category, exploded["fields_json"], datetime.now(timezone.utc)))
        cur.execute("""
            INSERT INTO "ChecklistTemplateItems"
                ("ChecklistId","TemplateId","Order","ScoringType","Weight","PhotoRequired","FailCorrectiveActionText")
            VALUES (%s,%s,%s,%s,1.0,false,%s) ON CONFLICT DO NOTHING
        """, (checklist_id, new_id, order_start + i, exploded["scoring_type"], exploded["corrective"]))

    print(f"  ✓ {tmpl['name']:<35} exploded into {len(fields)} scored items")
    total_items += len(fields)

print(f"\n  Total: {total_items} scored items across {len(TEMPLATES)} sections")

# ─────────────────────────────────────────────────────────────────────────────
# Reset today's task instances to Pending
# ─────────────────────────────────────────────────────────────────────────────
print("\nResetting today's task instances to Pending:\n")

today_utc = datetime.now(timezone.utc).replace(hour=0, minute=0, second=0, microsecond=0)
today_end = today_utc + timedelta(days=1)

cur.execute("""
    DELETE FROM "TaskInstances"
    WHERE "StoreId"=%s AND "TenantId"=%s AND "DueAt">=%s AND "DueAt"<%s
""", (STORE_ID, TENANT_ID, today_utc, today_end))
print(f"  Cleared {cur.rowcount} stale instance(s)")

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
conn.close()

print("\n✓ Done — log in as employee@bajco.net at http://localhost:4201")
