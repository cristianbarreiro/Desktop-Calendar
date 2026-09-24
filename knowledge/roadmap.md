# Implementation Roadmap

## Phases Breakdown

- [x] **Phase 0: Repository + AI context infrastructure**
  - Canonical contracts, multi-agent files, documentation tree, skills, knowledge base.
- [x] **Phase 1: Solution + projects + dependency boundaries**
  - .NET 10 solution, 6 projects, references, packages, initial domain stubs and smoke tests.
- [ ] **Phase 2: Application shell + window management**
  - `IWindowManager`, window transition between Widget and Main Window, Host configuration.
- [ ] **Phase 3: Widget UI**
  - Calendar grid control, date calculations, day selection, expandable day detail tray.
- [ ] **Phase 4: Calendar domain**
  - `DateRange`, `TimeRange`, full domain validation rules, unit tests.
- [ ] **Phase 5: SQLite persistence**
  - Concrete repositories, EF Core SQLite migrations, startup schema initialization.
- [ ] **Phase 6: Events**
  - Event CRUD UI, indicators in month grid, day detail view.
- [ ] **Phase 7: Notes**
  - Note list, note editor, search, timestamps.
- [ ] **Phase 8: Settings**
  - Dark/Light theme switching, widget behavior, date/time format customization.
- [ ] **Phase 9: Windows integration**
  - System tray icon, minimize to tray, auto-start with Windows, multi-monitor DPI validation.
- [ ] **Phase 10: Testing + polish**
  - Full test coverage, visual regression, accessibility checks.
- [ ] **Phase 11: Packaging + release**
  - MSIX / single-file self-contained deployment.
