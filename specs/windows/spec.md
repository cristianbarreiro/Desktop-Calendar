# Windows Integration & Lifecycle Specification

## 1. Overview
Defines operating system level behaviors including window management, single-instance execution, DPI awareness, startup, and system tray integration.

---

## 2. Behavioral Specifications

### 2.1 Single Instance Enforcement
- **Trigger**: User launches the executable.
- **Mechanism**: Global named OS Mutex (`DesktopCalendarWidget_SingleInstance`).
- **Behavior**:
  - If no other instance owns the mutex, the application proceeds with normal startup.
  - If an existing instance holds the mutex, the secondary instance activates/focuses the primary instance window and terminates immediately without showing duplicate UI.
- **Verification / Test Scenarios**:
  - Launching two instances concurrently yields exactly one active UI process.

### 2.2 Dual-Mode Window Switching
- **Trigger**:
  - User clicks `[APP]` in Widget window.
  - User clicks `[WIDGET]` in Full Application window.
- **Behavior**:
  - `IWindowManager.SwitchToFullApplication()`:
    1. Instantiates or reveals `MainWindow`.
    2. Centers `MainWindow` or restores previous window bounds.
    3. Hides or minimizes `WidgetWindow`.
  - `IWindowManager.SwitchToWidget()`:
    1. Reveals `WidgetWindow` at its saved screen coordinates.
    2. Minimizes or hides `MainWindow`.
- **Verification / Test Scenarios**:
  - Invoking window switch preserves unsaved state where applicable and alters window visibility accordingly.

### 2.3 Window Position Persistence
- **Trigger**: Window moves or resizes.
- **Behavior**:
  - Debounced saving of `Left`, `Top`, `Width`, `Height` to local settings.
  - On subsequent launch, window restores to the saved position.
  - If saved coordinates fall outside current active monitor display bounds (e.g., disconnected monitor), window defaults to primary monitor center.
- **Verification / Test Scenarios**:
  - Given off-screen coordinates (-5000, -5000), window reposition logic forces coordinates onto visible desktop area.

### 2.4 DPI Awareness & Scaling
- **Configuration**: Per-Monitor V2 DPI awareness configured via application manifest.
- **Behavior**:
  - Text, borders, and controls scale crisp and clear across high-DPI displays and mixed-DPI multi-monitor environments.
  - No blurry bitmap scaling or miscalculated hit-testing on display crossing.
