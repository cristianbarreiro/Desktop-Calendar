# Known Issues & Risks

## Active Issues
*No known functional defects at this stage.*

## Architectural Risks & Mitigations

1. **WPF and Modern High-DPI Displays**:
   - *Risk*: Blurry text or improper window coordinates on mixed-DPI multi-monitor environments.
   - *Mitigation*: Ensure `app.manifest` enables Per-Monitor V2 DPI awareness and use device-independent units.

2. **Single Instance & Widget/App Hand-off**:
   - *Risk*: Launching the app multiple times creating conflicting SQLite file locks.
   - *Mitigation*: Implement a named OS Mutex in `Program.cs` and pass messages to the existing active instance.

3. **Database Concurrency in SQLite**:
   - *Risk*: `database is locked` errors during concurrent reads and writes.
   - *Mitigation*: Enable SQLite WAL mode (`PRAGMA journal_mode=WAL;`) during database initialization.
