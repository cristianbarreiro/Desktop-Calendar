# Test Generation Prompt

Generate automated tests for a component in Desktop Calendar Widget:

1. **Target Selection**:
   - For domain entities/services/rules: Target `tests/CalendarWidget.UnitTests/Core/`.
   - For ViewModels: Target `tests/CalendarWidget.UnitTests/Presentation/`.
   - For SQLite repositories/DbContext: Target `tests/CalendarWidget.IntegrationTests/Persistence/`.
2. **Test Naming Standard**:
   - Format: `[MethodName]_[Condition]_[ExpectedResult]`
3. **Required Scenarios**:
   - Happy path (valid inputs).
   - Boundary values (empty title, max title length, edge dates, leap years).
   - Error cases (start time after end time, null parameters).
4. **Structure**:
   - Arrange, Act, Assert with blank line separators.
   - Use `FluentAssertions` methods (`.Should().Be(...)`, `.Should().Throw<...>()`).
