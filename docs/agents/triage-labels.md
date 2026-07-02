# Triage Labels

The skills speak in terms of five canonical triage roles. This file maps those roles to the actual label strings used in OpsFlow's GitHub issue tracker. OpsFlow uses the canonical names verbatim.

| Canonical role    | Label in our tracker | Meaning                                  |
| ----------------- | -------------------- | ---------------------------------------- |
| `needs-triage`    | `needs-triage`       | Maintainer needs to evaluate this issue  |
| `needs-info`      | `needs-info`         | Waiting on reporter for more information |
| `ready-for-agent` | `ready-for-agent`    | Fully specified, ready for an AFK agent  |
| `ready-for-human` | `ready-for-human`    | Requires human implementation            |
| `wontfix`         | `wontfix`            | Will not be actioned                     |

When a skill mentions a role (e.g. "apply the AFK-ready triage label"), use the corresponding label string from this table.

These labels don't exist on the GitHub repo yet — create them on first use, e.g.:

```bash
gh label create ready-for-agent --description "Fully specified, ready for an AFK agent" --color 0E8A16
```

Edit the right-hand column to match whatever vocabulary you actually use.
