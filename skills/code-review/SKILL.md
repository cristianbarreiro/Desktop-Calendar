---
name: code-review
description: Comprehensive review checklist for architecture, security, performance, and code quality.
---

# Code Review Skill

## Purpose
Systematically review code changes before completing a task or submitting a pull request.

## When to Use
- Prior to finalizing any non-trivial code modifications.
- When validating contributions across architectural layers.

## Checklist
1. **Architecture Boundaries**:
   - Did any UI code bleed into Core?
   - Did Presentation reference Infrastructure?
2. **Quality & Standards**:
   - Are public members documented with XML comments?
   - Are classes marked `sealed` where appropriate?
   - Is formatting compliant (`dotnet format --verify-no-changes`)?
3. **Tests**:
   - Are new business rules backed by automated tests?
   - Do all unit and integration tests pass (`dotnet test`)?
4. **Security & Privacy**:
   - Are connection strings or secrets committed? (Must be NO)
   - Is user input validated properly?
   - Are there any unexpected network calls? (Must be NO)

## Validation
Execute:
```bash
dotnet build
dotnet test
dotnet format --verify-no-changes
```
