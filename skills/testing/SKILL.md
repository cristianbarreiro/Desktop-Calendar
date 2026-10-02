---
name: testing
description: Automated test authoring guidelines using xUnit and FluentAssertions for CalendarWidget.
---

# Testing Decision Aid

Use tests to protect observable behavior. Choose the smallest layer that can establish the contract:

- Pure domain and transformation rules: `CalendarWidget.UnitTests`.
- Persistence, WPF lifecycle, or OS behavior: `CalendarWidget.IntegrationTests`; isolate external state with temporary resources.
- Cross-service races: coordinate actors with barriers, gates, or explicit seams, then assert preserved state. Do not depend on sleeps or scheduler luck.

Name tests `Method_Condition_ExpectedResult` and keep setup, action, and assertions clear. Keep assertions meaningful; do not skip tests, weaken them, or disable parallelism to hide a failure. Reproduce failures narrowly, then expand to the suite and CI as risk requires.
