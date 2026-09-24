# Project State

## Overview
- **Project**: Modern Desktop Calendar Widget
- **Current Phase**: Phase 3 Complete (Controlled Remediation of Audit Findings)
- **Current Date**: 2026-09-24

## Implementation Status

### Core Domain
- [x] Initial Entities (`CalendarEvent`, `Note`)
- [x] Initial Interfaces (`ICalendarEventRepository`, `INoteRepository`)
- [x] Initial Value Object (`DateRange`)
- [ ] Value Objects (`TimeRange`, `ColorHex`)
- [x] Domain Exception (`DomainValidationException`)

### Specifications & AI Context
- [x] Full Specification Suite (`specs/calendar/`, `specs/widget/`, `specs/notes/`, `specs/settings/`, `specs/windows/`)
- [x] Canonical Agent Contract (`AGENTS.md`)
- [x] 8 Reusable Agent Skills (`skills/`) with Git tracking verified
- [x] Cursor & GitHub Copilot rule suite

### Infrastructure
- [x] EF Core `AppDbContext` configured with entity models
- [x] Infrastructure DI extension method (`AddInfrastructure`)
- [ ] SQLite repository implementations (`EfCalendarEventRepository`, `EfNoteRepository`)
- [ ] Database migration pipeline
- [ ] Windows Shell / Tray / Startup services

### Presentation
- [x] Presentation project created with `CommunityToolkit.Mvvm`
- [x] Foundational `ViewModelBase` created
- [ ] ViewModels (`WidgetViewModel`, `CalendarViewModel`, `NotesViewModel`, `SettingsViewModel`)
- [ ] XAML Views (`WidgetWindow`, `MainWindow`)
- [ ] Design System styles, colors, and controls

### Host Application
- [x] `CalendarWidget.App` setup with `Microsoft.Extensions.Hosting`
- [ ] Host service builder and window lifecycle management

### Testing & QA
- [x] Unit test project configured (`xUnit` + `FluentAssertions`)
- [x] Integration test project configured (`xUnit` + `FluentAssertions` + EF Core InMemory/Sqlite)
- [x] Renamed tests matching class fixtures (`CalendarEventTests.cs`, `AppDbContextTests.cs`)
- [x] 5 automated tests passing (0 failures)
- [x] Solution builds with 0 errors and 0 warnings

## Known Technical Debt
- Tracked temporary WPF build files removed and excluded via `.gitignore`.
- Git release ignore pattern anchored to prevent directory collisions.

## Current Priorities
1. Proceed to Application Shell & Window Lifecycle Management (Phase 2 Roadmap).
2. Implement Desktop Widget UI and compact calendar grid (Phase 3 Roadmap).
