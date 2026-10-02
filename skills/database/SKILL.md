---
name: database
description: Persistence decision aid for SQLite/EF Core and file-backed settings, including data safety and concurrent writes.
---

# Persistence & Database Decision Aid

Use for data storage, EF Core/SQLite, file-backed settings, or concurrent persistence. Start with `docs/architecture/persistence.md` and inspect the owning repository/service and its tests; consult `knowledge/failure-memory.md` for the historical file-replacement incident.

## Choose a Path

- For mapping or query changes, update existing `AppDbContext` configuration and test provider behavior with temporary SQLite integration tests when needed.
- For schema changes, update the domain model as required, create a migration with the configured project/startup pair, and inspect generated operations for data loss.
- For file persistence, preserve atomic replacement, path-level coordination between repository instances, unique same-directory temporary files, valid prior data, and interrupted-write recovery. Verify the actual state transition and cleanup paths, not only the happy path.
- For concurrent writers, establish the intended ordering/latest-state semantics explicitly and use deterministic gates in regression tests; do not infer ordering from task start time.
- For destructive transformations, define data preservation or rollback before applying them.

Never commit or delete user data. Validate focused persistence regressions first; use the full suite when a shared persistence contract changes. Keep incident-specific chronology in Failure Memory rather than copying it into this workflow.
