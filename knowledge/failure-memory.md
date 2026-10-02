# Failure Memory

This is a compact record of verified incidents and useful diagnostics. Consult it when a task matches; do not treat a past fix as a universal prescription.

## File Settings Replacement — 2026-10-02

- **Evidence**: `ConcurrentSettingsAndPlacement_DeterministicGating_PreservesBothStreams` reported `UnauthorizedAccessException` in the file replacement path. The earlier repository synchronized only per instance and reused one `.tmp` path, leaving writers from separate repository instances without a shared ownership boundary and able to collide on that temporary file.
- **Fix**: `FileSettingsRepository` now gates operations by normalized full path, uses a unique same-directory temp file, and restores a stable `.bak` on the next load if the process stops after moving the old file aside. Replacement, cleanup, and recovery run under the path gate.
- **Regression evidence**: multi-repository saves, concurrent Settings/Placement, successful replacement cleanup, and interrupted-save recovery have integration coverage.
- **Avoid**: timing sleeps, unbounded retries, suppressing access exceptions, fixed shared temp names, or inferring crash safety from a passing concurrency test alone.

## WPF Test STA Initialization — 2026-10-02

- **Evidence**: `NotesView_WhenInstantiatedDirectly_InitializesWithoutException` timed out in `WpfTestContext.EnsureStaThreadStarted()` while constructing the test context, before its NotesView assertion ran. The stack locates the failure in test-host STA/Dispatcher initialization, not in NotesView production behavior.
- **Cause and remediation**: WPF `Application.Current`, its STA thread, and Dispatcher are process-wide. Test startup, active ownership, shutdown, and recreation need one coordinated lifecycle. The integration harness now owns that synchronization; keep this isolation in test infrastructure instead of adding production UI workarounds.
- **Regression evidence**: NotesView initialization and WPF lifecycle tests pass in Release and CI at `395b4b3`.

## Current Verification Snapshot

- Windows, .NET SDK `10.0.302`; 421 tests (326 unit, 95 integration).
- The full Release suite passed twice locally; focused regressions and format verification passed.
- GitHub Actions for `395b4b3` passed build, tests, and format verification.
- Existing release remains `v1.0.0`; this snapshot does not imply a new release.
