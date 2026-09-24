# Domain Knowledge & Rules

## Entities

### CalendarEvent
- Represents an event scheduled at a given time or all day.
- Fields: `Id` (Guid), `Title` (string, required, max 200 chars), `Description` (string, optional, max 2000 chars), `StartTime` (DateTime UTC), `EndTime` (DateTime UTC), `IsAllDay` (bool), `CreatedAt` (DateTime UTC), `UpdatedAt` (DateTime UTC).
- **Rules**:
  - `EndTime` must be greater than or equal to `StartTime`.
  - For all-day events, time components default to 00:00:00 to 23:59:59.
  - Title cannot be empty or whitespace.

### Note
- Represents an offline-first scratchpad or persistent record.
- Fields: `Id` (Guid), `Title` (string, required, max 200 chars), `Content` (string, max 50,000 chars), `CreatedAt` (DateTime UTC), `UpdatedAt` (DateTime UTC).
- **Rules**:
  - Title cannot be empty.
  - Search matches title or content via case-insensitive substring search.

## Value Objects (Planned)
- `DateRange`: Encapsulates start and end timestamps with overlap validation.
- `TimeRange`: Time-of-day representation for recurrence.
- `ColorHex`: Hex color representation for categories (#RRGGBB).
