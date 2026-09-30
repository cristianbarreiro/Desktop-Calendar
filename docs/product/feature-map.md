# Feature Map

## Application Modes

```
Desktop Calendar Widget
├── Widget Mode (compact) 🟢 [Shell Container]
│   ├── Calendar Grid 🟡 [Shell Grid]
│   ├── Current Time 🟢
│   ├── Day Selection + Detail 🟡 [Shell Selection / Tray Toggle]
│   ├── Event Indicators 🟢
│   ├── Open App Button 🟢
│   └── Minimize / Close Control 🟢
│
└── Full Application Mode (window) 🟢 [Shell Container]
    ├── Calendar 🟢
    │   ├── Month View 🟢 [42-cell Shell]
    │   ├── Month Navigation 🟢
    │   ├── Day Selection 🟢
    │   ├── Event Management (CRUD) 🟢
    │   └── Day Detail Panel 🟢
    │
    ├── Notes 🟢
    │   ├── Note List 🟢
    │   ├── Note Creation 🟢
    │   ├── Note Editing 🟢
    │   ├── Note Deletion 🟢
    │   └── Note Search 🟢
    │
    ├── Settings 🟢
    │   ├── Appearance (Themes, Opacity, Always on Top) 🟢
    │   ├── Calendar Preferences (First Day, Date/Time Formats) 🟢
    │   ├── Windows Integration (Start with Windows) 🟢
    │   └── Data Management (Export, Import, Reset) 🟢
    │
 │   └── Switch to Widget 🟢
│
└── Windows Shell Integration 🟢
    ├── Single-Instance Mutex & Named Pipe IPC 🟢
    ├── System Tray & Minimize-to-Tray 🟢
    ├── Position Persistence & Off-Screen Recovery 🟢
    ├── Per-Monitor V2 DPI Awareness 🟢
    └── Dual-Window Lifecycle Coordination 🟢
```

## Feature Status Legend

- 🔴 Not started
- 🟡 Shell / Placeholder
- 🟢 Complete (Phase Scope Met)

## Current Status

Engineering Foundation (Phases 0–3) and Phases 4–11 (Application Shell, Widget UI, Calendar Grid & Navigation, Persistence, Events Management, Notes Management, Settings & Appearance, Windows Integration) are complete. The application has a Generic Host composition root, DI container, explicit application lifecycle, single-instance global mutex enforcement with Named Pipe secondary activation handoff, notification area system tray integration with minimize-to-tray, multi-monitor off-screen bounds recovery, debounced window position/size persistence, Per-Monitor V2 DPI awareness, bidirectional window switching, month navigation, 42-cell deterministic grid rendering, real-time clock, EF Core SQLite persistence, full Event Management (CRUD UI, modal dialogs, day detail panel, domain validation, and 42-cell event indicator loading), full Notes Management (master-detail UI, CRUD operations, delete confirmation modal, domain validation, and case-insensitive substring search), and full Settings & Appearance (runtime theme switching across Dark/Light/System without restart, opacity slider, always-on-top toggle, start with Windows configuration, calendar preferences, JSON backup export, safe transactional import, and factory reset). The next phase is Phase 12 (Testing, Accessibility & Polish).
