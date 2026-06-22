#!/usr/bin/env python3
import psycopg2
from pathlib import Path

ROOT = Path(__file__).parent.parent
env = {}
for line in (ROOT / ".env").read_text().splitlines():
    line = line.strip()
    if line and not line.startswith("#") and "=" in line:
        k, v = line.split("=", 1)
        env[k.strip()] = v.strip()

cs = env["TENANT_DB_CONNECTION_STRING"]
parts = {k.strip(): v.strip() for k, v in (p.split("=", 1) for p in cs.split(";") if "=" in p)}
conn = psycopg2.connect(host=parts["Host"], dbname=parts["Database"], user=parts["Username"],
    password=parts["Password"], port=int(parts["Port"]), sslmode="require")
cur = conn.cursor()

for table in ["StoreSettings","FormTemplates"]:
    cur.execute("""
        SELECT column_name, data_type, is_nullable
        FROM information_schema.columns
        WHERE table_name = %s AND table_schema = 'public'
        ORDER BY ordinal_position
    """, (table,))
    rows = cur.fetchall()
    print(f"\n── {table} ──")
    for col, dtype, nullable in rows:
        print(f"  {col:<35} {dtype:<20} nullable={nullable}")
conn.close()
