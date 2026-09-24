# Desktop Widget Feature Specification

## 1. Overview
The Desktop Widget is the primary quick-glance experience. It resides unobtrusively on the desktop, displaying the current month, current time, and day-specific event details on demand.

---

## 2. Behavioral Specifications

### 2.1 Default Compact State
- **Trigger**: Application starts in Widget mode, or day detail tray is closed.
- **Visual Presentation**:
  - Top header: `[APP]` switch button on top-left, `[─]` minimize button on top-right.
  - Subheader: Current month and year (left-aligned), current digital time (right-aligned, updating every second).
  - Body: Compact 7x6 month grid.
  - Dimensions: Fixed compact footprint (~280px width × ~240px height).
- **Constraints**:
  - No horizontal scrolling; must remain strictly within compact bounds.

### 2.2 Day Selection & Vertical Tray Expansion
- **Trigger**: User clicks an individual day cell in the widget calendar grid.
- **State Changes**:
  - `SelectedDate` is assigned.
  - `IsExpanded` transitions to `true`.
  - Day events are retrieved for `SelectedDate`.
- **Visual Presentation**:
  - The widget window smoothly expands vertically (approx. +100px to 140px height, ease-out transition < 200ms).
  - A detail panel appears below a horizontal divider showing:
    - Selected day header (e.g. "TODAY" or "SEPTEMBER 15, 2026").
    - Chronological list of events with start time and title.
    - Empty state message if no events exist ("No events scheduled").
- **Toggling / Collapse**:
  - Clicking the currently selected day a second time toggles `IsExpanded` to `false` and collapses the tray back to compact height.
  - Pressing `Escape` collapses the expanded tray.
- **Verification / Test Scenarios**:
  - Given collapsed widget, clicking Day 1 sets `IsExpanded = true` and target height to expanded size.
  - Clicking Day 1 again sets `IsExpanded = false` and target height to compact size.
  - Selecting Day 2 while Day 1 is expanded keeps `IsExpanded = true` and updates displayed events to Day 2.

### 2.3 Application Switch Action
- **Trigger**: User clicks the `[APP]` button in the top-left corner.
- **State Changes**:
  - Full desktop application is requested via window manager service.
  - Widget window hides or minimizes according to configuration.
- **Verification / Test Scenarios**:
  - Executing `SwitchToAppCommand` invokes `IWindowManager.ShowFullApplication()`.

### 2.4 Minimize Action
- **Trigger**: User clicks `[─]` button.
- **State Changes**:
  - Window state changes to Minimized (or hidden to system tray if tray integration enabled).
