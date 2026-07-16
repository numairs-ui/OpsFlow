#!/bin/bash

INPUT=$(cat)
# jq is not reliably present on Windows dev machines for this repo; python is (used by
# execution/*.py scripts already), so parse the hook payload with it instead.
COMMAND=$(echo "$INPUT" | python -c "import sys, json
try:
    print(json.load(sys.stdin).get('tool_input', {}).get('command', ''))
except Exception:
    print('')")

# Customized from the Matt Pocock "git-guardrails-claude-code" skill default: blocks
# destructive/history-rewriting git commands, but NOT plain `git push` — this repo's
# incident (2026-07-16) was an unstashed `git reset --hard` wiping uncommitted WIP, not a
# routine push. Blocking every push would add friction to normal, already-approved work.
DANGEROUS_PATTERNS=(
  "git reset --hard"
  "reset --hard"
  "git clean -fd"
  "git clean -f"
  "git checkout \."
  "git restore \."
  "git branch -D"
  "push --force"
  "push -f"
)

for pattern in "${DANGEROUS_PATTERNS[@]}"; do
  if echo "$COMMAND" | grep -qE "$pattern"; then
    echo "BLOCKED: '$COMMAND' matches dangerous pattern '$pattern'. The user has prevented you from doing this. If you believe this is needed, stop and ask the user to run it themselves or explicitly re-approve." >&2
    exit 2
  fi
done

exit 0
