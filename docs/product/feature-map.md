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
    └── Switch to Widget 🟢
```

## Feature Status Legend

- 🔴 Not started
- 🟡 Shell / Placeholder
- 🟢 Complete (Phase Scope Met)

## Current Status

Engineering Foundation (Phases 0–3) and Phases 4–10 (Application Shell, Widget UI, Calendar Grid & Navigation, Persistence, Events Management, Notes Management, Settings & Appearance) are complete. The application has a Generic Host composition root, DI container, explicit application lifecycle, bidirectional window switching, month navigation, 42-cell deterministic grid rendering, real-time clock, an EF Core SQLite persistence layer, full Event Management (CRUD UI, modal dialogs, day detail panel, domain validation, and 42-cell event indicator loading), full Notes Management (master-detail UI, CRUD operations, delete confirmation modal, domain validation, and case-insensitive substring search), and full Settings & Appearance (runtime theme switching across Dark/Light/System without restart, opacity slider, always-on-top toggle, start with Windows configuration, calendar preferences, JSON backup export, safe transactional import, and factory reset). The next phase is Phase 11 (Windows Integration).
