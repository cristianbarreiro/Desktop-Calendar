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
    ├── Notes 🟡 [Shell Placeholder]
    │   ├── Note List 🔴
    │   ├── Note Creation 🔴
    │   ├── Note Editing 🔴
    │   ├── Note Deletion 🔴
    │   └── Note Search 🔴
    │
    ├── Settings 🟡 [Shell Placeholder]
    │   ├── Appearance 🔴
    │   ├── Widget Behavior 🔴
    │   ├── Calendar Preferences 🔴
    │   └── Data Management 🔴
    │
    └── Switch to Widget 🟢
```

## Feature Status Legend

- 🔴 Not started
- 🟡 Shell / Placeholder
- 🟢 Complete (Phase Scope Met)

## Current Status

Engineering Foundation (Phases 0–3) and Phases 4–8 (Application Shell, Widget UI, Calendar Grid & Navigation, Persistence, Events Management) are complete. The application has a Generic Host composition root, DI container, explicit application lifecycle, bidirectional window switching, month navigation, 42-cell deterministic grid rendering, real-time clock, navigation placeholders, an EF Core SQLite persistence layer, and full Event Management (CRUD UI, modal dialogs, day detail panel, domain validation, and 42-cell event indicator loading). The next phase is Phase 9 (Notes Management).
