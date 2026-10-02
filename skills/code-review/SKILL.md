---
name: code-review
description: Comprehensive review checklist for architecture, security, performance, and code quality.
---

# Code Review Decision Aid

Use for meaningful changes or when another layer's contract is affected. Scale review to the diff and its risk.

## Review Questions

- Does the change preserve project boundaries and existing ownership?
- Are failure paths, cancellation, cleanup, and user-data effects handled?
- Are inputs validated and secrets absent? Does the diff introduce network activity?
- Do tests assert required behavior and fail deterministically?
- Are public APIs documented and formatting consistent where relevant?

## Evidence

Inspect the diff and run the narrowest relevant build/tests first. Run full Release tests and `dotnet format --verify-no-changes` when the change or repository gate warrants it. Report commands and outcomes; do not infer success from inspection.
