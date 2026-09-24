# Persistence Strategy

## Storage Engine: SQLite

We use SQLite as our embedded, local-first storage mechanism accessed through Entity Framework Core (`Microsoft.EntityFrameworkCore.Sqlite`).

## Key Decisions

1. **Local AppData Location**:
   - Database file stored at `%LOCALAPPDATA%\DesktopCalendar\calendar.db`.
   - In Development: Configurable or localized to project folder or memory.
2. **Deterministic Schema & Migrations**:
   - All migrations are tracked via EF Core Migrations tooling.
   - Database initialization executes `context.Database.MigrateAsync()` during host startup.
   - Non-destructive updates: Destructive migration strategies (e.g. dropping production columns without backups) are strictly prohibited.
3. **Connection Management**:
   - Connection lifetime scoped per unit of work / UI operation.
   - SQLite WAL (Write-Ahead Logging) mode enabled for concurrency and read/write performance.
4. **Backup & Export**:
   - Export mechanism copies SQLite DB or serializes tables to JSON format for portability.
   - No sensitive data or credentials stored unencrypted.
5. **No Direct UI Access**:
   - Views and ViewModels NEVER touch `AppDbContext` directly.
   - Access is mediated strictly via interfaces (`ICalendarEventRepository`, `INoteRepository`).
