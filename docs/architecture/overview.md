# Architecture Overview

## Architectural Style

The Desktop Calendar Widget employs a **Layered Architecture** adhering to **Clean Architecture** and **MVVM (Model-View-ViewModel)** principles. The architecture is strictly decoupled and unidirectional:

```
[ CalendarWidget.App ]
       │            │           │
       ▼            │           ▼
[ Presentation ]    │    [ Infrastructure ]
       │            │           │
       │            ▼           │
       └─────► [ Core ] ◄───────┘
```

## Layer Responsibilities

### 1. CalendarWidget.Core (Domain Layer)
- **Role**: Pure domain logic, entities, value objects, domain interfaces, and validation rules.
- **Dependencies**: None (pure C# standard library).
- **Rules**:
  - MUST NOT reference WPF, UI frameworks, EF Core, or SQLite.
  - Entities encapsulate business state and validation.
  - Defines repository interfaces (`ICalendarEventRepository`, `INoteRepository`).

### 2. CalendarWidget.Infrastructure (Persistence & OS Integration)
- **Role**: Persistence (EF Core, SQLite), OS-specific integrations (Win32, system tray, notifications).
- **Dependencies**: `CalendarWidget.Core`, EF Core Sqlite.
- **Rules**:
  - Implements interfaces defined in `Core`.
  - Encapsulates database migrations, schema creation, and SQLite configurations.
  - MUST NOT depend on `Presentation` or WPF UI elements.

### 3. CalendarWidget.Presentation (UI & ViewModels)
- **Role**: Views, ViewModels, UI controls, styles, themes, converters.
- **Dependencies**: `CalendarWidget.Core`, `CommunityToolkit.Mvvm`, WPF.
- **Rules**:
  - ViewModels orchestrate UI state and user intent.
  - Binds to Core entities/models or Presentation DTOs.
  - MUST NOT reference `Infrastructure` or direct EF Core DbContext.

### 4. CalendarWidget.App (Application Composition Root)
- **Role**: Entry point, startup sequence, dependency injection wiring (`Microsoft.Extensions.Hosting`), and concrete window lifecycle composition.
- **Dependencies**: References `Core`, `Infrastructure`, and `Presentation`.
- **Rules**:
  - Configures services, registers repositories and view models.
  - Coordinates concrete window lifecycles (Widget Mode vs Full Application Mode).
  - Owns application-specific window orchestration; window-management contracts must remain outside Core.

## Cross-Cutting Guidelines
- **Offline / Local First**: No network calls by default; data resides in a local SQLite file.
- **Observability**: Structured logging via `Microsoft.Extensions.Logging`.
- **Decoupled System Calls**: Windows API / shell hooks are isolated behind infrastructure or application-facing contracts as appropriate; domain contracts remain technology-agnostic.
