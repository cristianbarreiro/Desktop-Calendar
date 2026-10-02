---
name: database
description: EF Core SQLite schema management, migration workflows, and query optimization.
---

# Database Workflow Aid

Use for EF Core mappings, SQLite queries, or schema changes. Start with `docs/architecture/persistence.md` and inspect the current model and migrations.

## Choose a Path

- For mapping or query changes, update existing `AppDbContext` configuration and test provider behavior with temporary SQLite integration tests when needed.
- For schema changes, update the domain model as required, create a migration with the configured project/startup pair, and inspect generated operations for data loss.
- For destructive transformations, define data preservation or rollback before applying them.

Never commit user database files. Validate affected integration tests; expand to the full suite when persistence contracts or shared state change.
