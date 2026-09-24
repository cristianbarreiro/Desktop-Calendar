# Development Log

## 2026-09-24 — Repository Initialization (Phase 0 & 1)

### Activities
- Inspected repository workspace (clean git state).
- Established multi-agent context files (`AGENTS.md`, `CLAUDE.md`, `GEMINI.md`, `.cursor/rules/`, `.github/`).
- Scaffolded .NET 10 solution `CalendarWidget.slnx` with 6 projects:
  - `CalendarWidget.App` (WPF Executable)
  - `CalendarWidget.Core` (Class library)
  - `CalendarWidget.Infrastructure` (Class library)
  - `CalendarWidget.Presentation` (WPF Class library)
  - `CalendarWidget.UnitTests` (xUnit)
  - `CalendarWidget.IntegrationTests` (xUnit)
- Configured project dependencies enforcing unidirectional Clean Architecture.
- Added foundational packages: `CommunityToolkit.Mvvm`, `Microsoft.Extensions.Hosting`, `Microsoft.EntityFrameworkCore.Sqlite`, `FluentAssertions`.
- Added `.editorconfig`, `Directory.Build.props`, and test-specific rule overrides.
- Implemented initial domain entities (`CalendarEvent`, `Note`) and repositories (`ICalendarEventRepository`, `INoteRepository`).
- Configured `AppDbContext` and initial passing unit/integration smoke tests.
- Authored product vision, requirements, user flows, architecture documents, UI design system, ADR-001, and knowledge base.

## 2026-09-24 — Phase 3: Controlled Remediation

### Activities
- Unanchored `.gitignore` pattern `[Rr]elease*/` corrected and `!skills/**` added, ensuring `skills/release/SKILL.md` is tracked in git.
- Untracked temporary MSBuild artifact `src/CalendarWidget.App/CalendarWidget.App_krlchx0l_wpftmp.csproj` and ignored via `*_wpftmp.csproj`.
- Renamed test files `UnitTest1.cs` to match class fixtures (`Core/CalendarEventTests.cs`, `Persistence/AppDbContextTests.cs`).
- Established complete `/specs/` hierarchy with 5 behavioral specifications (`calendar`, `widget`, `notes`, `settings`, `windows`).
- Harmonized `docs/architecture/application-structure.md` by annotating planned product implementation components.
- Validated solution build, test suite execution (5/5 passed), and formatting verification.

## 2026-09-24 — Phase Roadmap Normalization

### Activities
- Established Canonical Project Roadmap distinguishing Engineering Foundation (Phases 0–3) from Product Implementation (Phases 4–13).
- Updated `README.md`, `knowledge/roadmap.md`, `docs/project-state.md`, `docs/current-sprint.md`, and `docs/product/feature-map.md`.
- Normalized phase numbers across architecture documentation to designate Phase 4 (Application Shell) as the immediate next target.
- Verified global consistency across all agent instructions, rules, and documentation files.


