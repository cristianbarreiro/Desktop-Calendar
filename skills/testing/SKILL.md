---
name: testing
description: Automated test authoring guidelines using xUnit and FluentAssertions for CalendarWidget.
---

# Testing Skill

## Purpose
Ensure all business rules, date calculations, ViewModels, and persistence logic are thoroughly covered by automated tests.

## When to Use
- Adding or modifying business logic in `Core`.
- Implementing persistence repositories in `Infrastructure`.
- Creating ViewModels in `Presentation`.

## Prerequisites
- Familiarity with `xUnit` and `FluentAssertions`.

## Workflow
1. Place unit tests in `tests/CalendarWidget.UnitTests/`.
2. Place integration tests (database, OS) in `tests/CalendarWidget.IntegrationTests/`.
3. Follow the naming pattern: `MethodName_Condition_ExpectedResult`.
4. Structure tests using AAA (Arrange, Act, Assert).
5. Prefer in-memory database providers or clean isolated SQLite instances for integration tests.

## Constraints
- Do not test framework behavior (e.g. testing that `int x = 5` sets `x` to `5`).
- Never disable existing tests to bypass build failures.

## Validation
```bash
dotnet test
```
