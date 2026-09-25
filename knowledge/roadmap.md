# Canonical Project Roadmap

This document serves as the canonical source of truth for project phase definitions, execution order, and scope boundaries.

---

## Part I: Engineering Foundation

### Phase 0 — Repository Foundation
- **Status**: COMPLETED
- **Scope**: Initial repository structure, solution setup, project organization, baseline tooling, `.gitignore`, `.gitattributes`, LICENSE, build scripts.

### Phase 1 — Architecture & Context
- **Status**: COMPLETED
- **Scope**: Architecture definition, Clean Architecture project boundaries, development conventions, AI context (`AGENTS.md`, `CLAUDE.md`, `GEMINI.md`, Cursor rules, Copilot instructions), documentation structure, specifications (`specs/`), and engineering rules.

### Phase 2 — Repository Audit
- **Status**: COMPLETED
- **Scope**: Read-only comprehensive audit of architecture, implementation, documentation, testing, CI/CD, AI context, and project consistency.

### Phase 3 — Audit Remediation
- **Status**: COMPLETED
- **Scope**: Controlled remediation of verified audit findings: `.gitignore` corrections, removing temporary build artifacts, renaming test fixtures, authoring feature specifications, and harmonizing documentation.

---

## Part II: Product Implementation

### Phase 4 — Application Shell
- **Status**: COMPLETED
- **Scope**:
  - WPF application shell and Host startup lifecycle (`Microsoft.Extensions.Hosting`)
  - Dependency injection integration and service composition
  - `MainWindow` shell container
  - `WidgetWindow` shell container
  - Basic navigation placeholders (Calendar, Notes, Settings)
  - Widget ↔ Main Application switching mechanism
  - Standard window controls (minimize, maximize/restore, close)
  - Initial design-system resource dictionaries and styling integration
  - Window lifecycle and state management
  - Initial application-level automated tests

### Phase 5 — Widget UI
- **Status**: COMPLETED
- **Scope**:
  - Compact calendar widget layout
  - Real-time digital clock display
  - Month navigation controls
  - Selected date visualization
  - Expandable/collapsible day detail tray with smooth transition
  - Event indicators on day cells
  - Initial widget interaction and hover models

### Phase 6 — Calendar
- **Status**: COMPLETED
- **Scope**:
  - 42-cell calendar grid calculation and rendering
  - Month navigation and boundary month trailing/leading days
  - Date selection and today indicator
  - Full keyboard navigation (Arrow keys, PageUp/PageDown, Home/End)
  - Configurable first day of week (Monday / Sunday)
  - Calendar presentation behavior and styling

### Phase 7 — Persistence
- **Status**: COMPLETED
- **Scope**:
  - EF Core SQLite integration and configuration
  - Deterministic database migrations pipeline
  - Concrete repositories (`EfCalendarEventRepository`, `EfNoteRepository`)
  - Persistence integration with DI container
  - Database initialization, WAL mode configuration, and safe startup checks

### Phase 8 — Events
- **Status**: NEXT
- **Scope**:
  - Event CRUD operations (Create, Read, Update, Delete)
  - Validation rules (`EndTime >= StartTime`, mandatory title)
  - Event indicators in calendar month grid
  - Day detail event list display
  - Event persistence integration

### Phase 9 — Notes
- **Status**: PLANNED
- **Scope**:
  - Notes CRUD operations
  - Note list view and editor view
  - Creation and update timestamps
  - Substring search filtering across title and content
  - Persistence integration

### Phase 10 — Settings
- **Status**: PLANNED
- **Scope**:
  - Dark / Light theme runtime switching
  - Always-on-top window toggle
  - Start with Windows configuration
  - Widget opacity adjustment
  - First day of week and time/date format preferences
  - Data export, import, and database reset

### Phase 11 — Windows Integration
- **Status**: PLANNED
- **Scope**:
  - Single-instance application enforcement via named global OS Mutex
  - Secondary-instance activation and focus handoff
  - Dual-window lifecycle coordination
  - Window position and size persistence across restarts
  - Off-screen recovery for multi-monitor disconnects
  - Per-Monitor V2 DPI scaling awareness
  - System tray icon and minimize-to-tray integration

### Phase 12 — Testing & Polish
- **Status**: PLANNED
- **Scope**:
  - Comprehensive unit test expansion
  - Persistence and lifecycle integration tests
  - UI/application-level validation
  - Accessibility compliance (WCAG 2.1 AA, high-visibility focus, screen reader names)
  - Keyboard navigation refinement
  - Visual consistency and reduced-motion compliance
  - Performance profiling and edge-case hardening

### Phase 13 — Packaging & Release
- **Status**: PLANNED
- **Scope**:
  - Production Release configuration build
  - Self-contained / framework-dependent packaging
  - Distribution installer preparation
  - Version numbering and release documentation
  - GitHub release workflow configuration
