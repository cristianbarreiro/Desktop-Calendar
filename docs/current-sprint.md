# Current Sprint

## Sprint Objective

Prepare the repository for the first product implementation vertical slice and establish the Application Shell as the next implementation target.

---

## Status

- **Engineering Foundation (Phases 0–3)**: COMPLETE
- **Product Implementation (Phases 4–13)**: READY TO START

---

## Completed

- [x] **Repository Foundation (Phase 0)**: Solution setup, 6 projects, Directory.Build.props, .editorconfig, git hygiene.
- [x] **Architecture & Context (Phase 1)**: Clean Architecture boundaries, zero-outward Core, AGENTS.md canonical contract, multi-agent context files (Claude, Gemini, Cursor, Copilot), skills suite, internal knowledge base.
- [x] **Specifications (Phase 1/3)**: Full behavioral specifications in `/specs/` (calendar, widget, notes, settings, windows).
- [x] **Repository Audit (Phase 2)**: Comprehensive read-only audit across architecture, testing, CI, and AI context.
- [x] **Audit Remediation (Phase 3)**: Fixed `.gitignore` release rule, untracked intermediate artifacts, renamed test fixtures, harmonized documentation.
- [x] **Build & Test Validation**: 0 errors, 0 warnings, 5/5 automated tests passing, format verification passing.

---

## Next Implementation Target

### Phase 4 — Application Shell

- Implement `Program.cs` Generic Host composition root (`Microsoft.Extensions.Hosting`).
- Register DI services, view models, and window management.
- Establish `MainWindow` and `WidgetWindow` shell containers.
- Implement basic navigation and view switching (`[APP]` ↔ `[WIDGET]`).
- Integrate initial styling and design system tokens.
- Add application-level validation tests.

---

## Explicitly Out of Scope for Phase 4

Do NOT implement during Application Shell:
- SQLite database CRUD operations (Planned Phase 7)
- EF Core migrations (Planned Phase 7)
- Event persistence and validation forms (Planned Phase 8)
- Notes persistence and text editor (Planned Phase 9)
- Recurring events logic
- Event reminders and notifications
- External calendar synchronization
- System tray icon docking (Planned Phase 11)
- Windows startup registration (Planned Phase 11)
- Advanced Windows integrations (Planned Phase 11)
- Packaging, installers, and release automation (Planned Phase 13)
