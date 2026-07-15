#!/usr/bin/env python3
"""
One-time migration (OpsFlow A6): explode already-seeded flat "walk"-style TaskTemplates into
real multi-item scored Checklists — the same shape the fixed template-import path now produces
(one Checklist + one TaskTemplate per item + a scored ChecklistTemplateItem row).

A flat walk template is a single TaskTemplate whose Fields are a list of per-area checks. Each
field becomes one checklist item:
  * Boolean field  -> PassFail-scored item; its correctiveActionText becomes FailCorrectiveActionText
                      (the item's own field set is emptied — the Pass/Fail toggle replaces the boolean).
  * other field    -> unscored item (ScoringType = null) that keeps the original field for data capture.

Safety:
  * Dry-run by DEFAULT. Pass --apply to write. Never run --apply against production data you don't
    own; point --database-url at a disposable/test database.
  * --self-test runs the pure transform on built-in sample data (no DB) and asserts the output shape.

Usage:
    python3 execution/migrate_flat_walks_to_checklists.py --self-test
    python3 execution/migrate_flat_walks_to_checklists.py --tenant bajco-dev            # dry run
    python3 execution/migrate_flat_walks_to_checklists.py --tenant bajco-dev --apply \\
        --database-url "Host=...;Database=...;Username=...;Password=...;Port=5432"
"""
from __future__ import annotations

import argparse
import json
import sys
import uuid
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

# Name substrings that identify a flat "walk"-style template worth exploding.
WALK_NAME_HINTS = ("walk", "check", "inspection", "audit")


def _ts() -> str:
    return datetime.now(timezone.utc).isoformat()


def explode_flat_template(name: str, fields: list[dict[str, Any]]) -> dict[str, Any]:
    """
    Pure transform: flat template (name + fields) -> checklist spec (name + scored items).
    No DB access — this is the unit under test.
    """
    items: list[dict[str, Any]] = []
    for order, field in enumerate(fields):
        ftype = (field.get("type") or "").lower()
        label = field.get("label") or f"Item {order + 1}"
        corrective = field.get("correctiveActionText")

        if ftype == "boolean":
            items.append({
                "name": label,
                "order": order,
                "fieldsJson": "[]",           # Pass/Fail toggle replaces the boolean field
                "scoringType": "PassFail",
                "weight": 1.0,
                "photoRequired": False,
                "failCorrectiveActionText": corrective,
                "failScoreThreshold": None,
            })
        else:
            # Keep the original field so numeric/text captures still work; leave it unscored.
            items.append({
                "name": label,
                "order": order,
                "fieldsJson": json.dumps([field]),
                "scoringType": None,
                "weight": 1.0,
                "photoRequired": False,
                "failCorrectiveActionText": None,
                "failScoreThreshold": None,
            })

    return {"name": name, "items": items}


def _run_self_test() -> int:
    sample_fields = [
        {"id": "f1", "type": "Boolean", "label": "Floor walk completed", "correctiveActionText": None},
        {"id": "f2", "type": "Boolean", "label": "Spills cleaned", "correctiveActionText": "Re-clean and dry the area"},
        {"id": "f3", "type": "Text", "label": "Hazards found — details", "required": False},
    ]
    spec = explode_flat_template("Spill & Hazard Walk", sample_fields)

    assert spec["name"] == "Spill & Hazard Walk"
    assert len(spec["items"]) == 3
    assert spec["items"][0]["scoringType"] == "PassFail"
    assert spec["items"][0]["fieldsJson"] == "[]"
    assert spec["items"][1]["failCorrectiveActionText"] == "Re-clean and dry the area"
    assert spec["items"][2]["scoringType"] is None          # Text stays unscored
    assert json.loads(spec["items"][2]["fieldsJson"])[0]["id"] == "f3"
    # order is preserved and contiguous
    assert [i["order"] for i in spec["items"]] == [0, 1, 2]

    print(json.dumps({"self_test": "passed", "sample": spec}, indent=2))
    return 0


def _load_env_conn_string(key: str) -> str | None:
    env_path = Path(__file__).parent.parent / ".env"
    if not env_path.exists():
        return None
    for line in env_path.read_text().splitlines():
        line = line.strip()
        if line and not line.startswith("#") and "=" in line:
            k, v = line.split("=", 1)
            if k.strip() == key:
                return v.strip()
    return None


def _parse_conn(cs: str) -> dict[str, str]:
    return {k.strip(): v.strip() for k, v in (p.split("=", 1) for p in cs.split(";") if "=" in p)}


def _find_flat_walks(cur, tenant_id: str) -> list[tuple[str, str, list[dict]]]:
    """Return (template_id, name, fields) for flat walk templates not already exploded into a checklist."""
    like = " OR ".join(["LOWER(\"Name\") LIKE %s" for _ in WALK_NAME_HINTS])
    params: list[Any] = [tenant_id] + [f"%{h}%" for h in WALK_NAME_HINTS]
    cur.execute(
        f'SELECT "Id", "Name", "Fields" FROM "TaskTemplates" '
        f'WHERE "TenantId" = %s AND ({like})',
        params,
    )
    rows = cur.fetchall()

    out: list[tuple[str, str, list[dict]]] = []
    for tid, name, fields in rows:
        # Skip if a checklist with the same name already exists for this tenant (idempotent re-run).
        cur.execute(
            'SELECT COUNT(*) FROM "Checklists" WHERE "TenantId" = %s AND "Name" = %s',
            (tenant_id, name),
        )
        if cur.fetchone()[0] > 0:
            continue
        parsed = fields if isinstance(fields, list) else json.loads(fields or "[]")
        if parsed:
            out.append((tid, name, parsed))
    return out


