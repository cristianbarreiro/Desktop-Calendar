---
name: database
description: EF Core SQLite schema management, migration workflows, and query optimization.
---

# Database Skill

## Purpose
Guide database changes, schema migrations, and entity mappings with EF Core and SQLite.

## When to Use
- Adding or modifying entities in `CalendarWidget.Core.Entities`.
- Modifying `AppDbContext` in `CalendarWidget.Infrastructure.Persistence`.
- Generating or applying EF Core migrations.

## Prerequisites
- Review `docs/architecture/persistence.md`.
- `dotnet-ef` global tool (if generating migrations).

## Workflow
1. Update entities in `Core`.
2. Configure model mappings via Fluent API in `AppDbContext.OnModelCreating`.
3. Add migration via CLI:
   ```bash
   dotnet ef migrations add <MigrationName> --project src/CalendarWidget.Infrastructure --startup-project src/CalendarWidget.App
   ```
4. Verify migration script performs non-destructive schema adjustments.
5. Test against in-memory or temporary SQLite database in `CalendarWidget.IntegrationTests`.

## Constraints
- Never commit user database files (`*.db`).
- Never perform destructive schema operations without explicit migration strategies.

## Validation
- Execute integration tests: `dotnet test tests/CalendarWidget.IntegrationTests`.
