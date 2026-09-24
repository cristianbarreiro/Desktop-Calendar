---
name: release
description: Release preparation, packaging, versioning, and deployment verification for Desktop Calendar.
---

# Release Skill

## Purpose
Prepare production binaries, manage version increments, and ensure release builds satisfy all deployment criteria.

## When to Use
- Releasing a milestone or release candidate.
- Building self-contained or framework-dependent production distributions.

## Workflow
1. Verify all tests pass on Release configuration:
   ```bash
   dotnet test -c Release
   ```
2. Verify code formatting and analyzers:
   ```bash
   dotnet format --verify-no-changes
   ```
3. Publish WPF application:
   ```bash
   dotnet publish src/CalendarWidget.App/CalendarWidget.App.csproj -c Release -r win-x64 --self-contained false -o publish/win-x64
   ```
4. Verify executable launches without external database dependencies (SQLite auto-initializes).
5. Document changes in `docs/development-log.md` and bump version tag.

## Constraints
- Never release a build containing uncommitted debugging code or failing tests.
- Never package development database files.
