#!/usr/bin/env python3
"""Check current database state."""
import psycopg2
from pathlib import Path

ROOT = Path(__file__).parent.parent
env = {}
for line in (ROOT / ".env").read_text().splitlines():
    line = line.strip()
    if line and not line.startswith("#") and "=" in line:
        k, v = line.split("=", 1)
        env[k.strip()] = v.strip()

cs = env["MASTER_DB_CONNECTION_STRING"]
parts = {k.strip(): v.strip() for k, v in (p.split("=", 1) for p in cs.split(";") if "=" in p)}
conn = psycopg2.connect(
    host=parts["Host"], dbname=parts["Database"], user=parts["Username"],
    password=parts["Password"], port=int(parts["Port"]), sslmode="require"
)
cur = conn.cursor()
cur.execute("SELECT table_name FROM information_schema.tables WHERE table_schema='public' ORDER BY table_name")
print("Tables:", [r[0] for r in cur.fetchall()])
cur.execute('SELECT "Id", "Name", "IsActive" FROM "Tenants" LIMIT 10')
print("Tenants:", cur.fetchall())
cur.execute('SELECT COUNT(*) FROM "UserProfiles"')
print("UserProfiles count:", cur.fetchone())
cur.execute('SELECT COUNT(*) FROM "Stores"')
print("Stores count:", cur.fetchone())
conn.close()
