# Phase 4 Readiness Audit

## Audit Metadata

- Date: 2026-09-24
- Audited revision: `de5fc589fbf9010f2c51c6f512b9146fe065d23e`
- Branch: `main`
- Audit type: Read-only readiness audit before Product Phase 4
- Target: Application Shell

## Executive Result

**Result: READY FOR PHASE 4 AFTER CONTROLLED REMEDIATION**

The repository foundation is coherent and the recent roadmap normalization is substantially correct. The product implementation itself has not started: the current WPF host still uses `StartupUri`, `MainWindow` is a template shell, there is no `Program.cs`, no `WidgetWindow`, and no application-level window manager.

No missing MVP feature blocks the start of Phase 4. Several architecture/documentation guardrails should be corrected before implementation so that Phase 4 agents do not place UI/application orchestration contracts in the domain layer or carry forward stale documentation.

---

## Findings

### F-01 — Architecture abstraction ownership is ambiguous
- Severity: **MEDIUM**
- Area: Architecture / AI guidance
- Evidence:
  - `skills/architecture/SKILL.md` states that new abstractions should be verified in `CalendarWidget.Core.Interfaces`.
  - The architecture also states that Core is a pure domain layer with zero UI/WPF dependencies.
  - Phase 4 requires an `IWindowManager` contract for UI/application window coordination.
- Risk:
  An agent can incorrectly place `IWindowManager` in Core, weakening the domain boundary.
- Required remediation:
  Explicitly distinguish:
  - domain contracts -> Core;
  - UI orchestration contracts -> Presentation;
  - infrastructure-private integration contracts -> Infrastructure;
  - concrete composition/window ownership -> App.
  Record this decision in an ADR.

### F-02 — Windows integration documentation overstates that all OS contracts live in Core
- Severity: **MEDIUM**
- Area: Architecture documentation
- Evidence:
  `docs/architecture/overview.md` and `docs/architecture/windows-integration.md` broadly describe Windows integrations as contracts defined in Core.
- Risk:
  The statement conflicts with the goal of keeping Core domain-focused and is especially problematic for window management.
- Required remediation:
  Narrow the rule to domain repository contracts in Core and define ownership by layer for UI/app/OS integration.

### F-03 — NotificationService is associated with Phase 11 while notifications are future scope
- Severity: **LOW**
- Area: Architecture roadmap alignment
- Evidence:
  `docs/architecture/application-structure.md` labels `NotificationService` as Planned Phase 11, while the canonical roadmap keeps reminders/notifications outside the MVP Windows Integration scope.
- Risk:
  Agents may implement reminder/notification behavior earlier than intended.
- Required remediation:
  Mark notifications as Future / post-MVP rather than Phase 11.

### F-04 — README setup directory is inaccurate
- Severity: **LOW**
- Area: Documentation
- Evidence:
  The repository is `Desktop-Calendar`, but setup instructions use `cd "Desktop Calendar"`.
- Risk:
  Copy/paste setup can fail.
- Required remediation:
  Use `cd Desktop-Calendar`.

### F-05 — .slnx file type is not explicitly declared in .gitattributes
- Severity: **LOW**
- Area: Repository hygiene
- Evidence:
  `.gitattributes` contains a rule for `.sln` but the solution is `CalendarWidget.slnx`.
- Risk:
  Minor inconsistency in text normalization configuration.
- Required remediation:
  Add `*.slnx text eol=crlf`.

### F-06 — Public Core API documentation is incomplete
- Severity: **LOW**
- Area: Code quality
- Evidence:
  Public properties and repository methods do not consistently have XML documentation despite the repository coding convention requiring documentation for public APIs.
- Risk:
  Inconsistent API documentation and weaker generated IntelliSense.
- Required remediation:
  Add concise XML documentation to current public production APIs without changing behavior.

### F-07 — Persistence validation is intentionally incomplete, not a Phase 4 blocker
- Severity: **INFO**
- Area: Testing / Persistence
- Evidence:
  Current integration coverage verifies `AppDbContext` construction with EF Core InMemory; concrete SQLite repositories and migrations are still planned for Phase 7.
- Assessment:
  This is correctly out of scope for Phase 4. Do not pull Phase 7 work forward to satisfy this audit.

### F-08 — Domain validation is intentionally incomplete, not a Phase 4 blocker
- Severity: **INFO**
- Area: Domain
- Evidence:
  `CalendarEvent` is currently a persistence-friendly entity without full validation, while the calendar specification requires validation that is planned for later product implementation.
- Assessment:
  Do not introduce event CRUD/validation into Phase 4.

### F-09 — Current CI is green on the audited revision
- Severity: **PASS**
- GitHub Actions run for `de5fc589fbf9010f2c51c6f512b9146fe065d23e` completed successfully.
- The workflow completed restore, build, test, and formatting verification steps successfully.

### F-10 — Dependency direction is coherent
- Severity: **PASS**
- Current project references match the documented direction:
  - App -> Core, Infrastructure, Presentation
  - Presentation -> Core
  - Infrastructure -> Core
  - Tests reference the layers under test
- No circular reference is present in the project files inspected.

### F-11 — Roadmap normalization is now coherent
- Severity: **PASS**
- Canonical engineering phases 0–3 are marked complete.
- Product Phase 4 is the next implementation target.
- README, project state, current sprint, feature map, and knowledge roadmap now describe the same phase model.

---

## Phase 4 Readiness Matrix

| Area | Status | Phase 4 Impact |
|---|---|---|
| Repository structure | PASS | None |
| Dependency boundaries | PASS | None |
| Canonical roadmap | PASS | None |
| Product specifications | PASS | None |
| AI agent guidance | PARTIAL | Remediate F-01/F-02 |
| WPF host bootstrap | PARTIAL | Phase 4 implementation |
| MainWindow | PARTIAL | Phase 4 implementation |
| WidgetWindow | NOT IMPLEMENTED | Phase 4 implementation |
| Window manager | NOT IMPLEMENTED | Phase 4 implementation |
| Navigation shell | NOT IMPLEMENTED | Phase 4 implementation |
| Design system resources | NOT IMPLEMENTED | Phase 4 implementation |
| Persistence repositories | NOT IMPLEMENTED | Correctly deferred to Phase 7 |
| Event CRUD | NOT IMPLEMENTED | Correctly deferred to Phase 8 |
| Notes | NOT IMPLEMENTED | Correctly deferred to Phase 9 |
| Windows integration | NOT IMPLEMENTED | Correctly deferred to Phase 11 |
| CI build/test/format | PASS | None |

## Audit Conclusion

The repository **does not need another product-preparation phase**.

The correct sequence is:

1. Record this audit.
2. Apply the controlled remediations for F-01 through F-06.
3. Re-run consistency/validation checks.
4. Begin **Product Phase 4 — Application Shell**.

Phase 4 should remain a vertical slice and must not absorb persistence, event CRUD, notes persistence, notifications, tray integration, startup registration, or packaging.
