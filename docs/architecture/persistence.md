# Persistence Strategy

## Storage Engine: SQLite

We use SQLite as our embedded, local-first storage mechanism accessed through Entity Framework Core (`Microsoft.EntityFrameworkCore.Sqlite`).

## Key Decisions

1. **Local AppData Location**:
   - Database file stored at `%LOCALAPPDATA%\DesktopCalendar\calendar.db`.
   - The parent directory is created automatically at startup if it does not exist.
   - Tests use isolated temporary SQLite files in `%TEMP%`, cleaned up after each test.
2. **Deterministic Schema & Migrations**:
   - All migrations are tracked via EF Core Migrations tooling.
   - Initial migration: `20260925191413_InitialCreate` — creates `CalendarEvents` and `Notes` tables with all constraints and indexes.
   - Database initialization executes `context.Database.MigrateAsync()` during host startup via `DatabaseInitializer`.
   - Non-destructive updates: Destructive migration strategies (e.g. dropping production columns without backups) are strictly prohibited.
3. **Connection Management**:
   - Connection lifetime scoped per unit of work / UI operation (`AddDbContext` with scoped lifetime).
   - SQLite WAL (Write-Ahead Logging) mode enabled via `PRAGMA journal_mode=WAL` in `DatabaseInitializer.EnableWalModeAsync`.
4. **Repository Implementations**:
   - `EfCalendarEventRepository` implements `ICalendarEventRepository` using EF Core + SQLite.
   - `EfNoteRepository` implements `INoteRepository` using EF Core + SQLite.
   - Both use `AsNoTracking()` for read operations and deterministic ordering.
   - `GetByDateRangeAsync` uses strict overlap semantics: `event.StartTime < requestedEnd AND event.EndTime > requestedStart`.
   - `SearchAsync` uses `EF.Functions.Like` for case-insensitive SQLite substring matching.
5. **Design-Time Factory**:
   - `AppDbContextFactory` implements `IDesignTimeDbContextFactory<AppDbContext>` to support `dotnet ef` tooling without requiring the WPF startup project.
6. **Backup & Export**:
   - Export mechanism copies SQLite DB or serializes tables to JSON format for portability (planned Phase 10).
   - No sensitive data or credentials stored unencrypted.
7. **No Direct UI Access**:
   - Views and ViewModels NEVER touch `AppDbContext` directly.
   - Access is mediated strictly via interfaces (`ICalendarEventRepository`, `INoteRepository`).
