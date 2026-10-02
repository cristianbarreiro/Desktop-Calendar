---
name: failure-investigation
description: Evidence-led triage for intermittent, concurrency, lifecycle, and test-host failures.
---

# Failure Investigation Decision Aid

Use when an error is intermittent, a failure occurs before the assertion under investigation, or multiple failures may be conflated. Load the narrowest test, implementation, CI log, and relevant entry in `knowledge/failure-memory.md`.

## Establish What Is Known

Record only the concise evidence needed to distinguish:

1. **Observed:** exact failing test, exception, stack location, and whether the test body started.
2. **Reproduction:** where and how often it occurred; distinguish a historical CI report from local reproduction.
3. **Hypothesis:** plausible mechanism, explicitly marked unconfirmed.
4. **Confirmed cause:** supported by source/test evidence or a deterministic reproduction—not timing correlation alone.
5. **Fix and regression:** the changed owner and test that would fail without the fix.
6. **Validation and uncertainty:** focused/full suite or CI evidence, plus what remains unknown.

## Choose the Next Action

- Reproduce narrowly and inspect the owning code before editing. If the failure occurs during fixture setup, do not attribute it to the test body.
- For concurrency, find shared resources and ordering contracts; use gates/barriers to make interleavings observable.
- For hangs, collect bounded hang diagnostics and identify the last active test/thread; do not add sleeps or blind retries.
- Separate concurrent incidents unless evidence connects them. Keep production, persistence, and test-harness lifecycles distinct.
- If cause remains unconfirmed, report the uncertainty and stop short of a speculative production change. Add Failure Memory only when the lesson is likely to prevent recurrence.

Use `skills/testing/SKILL.md` to select validation scope. Failure Memory owns incident history; this skill owns the investigation workflow.
