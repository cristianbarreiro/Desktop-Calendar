---
name: wpf-ui
description: Best practices for XAML design, MVVM bindings, theming, and accessibility in WPF.
---

# WPF UI & STA Decision Aid

Use for XAML, view models, themes, accessibility, or WPF lifecycle changes. Load only the relevant design-system or interaction source. For a test timeout, first determine whether it occurs in application startup/lifecycle or in the test STA/Dispatcher harness.

## Choose the Owner

- Keep view state and commands in Presentation view models; reserve code-behind for view-specific interaction such as focus or dragging.
- Keep persistence and EF Core behind Core contracts and Infrastructure services.
- Follow nearby resource dictionaries and theme behavior; avoid a new styling pattern without a concrete need.
- Give interactive controls accessible names and preserve keyboard navigation.

## Test STA and Dispatcher

- Identify which thread owns `Application.Current`, the Dispatcher, and teardown; distinguish fixture setup/cleanup from the view or assertion under test.
- Isolate process-wide WPF state in test infrastructure, and validate initialization, ownership handoff, pumping, shutdown, and cleanup.
- Use explicit state/signals and bounded diagnostics. Do not add sleeps, blind retries, or production workarounds for a harness-only timeout.
- For the historical `NotesView` timeout, use `knowledge/failure-memory.md`: initialization timed out before the assertion; its trigger was not confirmed and it did not recur in later full-suite validation.

Validate affected UI/lifecycle tests first, then scale to the full suite when shared dispatcher state is involved. Do not present a historical non-reproduction as proof of a root cause.
