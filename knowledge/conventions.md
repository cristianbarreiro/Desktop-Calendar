# Coding Conventions & Standards

## C# Language Conventions

- **Namespaces**: Use file-scoped namespaces (`namespace CalendarWidget.Core.Entities;`).
- **Constructors**: Prefer primary constructors or explicit constructors with `readonly` fields.
- **Classes**: Mark classes `sealed` by default unless explicitly designed for inheritance.
- **Records**: Use `record` or `record struct` for immutable value objects and DTOs.
- **Nullability**: Nullable reference types are enabled across all projects (`#nullable enable`).
- **Variables**: Use explicit typing when the right-hand side is not completely obvious (avoid ambiguous `var`).
- **Documentation**: All public API members must have XML doc comments (`/// <summary>`).

## XAML Conventions

- Group resources by theme dictionary.
- Always provide explicit `AutomationProperties.Name` for controls without text content (e.g. icon-only buttons).
- Use `StaticResource` for unchanging theme brushes and styles; use `DynamicResource` only when theme switching at runtime.
- Maintain consistent indentation (4 spaces).

## Testing Conventions
- Use `xUnit` and `FluentAssertions`.
- Method naming pattern: `MethodName_Condition_ExpectedResult`.
- Arrange-Act-Assert (AAA) pattern strictly separated with whitespace.
