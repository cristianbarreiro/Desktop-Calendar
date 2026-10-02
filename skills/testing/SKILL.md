---
name: testing
description: Automated test authoring guidelines using xUnit and FluentAssertions for CalendarWidget.
---

# Testing & Validation Decision Aid

Use tests to protect observable behavior. Choose the least expensive evidence that can establish the contract, then expand with risk:

- Pure domain and transformation rules: `CalendarWidget.UnitTests`.
- Persistence, WPF lifecycle, or OS behavior: `CalendarWidget.IntegrationTests`; isolate external state with temporary resources.
- Cross-service races: coordinate actors with barriers, gates, or explicit seams, then assert preserved state. Do not depend on sleeps or scheduler luck.

## Choose Validation Scope

- Start with the focused test or targeted regression for the changed contract.
- Run the full Release suite when shared state, persistence, lifecycle, project boundaries, or broad behavior changes.
- Repeat the full suite when validating an intermittent/concurrency failure or when the diagnosis depends on stability across runs.
- Use hang diagnostics (including blame-hang collection) when a test host stalls; inspect the dump/log and last test before changing code.
- Run `dotnet format CalendarWidget.slnx --verify-no-changes` for the repository gate and check CI for the commit being reported. A previous green run is not evidence for a later SHA.

For the canonical full-suite invocation, follow `.github/workflows/ci.yml`; add `--blame-hang-timeout 5m` only when diagnosing a hang. Use `--no-build` only when the relevant Release outputs were just built.

Name tests `Method_Condition_ExpectedResult` and keep setup, action, and assertions clear. Keep assertions meaningful; do not skip tests, weaken them, or disable parallelism to hide a failure. Reproduce failures narrowly, then expand only as the affected risk requires. Consult [`skills/failure-investigation/SKILL.md`](../failure-investigation/SKILL.md) when evidence is incomplete or incidents may be unrelated.
