# Calendar Domain & Feature Specification

## 1. Overview
The calendar provides month-view navigation, day selection, event markers, and event management. It serves as both the data provider for the compact widget and the core view in the full desktop application.

---

## 2. Behavioral Specifications

### 2.1 Month Grid Generation
- **Trigger**: Calendar view loads, or user navigates to previous/next month.
- **State Changes**:
  - `CurrentMonth` and `CurrentYear` properties are updated.
  - 42-cell (7 columns × 6 rows) calendar grid is computed.
  - Preceding trailing days from the previous month and following leading days from the next month are populated.
- **Visual Presentation**:
  - Month and year displayed in header (e.g. "SEPTEMBER 2026").
  - Weekday columns header displayed based on configured first day of week (Monday or Sunday).
  - Trailing/leading days outside the current month are rendered with subtle/muted styling (0.5 opacity).
  - Current system day ("Today") receives a distinct accent boundary.
- **Constraints**:
  - Calculations must correctly handle leap years, daylight saving transitions, and 28/29/30/31-day months.
- **Verification / Test Scenarios**:
  - Given year 2024 (leap year) and month February, the grid contains 29 active days.
  - Given first-day-of-week = Monday, the first column of the grid corresponds to Monday.

### 2.2 Date Selection
- **Trigger**: User clicks or keyboard-navigates to a specific date in the calendar grid.
- **State Changes**:
  - `SelectedDate` is updated to the targeted date.
  - Previous selection state is cleared.
- **Visual Presentation**:
  - Selected date cell transitions to accent background with high-contrast text.
- **Verification / Test Scenarios**:
  - Selecting date X updates `SelectedDate == X`.
  - Pressing arrow keys shifts `SelectedDate` by ±1 day (Left/Right) or ±7 days (Up/Down).

### 2.3 Event Indicators
- **Trigger**: Events are loaded or modified for the displayed month range.
- **State Changes**:
  - Day cells query the repository for events overlapping their 24-hour timestamp range.
  - Flag `HasEvents` is evaluated for each day cell.
- **Visual Presentation**:
  - A subtle 4px circular dot indicator appears centered below the day number when `HasEvents == true`.
- **Verification / Test Scenarios**:
  - Adding an event for 2026-09-15 causes the day cell for 15 to set `HasEvents = true`.
  - Deleting all events on 2026-09-15 reverts `HasEvents` to `false`.

### 2.4 Event Creation & Modification
- **Trigger**: User submits event form with Title, StartTime, EndTime, Description.
- **State Changes**:
  - An `Event` entity is instantiated with unique GUID and UTC timestamps.
  - Entity is persisted via `ICalendarEventRepository`.
- **Constraints**:
  - Title is mandatory (non-empty, non-whitespace, max 200 characters).
  - `EndTime >= StartTime`. If violated, raises domain validation error.
- **Verification / Test Scenarios**:
  - Attempting to save event with `EndTime < StartTime` throws `DomainValidationException`.
  - Attempting to save event with empty title fails validation.
