# Code Review Prompt

Perform a comprehensive code review of the recent changes:

1. **Verify Canonical Rules in AGENTS.md**:
   - Are classes marked `sealed`?
   - Are file-scoped namespaces used?
   - Is `var` used only when types are clear?
   - Are XML doc comments present on public APIs?
2. **Check Layer Boundaries**:
   - Confirm no WPF types in `Core` or `Infrastructure`.
   - Confirm no direct EF Core usage in `Presentation`.
3. **Inspect Tests**:
   - Do tests adhere to AAA format and `MethodName_Condition_ExpectedResult`?
   - Are assertions using FluentAssertions?
4. **Run Verification Commands**:
   - `dotnet build`
   - `dotnet test`
   - `dotnet format --verify-no-changes`
