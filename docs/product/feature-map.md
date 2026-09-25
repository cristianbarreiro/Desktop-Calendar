# Feature Map

## Application Modes

```
Desktop Calendar Widget
├── Widget Mode (compact) 🟢 [Shell Container]
│   ├── Calendar Grid 🟡 [Shell Grid]
│   ├── Current Time 🟢
│   ├── Day Selection + Detail 🟡 [Shell Selection / Tray Toggle]
│   ├── Event Indicators 🔴
│   ├── Open App Button 🟢
│   └── Minimize / Close Control 🟢
│
└── Full Application Mode (window) 🟢 [Shell Container]
    ├── Calendar 🟡 [Shell Navigation]
    │   ├── Month View 🟢 [42-cell Shell]
    │   ├── Month Navigation 🟢
    │   ├── Day Selection 🟢
    │   ├── Event Management (CRUD) 🔴
    │   └── Day Detail Panel 🟡 [Shell Summary]
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

Engineering Foundation (Phases 0–3) and Phase 4 (Application Shell) are complete. The application shell provides Generic Host composition root, DI container, explicit application lifecycle with coordinated shutdown, bidirectional window switching between compact widget and main application, month navigation, 42-cell deterministic grid rendering, real-time clock, and navigation placeholders. The next phase is Phase 5 (Widget UI).
