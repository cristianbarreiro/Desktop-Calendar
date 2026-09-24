---
name: dotnet
description: Standard workflows, coding conventions, and CLI operations for .NET 10 and C# 14.
---

# .NET Skill

## Purpose
Maintain high-quality C# code conforming to Microsoft and project standards.

## When to Use
- Writing or editing any `.cs` or `.csproj` files.
- Adding NuGet packages.
- Executing .NET CLI commands.

## Prerequisites
- .NET 10 SDK installed (`dotnet --version`).

## Workflow
1. Use file-scoped namespaces.
2. Mark classes `sealed` by default unless extension is intended.
3. Keep nullable reference types satisfied without blindly using `!`.
4. Prefer explicit types over `var` when type is not apparent.
5. Add XML docs (`///`) to public methods and types.
6. Use `dotnet format` to enforce formatting.

## Constraints
- Do not disable compiler warnings in code without justification.
- Do not add packages without justifying why an existing BCL feature is insufficient.

## Validation
```bash
dotnet build
dotnet format --verify-no-changes
```
