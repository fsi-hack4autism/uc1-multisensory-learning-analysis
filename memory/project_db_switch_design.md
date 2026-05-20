---
name: project-db-switch-design
description: Design decision for SQLite→PostgreSQL one-line switch; why sqlalchemy.Uuid is used instead of postgresql.UUID
metadata:
  type: project
---

SQLite is used as the local dev database; PostgreSQL is the target for the demo.

Switching is a single line in `api/.env`: comment out `sqlite+aiosqlite:///./sessions.db` and uncomment the `postgresql+asyncpg://...` line. No migration scripts needed — SQLAlchemy's `create_all()` creates the schema on first run.

**Why:** `sqlalchemy.Uuid` (not `sqlalchemy.dialects.postgresql.UUID`) is used in `models.py` so the ORM layer is backend-agnostic. The postgresql dialect type breaks on SQLite; the generic `Uuid` maps to native UUID on Postgres and String(32) on SQLite transparently.

**How to apply:** Do not change the `Uuid` import back to `postgresql.UUID` — that would break SQLite dev mode. If someone asks why we're not using the dialect-specific type, this is the reason.
