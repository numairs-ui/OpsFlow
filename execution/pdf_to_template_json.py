#!/usr/bin/env python3
"""
Layer 3 execution script: Convert a PDF operational procedure into OpsFlow
template import JSON (POST /admin/templates/import schema).

Usage:
    python3 pdf_to_template_json.py --pdf <path> [options]

Options:
    --pdf       Path to the PDF file (required)
    --scope     Template scope: System | Regional | Store  (default: System)
    --type      Hint template type: Task | Checklist | Form | auto  (default: auto)
    --output    Write JSON to this file instead of stdout
    --model     Claude model ID (default: claude-sonnet-4-6)
    --pages     Comma-separated page numbers to extract, e.g. 1,2,5  (default: all)
    --dry-run   Print extracted PDF text only; do not call Claude

Environment:
    ANTHROPIC_API_KEY  Required — Claude API key
"""
from __future__ import annotations

import argparse
import json
import os
import sys
import textwrap
from pathlib import Path
from typing import Any

import pdfplumber
import anthropic


# ---------------------------------------------------------------------------
# Schema reference injected into the Claude prompt
# ---------------------------------------------------------------------------
SCHEMA_REFERENCE = """
## OpsFlow Template Import Schema

POST /admin/templates/import accepts:
{
  "templates": [<TemplateDto>, ...]
}

Each TemplateDto is one of:

### TaskTemplateDto
{
  "type": "Task",
  "name": "string (required)",
  "description": "string (optional)",
  "category": "string — e.g. Operations | Inventory | Safety | Food Safety | Safe | Cleaning | Opening | Closing",
  "scope": "System | Regional | Store",
  "fields": [<TemplateField>, ...]
}

### ChecklistTemplateDto
{
  "type": "Checklist",
  "name": "string (required)",
  "description": "string (optional)",
  "scope": "System | Regional | Store",
  "items": [<TaskTemplateDto>, ...]   // each item is an inline task template
}

### FormTemplateDto
{
  "type": "Form",
  "name": "string (required)",
  "description": "string (optional)",
  "scope": "System | Regional | Store",
  "propagationType": "Sequential | Parallel | NotificationOnly",
  "approvalSteps": [
    { "role": "store_employee | store_manager | supervisor | admin", "order": <integer> }
  ],
  "fields": [<TemplateField>, ...]
}

### TemplateField
{
  "type": "Numeric | Boolean | Text | Photo | Checklist",
  "label": "string (required)",
  "required": true | false,

  // Numeric only:
  "rangeMin": <number | null>,
  "rangeMax": <number | null>,
  "correctiveActionText": "string | null",

  // Boolean only:
  "correctiveActionText": "string | null",   // fires when value is false/No

  // Checklist type only:
  "subItems": [
    { "label": "string", "required": true | false }
  ]
}

Decision guide:
- Task        → a single work item with data-capture fields (numeric readings, yes/no checks, photos, text entries)
- Checklist   → an ordered collection of Tasks executed together (e.g. Opening Procedure = multiple task items)
- Form        → a document requiring review/approval routing (e.g. incident reports, shift handover sign-offs)
  - Sequential  → reviewers approve in order (step 1 must approve before step 2 sees it)
  - Parallel    → all reviewers notified simultaneously; first action wins
  - NotificationOnly → no approval required; submission is recorded and all reviewers notified
"""

# ---------------------------------------------------------------------------
# Prompt builder
# ---------------------------------------------------------------------------

def build_prompt(pdf_text: str, scope: str, type_hint: str) -> str:
    type_instruction = (
        f"The operator has hinted this PDF is a **{type_hint}** template."
        if type_hint != "auto"
        else "Auto-detect the best template type (Task, Checklist, or Form) from the content."
    )

    return textwrap.dedent(f"""
        You are an expert at converting restaurant franchise operational procedures into
        structured digital templates for OpsFlow, a compliance management platform.

        {type_instruction}
        Use scope = "{scope}" for all templates you produce.

        {SCHEMA_REFERENCE}

        ## Instructions
        1. Read the PDF content carefully.
        2. Identify all distinct procedures, checklists, or forms present.
        3. For each one, produce a valid TemplateDto object.
        4. Map every measurable step to a TemplateField with the tightest type that fits:
           - Temperature readings, counts, dollar amounts → Numeric (set rangeMin/rangeMax where the document specifies limits)
           - Yes/No compliance checks → Boolean (set correctiveActionText if the document specifies what to do on failure)
           - Signatures, notes, names → Text
           - Photo evidence requirements → Photo
           - Ordered sub-checklists within a single task → Checklist field type
        5. Preserve all corrective action language verbatim from the source document.
        6. If the document is a multi-step operational procedure (e.g. opening checklist),
           produce a **Checklist** template whose items are the individual task steps.
        7. If the document requires sign-off or managerial review, produce a **Form** template
           with appropriate propagationType and approvalSteps.
        8. Output ONLY a valid JSON object in this exact shape — no markdown fences, no commentary:
           {{"templates": [...]}}

        ## PDF Content
        ---
        {pdf_text}
        ---

        Respond with ONLY the JSON object. No preamble, no explanation, no markdown.
    """).strip()


