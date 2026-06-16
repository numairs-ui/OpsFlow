# Bajco Group — Template Onboarding SOP

**Status:** Active  
**PRD ref:** FD-17 (TB-74), FD-18 (TB-73)  
**Scope:** Converting Bajco's 19 operational PDFs into OpsFlow System-scope templates

---

## Overview

Bajco Group's Papa Johns operational procedures exist as PDF documents. This SOP converts them into OpsFlow template import JSON using the Layer 3 script `execution/pdf_to_template_json.py`, then imports them via the Admin Panel.

The output feeds directly into `POST /admin/templates/import`.

---

## Prerequisites

| Item | Detail |
|------|--------|
| Python | 3.10+ |
| Dependencies | `pip3 install pdfplumber anthropic` |
| API Key | `ANTHROPIC_API_KEY` set in `.env` |
| OpsFlow running | Dev stack up (`supabase start`); Admin Panel accessible |

---

## Step 1 — Convert a Single PDF

```bash
cd /path/to/OpsFlow

# Activate .env
source .env

# Convert (output to stdout)
python3 execution/pdf_to_template_json.py \
  --pdf /path/to/bajco/procedures/opening_checklist.pdf \
  --scope System \
  --type Checklist

# Convert with output to file
python3 execution/pdf_to_template_json.py \
  --pdf /path/to/bajco/procedures/food_safety_log.pdf \
  --scope System \
  --output .tmp/food_safety_log.json

# Dry run — inspect extracted PDF text before sending to Claude
python3 execution/pdf_to_template_json.py \
  --pdf /path/to/bajco/procedures/mdog.pdf \
  --dry-run
```

### CLI flags

| Flag | Default | Description |
|------|---------|-------------|
| `--pdf` | required | Path to source PDF |
| `--scope` | `System` | Template scope: `System \| Regional \| Store` |
| `--type` | `auto` | Hint: `Task \| Checklist \| Form \| auto` |
| `--output` | stdout | Write JSON to file instead |
| `--model` | `claude-sonnet-4-6` | Claude model ID |
| `--pages` | all | Comma-separated page numbers, e.g. `1,2,5` |
| `--dry-run` | false | Print extracted PDF text only |

---

## Step 2 — Review the JSON

Open the output file and verify:

- [ ] Template names are clear and operational (no junk headings from PDF headers/footers)
- [ ] Field types are correct (temperatures → Numeric with rangeMax; yes/no → Boolean)
- [ ] `correctiveActionText` is present on fields where the PDF specifies failure actions
- [ ] `scope` is `System` for all Bajco templates
- [ ] Form templates have `propagationType` and `approvalSteps` that match the actual approval chain

**Manual edits are expected** — the script produces a solid first draft but PDF formatting artifacts may require cleanup. Edit the JSON file directly.

---

## Step 3 — Batch Multiple PDFs

Combine multiple JSON outputs into a single import payload:

```python
# .tmp/merge_templates.py
import json, glob, sys

templates = []
for f in sorted(glob.glob(".tmp/*.json")):
    with open(f) as fh:
        data = json.load(fh)
        templates.extend(data.get("templates", []))

print(json.dumps({"templates": templates}, indent=2))
```

```bash
python3 .tmp/merge_templates.py > .tmp/bajco_full_import.json
```

---

## Step 4 — Import via Admin Panel

1. Log in to OpsFlow dashboard as an `admin` user
2. Navigate to **Admin Panel → Template Import**
3. Either upload `.tmp/bajco_full_import.json` or paste its contents
4. Review the **import preview** (counts by type)
5. Click **Confirm Import**
6. Review the response — `{ "created": N, "failed": [...] }` — fix any failures and re-import those entries only

---

## Step 5 — Programmatic Import (alternative)

```bash
curl -X POST https://api.opsflow.local/admin/templates/import \
  -H "Authorization: Bearer <admin-jwt>" \
  -H "Content-Type: application/json" \
  -d @.tmp/bajco_full_import.json
```

---

## Known Edge Cases

| Situation | Resolution |
|-----------|------------|
| PDF is image-only (scanned) | Script exits with error. Pre-process with OCR (e.g. `ocrmypdf input.pdf output.pdf`) before running |
| Multi-column layout confuses extraction | Use `--pages` to extract only relevant pages; or use `--dry-run` to inspect and manually paste clean text |
| PDF has corporate header/footer on every page | Claude filters most boilerplate, but review `name` fields for any junk content |
| Temperature field missing range values | Set `rangeMax` manually (Papa Johns standard: walk-in max 40°F, dough max 56°F) |
| Form templates need Bajco-specific approval chain | Set `approvalSteps` to `[{ "role": "store_manager", "order": 1 }, { "role": "supervisor", "order": 2 }]` unless the PDF specifies differently |

---

## Bajco PDF Inventory

| # | PDF Name | Expected Type | Status |
|---|----------|---------------|--------|
| 1 | Opening Checklist | Checklist | Pending |
| 2 | Closing Checklist | Checklist | Pending |
| 3 | MDOG Daily Inventory | Task | Pending |
| 4 | Food Safety Temperature Log | Task | Pending |
| 5 | Shift Handover Sign-Off | Form (Sequential) | Pending |
| 6 | Manager Walk Audit | Task | Pending |
| 7 | Till Count Procedure | Task | Pending |
| 8 | Bank Deposit Log | Form (Sequential) | Pending |
| 9 | Equipment Cleaning Schedule | Checklist | Pending |
| 10 | Delivery Receiving Checklist | Checklist | Pending |
| 11 | New Employee Orientation | Checklist | Pending |
| 12 | Monthly Safety Inspection | Checklist | Pending |
| 13 | Incident Report Form | Form (Sequential) | Pending |
| 14 | Corrective Action Form | Form (Sequential) | Pending |
| 15 | Store Audit Checklist | Checklist | Pending |
| 16 | Waste Log | Task | Pending |
| 17 | Catering Order Checklist | Checklist | Pending |
| 18 | Refrigeration Temperature Log | Task | Pending |
| 19 | End-of-Day Financial Summary | Form (Sequential) | Pending |

> Update the **Status** column as each PDF is processed and imported.

---

## Self-Annealing Notes

_Add any new edge cases discovered during actual Bajco onboarding here so future agents don't repeat the same investigation._

- _(none yet — add as discovered)_
