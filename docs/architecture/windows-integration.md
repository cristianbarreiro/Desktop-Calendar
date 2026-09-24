# Windows OS Integration

## Core Integration Points

The Desktop Calendar Widget connects with native Windows capabilities through contracts owned by the layer that consumes them and implementations isolated in `Infrastructure`. Domain persistence contracts may live in `Core`; window/UI orchestration contracts do not.

### 1. System Tray & Shell Lifecycle
- Minimizing widget can dock to the notification area (Tray Icon) using native Windows Shell APIs or NotifyIcon.
- Single-instance enforcement using named Mutex (`DesktopCalendarWidget_SingleInstance`).

### 2. Auto-Start with Windows
- Configured via registry key `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` or Windows Startup Task API.
- Wrapped in an application/infrastructure-facing `IStartupManager` contract to enable unit/integration testing without modifying the actual host registry during tests.

### 3. Window Positioning & Display Awareness
- **Always-on-top**: Configurable `Topmost` WPF window property.
- **Multi-monitor & DPI scaling**: Handled via Per-Monitor V2 DPI awareness configured in `app.manifest`.
- **Position persistence**: Saves `Left`, `Top`, and `WindowWidth`/`WindowHeight` to local settings on window move/resize.

### 4. Native Notifications
- Windows Toast notifications for upcoming calendar event reminders.
- Encapsulated behind `INotificationService`.

## Constraints & Security
- No elevated / administrator privileges requested (`asInvoker` execution level).
- No background network listeners or invasive system hooks.