# ---------------------------------------------------------------------------
# PDF extraction
# ---------------------------------------------------------------------------

def extract_pdf_text(pdf_path: Path, pages: list[int] | None) -> str:
    all_text: list[str] = []
    with pdfplumber.open(pdf_path) as pdf:
        target_pages = (
            [pdf.pages[i - 1] for i in pages if 0 < i <= len(pdf.pages)]
            if pages
            else pdf.pages
        )
        for page in target_pages:
            text = page.extract_text(x_tolerance=2, y_tolerance=3) or ""
            tables = page.extract_tables()
            if tables:
                for table in tables:
                    for row in table:
                        cleaned = [cell or "" for cell in row]
                        text += "\n" + " | ".join(cleaned)
            all_text.append(text)
    return "\n\n--- PAGE BREAK ---\n\n".join(all_text).strip()


# ---------------------------------------------------------------------------
# Claude call
# ---------------------------------------------------------------------------

def call_claude(prompt: str, model: str) -> str:
    api_key = os.getenv("ANTHROPIC_API_KEY")
    if not api_key:
        print(
            json.dumps({"error": "ANTHROPIC_API_KEY environment variable is not set"}),
            file=sys.stderr,
        )
        sys.exit(1)

    client = anthropic.Anthropic(api_key=api_key)
    message = client.messages.create(
        model=model,
        max_tokens=8192,
        messages=[{"role": "user", "content": prompt}],
    )
    return message.content[0].text


# ---------------------------------------------------------------------------
# Validation
# ---------------------------------------------------------------------------

VALID_FIELD_TYPES = {"Numeric", "Boolean", "Text", "Photo", "Checklist"}
VALID_SCOPES = {"System", "Regional", "Store"}
VALID_PROPAGATION = {"Sequential", "Parallel", "NotificationOnly"}
VALID_ROLES = {"store_employee", "store_manager", "supervisor", "admin"}
VALID_TYPES = {"Task", "Checklist", "Form"}


def validate_field(field: Any, path: str) -> list[str]:
    errors: list[str] = []
    if not isinstance(field, dict):
        return [f"{path}: must be an object"]
    if field.get("type") not in VALID_FIELD_TYPES:
        errors.append(f"{path}.type: must be one of {sorted(VALID_FIELD_TYPES)}, got {field.get('type')!r}")
    if not field.get("label"):
        errors.append(f"{path}.label: required")
    if not isinstance(field.get("required"), bool):
        errors.append(f"{path}.required: must be a boolean")
    return errors


def validate_template(tmpl: Any, idx: int) -> list[str]:
    errors: list[str] = []
    path = f"templates[{idx}]"
    if not isinstance(tmpl, dict):
        return [f"{path}: must be an object"]

    t = tmpl.get("type")
    if t not in VALID_TYPES:
        errors.append(f"{path}.type: must be one of {sorted(VALID_TYPES)}, got {t!r}")
    if not tmpl.get("name"):
        errors.append(f"{path}.name: required")
    if tmpl.get("scope") not in VALID_SCOPES:
        errors.append(f"{path}.scope: must be one of {sorted(VALID_SCOPES)}")

    if t == "Task":
        for fi, field in enumerate(tmpl.get("fields", [])):
            errors.extend(validate_field(field, f"{path}.fields[{fi}]"))

    elif t == "Checklist":
        for ii, item in enumerate(tmpl.get("items", [])):
            errors.extend(validate_template(item, -1))  # nested
            if item.get("type") != "Task":
                errors.append(f"{path}.items[{ii}].type: checklist items must be Task")

    elif t == "Form":
        if tmpl.get("propagationType") not in VALID_PROPAGATION:
            errors.append(f"{path}.propagationType: must be one of {sorted(VALID_PROPAGATION)}")
        steps = tmpl.get("approvalSteps", [])
        if not steps:
            errors.append(f"{path}.approvalSteps: must be non-empty")
        for si, step in enumerate(steps):
            if step.get("role") not in VALID_ROLES:
                errors.append(f"{path}.approvalSteps[{si}].role: must be one of {sorted(VALID_ROLES)}")
            if not isinstance(step.get("order"), int):
                errors.append(f"{path}.approvalSteps[{si}].order: must be an integer")
        for fi, field in enumerate(tmpl.get("fields", [])):
            errors.extend(validate_field(field, f"{path}.fields[{fi}]"))

    return errors


