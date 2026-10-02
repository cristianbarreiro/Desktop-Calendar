# Failure Memory

Durable, evidence-backed incident lessons. This is not a log; current test/CI status belongs in [`docs/project-state.md`](../docs/project-state.md). Separate unrelated failures and label uncertainty explicitly.

## Settings File Replacement — Resolved

- **Failure / evidence:** `ConcurrentSettingsAndPlacement_DeterministicGating_PreservesBothStreams` reported `UnauthorizedAccessException` during atomic replacement of the shared settings file while Settings and window Placement writes overlapped.
- **Reproduction status:** Historically observed and confirmed as a persistence-concurrency defect.
- **Root cause:** Repository instances targeting one path had only instance-local coordination and used a shared temporary filename. Concurrent writers could collide during replacement.
- **Fix:** `466dc28` added normalized-path coordination and unique same-directory temp files. `395b4b3` added stable-backup recovery after an interrupted replacement. The existing implementation preserves the last valid file and serializes access by path.
- **Regression:** Multi-repository same-path saves, `ConcurrentSettingsAndPlacement_DeterministicGating_PreservesBothStreams`, replacement cleanup, and interrupted-save recovery are covered.
- **Validation:** Regressions and the full Release suite passed; current verified counts and CI SHA are maintained in Project State.
- **Prevention:** Preserve path-level coordination, unique temporaries, atomic replacement, backup recovery, and latest-state behavior. Use explicit gates in tests; do not add sleeps, blind retries, exception suppression, or shared temp paths.
- **Status:** Resolved.

## NotesView Test STA Timeout — Historical / Non-reproduced

- **Failure / evidence:** `NotesView_WhenInstantiatedDirectly_InitializesWithoutException` timed out in `WpfTestContext.EnsureStaThreadStarted()` while constructing the test context, before the NotesView action or assertion ran.
- **Reproduction status:** Not reproduced in subsequent full Release validation; the same regression and broader WPF tests later passed.
- **Root cause:** Unconfirmed. The observed failure location is test STA/Dispatcher initialization; the exact stage and trigger that exceeded the wait are unknown. It is not evidence of a NotesView defect or of the persistence incident.
- **Fix:** No definitive incident-specific fix is established. Test-harness lifecycle changes in `44e4062` and `0f0cb29` addressed WPF dispatcher lifecycle/cleanup; evidence does not prove they caused this timeout to stop recurring. No production workaround was added.
- **Regression:** The direct NotesView initialization regression and WPF test lifecycle/isolation tests.
- **Validation:** Focused NotesView/WPF lifecycle tests and later full Release runs passed. See Project State for the current validation snapshot.
- **Remaining uncertainty / prevention:** Keep production WPF lifecycle separate from test STA ownership, Dispatcher pumping, and cleanup. Diagnose the failing stage and collect hang evidence before changing code; do not infer a root cause from non-reproduction or use sleeps/blind retries.
- **Status:** Historical / non-reproduced / trigger unconfirmed.