def _find_or_create_template(cur, tenant_id: str, created_by: str, name: str, fields_json: str) -> str:
    """Reuse an existing TaskTemplate with the same (TenantId, Name, Scope='System') rather than
    always inserting — two exploded items (from the same or different flat templates) can share
    a label (e.g. a common step name), and TaskTemplates has a real unique constraint on that
    triple. Discovered via a real --apply run hitting IX_TaskTemplates_TenantId_Name_Scope."""
    cur.execute(
        'SELECT "Id" FROM "TaskTemplates" WHERE "TenantId" = %s AND "Name" = %s AND "Scope" = \'System\'',
        (tenant_id, name),
    )
    row = cur.fetchone()
    if row:
        return row[0]

    template_id = str(uuid.uuid4())
    cur.execute(
        'INSERT INTO "TaskTemplates" ("Id","TenantId","Name","Category","Scope","Fields","IsActive","CreatedByUserId","CreatedAt") '
        'VALUES (%s,%s,%s,%s,%s,%s::jsonb,%s,%s,%s)',
        (template_id, tenant_id, name, "Walk", "System", fields_json, True, created_by, _ts()),
    )
    return template_id


def _apply(cur, tenant_id: str, created_by: str, specs: list[dict[str, Any]]) -> int:
    count = 0
    for spec in specs:
        checklist_id = str(uuid.uuid4())
        cur.execute(
            'INSERT INTO "Checklists" ("Id","TenantId","Name","Scope","IsActive","CreatedByUserId","CreatedAt") '
            'VALUES (%s,%s,%s,%s,%s,%s,%s)',
            (checklist_id, tenant_id, spec["name"], "System", True, created_by, _ts()),
        )
        for item in spec["items"]:
            template_id = _find_or_create_template(cur, tenant_id, created_by, item["name"], item["fieldsJson"])
            cur.execute(
                'INSERT INTO "ChecklistTemplateItems" '
                '("ChecklistId","TemplateId","Order","ScoringType","Weight","PhotoRequired","FailCorrectiveActionText","FailScoreThreshold") '
                'VALUES (%s,%s,%s,%s,%s,%s,%s,%s) ON CONFLICT DO NOTHING',
                (checklist_id, template_id, item["order"], item["scoringType"], item["weight"],
                 item["photoRequired"], item["failCorrectiveActionText"], item["failScoreThreshold"]),
            )
        count += 1
    return count


def main() -> int:
    parser = argparse.ArgumentParser(description="Explode flat walk templates into scored checklists.")
    parser.add_argument("--self-test", action="store_true", help="Run the pure transform on sample data and exit.")
    parser.add_argument("--tenant", default="bajco-dev", help="Tenant id to migrate.")
    parser.add_argument("--created-by", default="system", help="CreatedByUserId for new rows.")
    parser.add_argument("--apply", action="store_true", help="Actually write to the DB (default: dry run).")
    parser.add_argument("--database-url", default=None,
                        help="TENANT_DB_CONNECTION_STRING override (semicolon key=value). Defaults to .env.")
    args = parser.parse_args()

    if args.self_test:
        return _run_self_test()

    try:
        import psycopg2  # imported lazily so --self-test needs no DB driver
    except ImportError:
        print(json.dumps({"error": "psycopg2 not installed; use --self-test for the DB-free check."}), file=sys.stderr)
        return 2

    cs = args.database_url or _load_env_conn_string("TENANT_DB_CONNECTION_STRING")
    if not cs:
        print(json.dumps({"error": "No connection string (pass --database-url or set TENANT_DB_CONNECTION_STRING)."}), file=sys.stderr)
        return 2

    parts = _parse_conn(cs)
    conn = psycopg2.connect(
        host=parts["Host"], dbname=parts["Database"], user=parts["Username"],
        password=parts["Password"], port=int(parts.get("Port", "5432")), sslmode="require",
    )
    conn.autocommit = False
    try:
        cur = conn.cursor()
        flats = _find_flat_walks(cur, args.tenant)
        specs = [explode_flat_template(name, fields) for _, name, fields in flats]

        summary = {
            "tenant": args.tenant,
            "flat_walk_templates_found": len(flats),
            "checklists_to_create": [{"name": s["name"], "items": len(s["items"])} for s in specs],
            "mode": "apply" if args.apply else "dry-run",
        }

        if not args.apply:
            summary["note"] = "Dry run — nothing written. Re-run with --apply against a test database to migrate."
            print(json.dumps(summary, indent=2))
            return 0

        created = _apply(cur, args.tenant, args.created_by, specs)
        conn.commit()
        summary["checklists_created"] = created
        print(json.dumps(summary, indent=2))
        return 0
    except Exception as exc:  # noqa: BLE001 — surface a structured error to the orchestrator
        conn.rollback()
        print(json.dumps({"error": str(exc)}), file=sys.stderr)
        return 1
    finally:
        conn.close()


if __name__ == "__main__":
    sys.exit(main())
