# Failure Memory

This is a compact record of verified incidents and useful diagnostics. Consult it when a task matches; do not treat a past fix as a universal prescription.

## File Settings Replacement — 2026-10-02

- **Evidence**: the reported `ConcurrentSettingsAndPlacement_DeterministicGating_PreservesBothStreams` failure was `UnauthorizedAccessException` in the file replacement path. The earlier implementation had an instance-only semaphore and one shared `.tmp` name, so separate repository objects targeting the same path could collide. Commit `466dc28` changed synchronization to a normalized path-level gate and made temp names unique. The full pre-fix suite passed locally once, so the original CI failure was not independently reproduced on this machine; the source-level race is the supported diagnosis, not a claim that this exact CI schedule was replayed.
- **Fix**: `FileSettingsRepository` now gates operations by normalized full path, uses a unique same-directory temp file, and restores a stable `.bak` on the next load if the process stops after moving the old file aside. Replacement, cleanup, and recovery run under the path gate.
- **Regression evidence**: multi-repository saves, concurrent Settings/Placement, successful replacement cleanup, and interrupted-save recovery have integration coverage.
- **Avoid**: timing sleeps, unbounded retries, suppressing access exceptions, fixed shared temp names, or inferring crash safety from a passing concurrency test alone.

## WPF Test STA Initialization — 2026-10-02

- **Evidence**: CI run #44 failed `NotesView_WhenInstantiatedDirectly_InitializesWithoutException` in `WpfTestContext.EnsureStaThreadStarted()` during fixture construction, before the NotesView assertion. CI run #45 passed the same WPF code; two full local Release runs also passed. This does not implicate NotesView, and the runner-specific trigger was not reproduced.
- **Cause and remediation**: the failure is in the test-host STA/Dispatcher startup path. WPF `Application.Current`, its STA thread, and Dispatcher are process-wide, so startup, ownership, shutdown, and recreation need coordinated lifecycle state. The helper has that coordination. The timeout is 10 seconds; evidence is consistent with a slow or starved initialization on the hosted runner, but does not isolate which startup stage exceeded the limit. Keep any further fix in test infrastructure, not production UI.
- **Regression evidence**: focused NotesView initialization, WPF lifecycle tests, both local full Release runs, and CI #45 passed.

## Current Verification Snapshot

- Windows, .NET SDK `10.0.302`; 421 tests (326 unit, 95 integration).
- The full Release suite passed twice locally; focused regressions and format verification passed.
- GitHub Actions for `395b4b3` passed build, tests, and format verification.
- Existing release remains `v1.0.0`; this snapshot does not imply a new release.
