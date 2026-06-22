#!/usr/bin/env python3
"""Start OpsFlow.Api with .env vars loaded correctly (handles semicolons in connection strings)."""
import os
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).parent.parent
ENV_FILE = ROOT / ".env"
API_PROJECT = ROOT / "backend" / "OpsFlow.Api"
DOTNET = Path.home() / ".dotnet" / "dotnet"

env = {**os.environ}
if ENV_FILE.exists():
    for line in ENV_FILE.read_text().splitlines():
        line = line.strip()
        if not line or line.startswith("#"):
            continue
        if "=" in line:
            k, v = line.split("=", 1)
            env[k.strip()] = v.strip()

result = subprocess.run(
    [str(DOTNET), "run", "--project", str(API_PROJECT), "--launch-profile", "http"],
    env=env,
)
sys.exit(result.returncode)
