# Settings Feature Specification

## 1. Overview
The Settings section provides user configuration for application appearance, widget display behavior, calendar formatting preferences, and local data maintenance.

---

## 2. Behavioral Specifications

### 2.1 Appearance Settings
- **Options**:
  - `Theme`: Dark (default), Light, or System.
- **Trigger**: User selects a theme option from the dropdown or radio group.
- **State Changes**:
  - Application resource dictionaries (`Dark.xaml` / `Light.xaml`) are swapped dynamically at runtime.
  - Setting is saved to local configuration.
- **Verification / Test Scenarios**:
  - Switching to Light theme replaces root background brush with `#F3F3F3`.
  - Switching to Dark theme replaces root background brush with `#1E1E1E`.

### 2.2 Widget Behavior Settings
- **Options**:
  - `AlwaysOnTop`: Boolean (default `false`). When true, sets `Window.Topmost = true` on the widget.
  - `StartWithWindows`: Boolean (default `false`). Configures startup registry key or startup task.
  - `WidgetOpacity`: Slider (50% to 100%, default 100%). Modifies `Window.Opacity`.
- **Verification / Test Scenarios**:
  - Toggling `AlwaysOnTop` updates the widget window `Topmost` property immediately without application restart.

### 2.3 Calendar Preferences
- **Options**:
  - `FirstDayOfWeek`: Monday (default) or Sunday.
  - `TimeFormat`: 24-hour (default) or 12-hour (AM/PM).
  - `DateFormat`: Regional short date pattern.
- **State Changes**:
  - Changing `FirstDayOfWeek` immediately recalculates and refreshes the calendar grid columns and day offsets.
- **Verification / Test Scenarios**:
  - Setting `FirstDayOfWeek = Sunday` sets column index 0 header to "S" / "Sun".

### 2.4 Data Management Settings
- **Options**:
  - `ExportData`: Serializes events and notes into a portable JSON backup file.
  - `ImportData`: Restores events and notes from a valid exported JSON file.
  - `ResetDatabase`: Clears all user events and notes after double-confirmation.
- **Verification / Test Scenarios**:
  - Export produces valid JSON matching domain schemas.
  - Reset database deletes all records from `CalendarEvents` and `Notes` tables.
