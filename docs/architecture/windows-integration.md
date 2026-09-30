# Windows OS Integration

## Core Integration Points

The Desktop Calendar Widget connects with native Windows capabilities through contracts owned by the layer that consumes them and implementations isolated in `App` and `Infrastructure`. Domain persistence contracts live in `Core`; window/UI orchestration and shell integration contracts live in `Presentation` and `App`.

### 1. Single-Instance Enforcement & Inter-Process Communication
- Single-instance enforcement using named global OS Mutex (`DesktopCalendarWidget_SingleInstance`) in `Program.cs` before Generic Host creation.
- Local asynchronous Named Pipe IPC (`DesktopCalendarWidget_SingleInstance_Pipe`) managed by `SingleInstanceCoordinator` (`ISingleInstanceCoordinator`).
- Secondary instances send an activation signal and terminate immediately with zero duplicate UI or host creation.
- Primary instance receives the signal and activates/focuses the active application or widget window on the UI thread (`ActivateCurrentWindow()`).

### 2. System Tray & Shell Lifecycle
- Notification area (System Tray) icon managed by `SystemTrayService` (`ITrayService`) using Windows `NotifyIcon`.
- Context menu options: "Open Application", "Open Widget", separator, and "Exit".
- Left-click and double-click actions restore and activate the current/preferred window.
- Minimize-to-tray: minimizing hides the active window while preserving the process under `ShutdownMode.OnExplicitShutdown`.
- Clean shutdown from tray and deterministic disposal on application exit, preventing unmanaged icon leaks or ghost tray icons.

### 3. Auto-Start with Windows
- Configured via registry key `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.
- Wrapped in an infrastructure-facing `IWindowsStartupService` / `WindowsStartupService` contract operating with standard user privileges (`asInvoker`).

### 4. Window Positioning & Display Awareness
- **Always-on-top**: Configurable `Topmost` WPF window property bound to `UserSettings.AlwaysOnTop`.
- **Multi-monitor & DPI scaling**: Handled via Per-Monitor V2 DPI awareness configured in `app.manifest` and project configuration (`ApplicationHighDpiMode`).
- **Position persistence**: Debounced asynchronous saving of `Left`, `Top`, and `MainWindowWidth`/`MainWindowHeight` to `settings.json` via `WindowPlacementService` (`IWindowPlacementService`).
- **Off-screen recovery**: Multi-monitor geometric bounds checking via `WindowBoundsHelper` and `IDisplayMonitorProvider` (`WpfDisplayMonitorProvider`), supporting negative monitor coordinates and relocating windows from disconnected monitors onto the visible primary display.

### 5. Native Notifications (Future Scope)
- Windows Toast notifications for upcoming calendar event reminders (architecture-ready).
- Encapsulated behind `INotificationService`.

## Constraints & Security
- No elevated / administrator privileges requested (`asInvoker` execution level).
- No background network listeners or invasive system hooks.
- Named pipe communication strictly confined to local machine (`.`) same-user boundary.
