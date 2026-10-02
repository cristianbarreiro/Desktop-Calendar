---
name: dotnet
description: Standard workflows, coding conventions, and CLI operations for .NET 10 and C# 14.
---

# .NET Workflow Aid

Use for C#/.NET changes and CLI choices. Confirm target framework and installed SDK; this project targets .NET 10 and enables nullable reference types.

## Style Decisions

- Follow nearby code: file-scoped namespaces, explicit types when inference is unclear, and `sealed` for non-extensible classes.
- Preserve nullable contracts and document public APIs.
- Prefer the BCL or existing dependencies; explain a concrete need before adding a package.
- Do not suppress warnings or alter project configuration just to obtain a clean build.

## Validation Choices

Build the affected project for a local change; use the Release solution build/tests and `dotnet format --verify-no-changes` when the change or CI gate warrants it. Avoid concurrent builds against the same checkout outputs.
