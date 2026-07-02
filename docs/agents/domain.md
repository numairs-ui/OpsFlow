# Domain Docs

How the engineering skills should consume OpsFlow's domain documentation when exploring the codebase. OpsFlow is **single-context**: one `CONTEXT.md` + `docs/adr/` at the repo root cover the whole product (backend + frontend share the domain language).

## Before exploring, read these

- **`CONTEXT.md`** at the repo root, and
- **`docs/adr/`** — read ADRs that touch the area you're about to work in.

If any of these files don't exist, **proceed silently**. Don't flag their absence; don't suggest creating them upfront. The `/domain-modeling` skill (reached via `/grill-with-docs` and `/improve-codebase-architecture`) creates them lazily when terms or decisions actually get resolved.

## File structure (single-context)

```
/
├── CONTEXT.md
├── docs/adr/
│   ├── 0001-multi-region-role-model.md
│   └── 0002-....md
├── backend/
└── frontend/
```

## Use the glossary's vocabulary

When your output names a domain concept (in an issue title, a refactor proposal, a hypothesis, a test name), use the term as defined in `CONTEXT.md` (e.g. Deposit Log, MDOG, Corrective Action, Recurring Assignment, Store Kiosk, scopes System/Regional/Store). Don't drift to synonyms the glossary explicitly avoids.

If the concept you need isn't in the glossary yet, that's a signal — either you're inventing language the project doesn't use (reconsider) or there's a real gap (note it for `/domain-modeling`).

## Flag ADR conflicts

If your output contradicts an existing ADR, surface it explicitly rather than silently overriding:

> _Contradicts ADR-0001 (multi-region role model) — but worth reopening because…_
