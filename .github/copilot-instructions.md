# GitHub Copilot Repository Instructions

Desktop Calendar Widget is a modern Windows desktop utility built on C# / .NET 10, WPF, EF Core, and SQLite.

## Golden Rules
1. **Canonical Contract**: Always adhere to [AGENTS.md](../AGENTS.md).
2. **Layering Boundaries**:
   - `Core`: Pure domain logic, no dependencies on WPF or EF Core.
   - `Infrastructure`: EF Core and SQLite; implements interfaces from `Core`.
   - `Presentation`: ViewModels and XAML; depends on `Core` and `CommunityToolkit.Mvvm`. Never references `Infrastructure`.
   - `App`: Composition root.
3. **Coding Standards**:
   - File-scoped namespaces, `sealed` classes by default, nullable reference types enabled.
   - XML documentation on all public members.
   - Use `xUnit` + `FluentAssertions` with naming `MethodName_Condition_ExpectedResult`.
4. **Local First**:
   - SQLite persistence via `%LOCALAPPDATA%`.
   - No telemetry, no unsolicited external network calls.
