# Bug Investigation Prompt

Investigate and resolve a bug in Desktop Calendar Widget:

1. **Reproduce & Isolate**:
   - Determine whether the failure originates in Domain logic, Persistence/SQLite, UI DataBinding, or OS integration.
2. **Write a Failing Test**:
   - Before writing any fix, write a test in `UnitTests` or `IntegrationTests` that reproduces the exact defect.
   - Run `dotnet test` and confirm it fails.
3. **Apply Minimal Fix**:
   - Make the smallest coherent change to fix the root cause.
   - Do not perform unrelated refactoring.
4. **Validate**:
   - Run `dotnet test` to confirm the test now passes and no regressions were introduced.
   - Run `dotnet format --verify-no-changes`.