def validate_output(data: Any) -> list[str]:
    if not isinstance(data, dict):
        return ["root: must be a JSON object"]
    if "templates" not in data:
        return ["root: missing required key 'templates'"]
    if not isinstance(data["templates"], list):
        return ["templates: must be an array"]
    errors: list[str] = []
    for i, tmpl in enumerate(data["templates"]):
        errors.extend(validate_template(tmpl, i))
    return errors


# ---------------------------------------------------------------------------
# JSON extraction (handles stray markdown fences from the model)
# ---------------------------------------------------------------------------

def extract_json(raw: str) -> dict:
    text = raw.strip()
    if text.startswith("```"):
        lines = text.splitlines()
        text = "\n".join(lines[1:-1] if lines[-1].strip() == "```" else lines[1:])
    return json.loads(text)


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------

def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Convert a PDF operational procedure to OpsFlow template import JSON.",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=__doc__,
    )
    parser.add_argument("--pdf", required=True, help="Path to the source PDF")
    parser.add_argument("--scope", default="System", choices=["System", "Regional", "Store"])
    parser.add_argument("--type", default="auto", choices=["Task", "Checklist", "Form", "auto"],
                        dest="type_hint")
    parser.add_argument("--output", help="Write JSON to this file (default: stdout)")
    parser.add_argument("--model", default="claude-sonnet-4-6", help="Claude model ID")
    parser.add_argument("--pages", help="Comma-separated page numbers to extract, e.g. 1,2,5")
    parser.add_argument("--dry-run", action="store_true",
                        help="Print extracted PDF text only; do not call Claude")
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    pdf_path = Path(args.pdf).resolve()

    if not pdf_path.exists():
        print(json.dumps({"error": f"PDF not found: {pdf_path}"}), file=sys.stderr)
        sys.exit(1)
    if pdf_path.suffix.lower() != ".pdf":
        print(json.dumps({"error": f"File is not a PDF: {pdf_path}"}), file=sys.stderr)
        sys.exit(1)

    pages: list[int] | None = None
    if args.pages:
        try:
            pages = [int(p.strip()) for p in args.pages.split(",")]
        except ValueError:
            print(json.dumps({"error": "--pages must be comma-separated integers"}), file=sys.stderr)
            sys.exit(1)

    # 1. Extract PDF text
    print(f"Extracting text from: {pdf_path.name}", file=sys.stderr)
    try:
        pdf_text = extract_pdf_text(pdf_path, pages)
    except Exception as exc:
        print(json.dumps({"error": f"PDF extraction failed: {exc}"}), file=sys.stderr)
        sys.exit(1)

    if not pdf_text.strip():
        print(json.dumps({"error": "No text extracted — PDF may be image-only or encrypted"}),
              file=sys.stderr)
        sys.exit(1)

    print(f"Extracted {len(pdf_text)} characters across {len(pages or [])} pages.", file=sys.stderr)

    if args.dry_run:
        print(pdf_text)
        return

    # 2. Build prompt and call Claude
    prompt = build_prompt(pdf_text, args.scope, args.type_hint)
    print(f"Calling Claude ({args.model})…", file=sys.stderr)

    try:
        raw_response = call_claude(prompt, args.model)
    except anthropic.APIError as exc:
        print(json.dumps({"error": f"Claude API error: {exc}"}), file=sys.stderr)
        sys.exit(1)

    # 3. Parse JSON
    try:
        data = extract_json(raw_response)
    except json.JSONDecodeError as exc:
        print(json.dumps({"error": f"Model returned invalid JSON: {exc}",
                          "raw": raw_response[:500]}), file=sys.stderr)
        sys.exit(1)

    # 4. Validate
    errors = validate_output(data)
    if errors:
        print(json.dumps({"warning": "Validation issues found", "issues": errors}), file=sys.stderr)

    # 5. Output
    output_json = json.dumps(data, indent=2, ensure_ascii=False)
    if args.output:
        out_path = Path(args.output)
        out_path.write_text(output_json, encoding="utf-8")
        print(f"Written to: {out_path}", file=sys.stderr)
    else:
        print(output_json)

    template_count = len(data.get("templates", []))
    print(f"Done. {template_count} template(s) produced.", file=sys.stderr)


if __name__ == "__main__":
    main()
