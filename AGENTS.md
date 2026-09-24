# AGENTS.md — Desktop Calendar Widget

> Canonical agent contract. All AI agents must read this file before making changes.

## Project Identity

- **Name**: Desktop Calendar Widget
- **Platform**: Windows
- **Language**: C# / .NET 10
- **UI Framework**: WPF (XAML)
- **Architecture**: MVVM + Layered Architecture
- **Storage**: SQLite via Entity Framework Core
- **Status**: Phase 4 Complete — Ready for Phase 5: Widget UI

## Architecture

```
src/
  CalendarWidget.App/            → Application host, DI, startup
  CalendarWidget.Core/           → Domain entities, interfaces, rules
  CalendarWidget.Infrastructure/ → EF Core, SQLite, OS integrations
  CalendarWidget.Presentation/   → ViewModels, Views, XAML, themes
tests/
  CalendarWidget.UnitTests/      → xUnit unit tests
  CalendarWidget.IntegrationTests/ → Integration tests
```

### Dependency Direction (strict)

```
Presentation → Core
App → Core, Infrastructure, Presentation
Infrastructure → Core
Tests → All (for testing only)
```

**Violations are blocking.**

- Core MUST NOT reference Infrastructure, Presentation, or App.
- Presentation MUST NOT reference Infrastructure directly.
- Infrastructure MUST NOT reference Presentation.
- UI MUST NOT access EF Core or persistence logic directly.
- Domain MUST NOT depend on WPF.

## Commands

```bash
# Restore
dotnet restore

# Build
dotnet build

# Test
dotnet test

# Format check
dotnet format --verify-no-changes

# Format fix
dotnet format

# Run application
dotnet run --project src/CalendarWidget.App
```

## Coding Conventions

- Use file-scoped namespaces
- Use primary constructors where appropriate
- Use `readonly` and `sealed` by default
- Prefer records for DTOs and value objects
- Use nullable reference types (enabled project-wide)
- Follow Microsoft C# coding conventions
- XML doc comments on all public API surfaces
- No `var` when the type is not obvious from the right-hand side

## Dependency Rules

- Every NuGet dependency must have a concrete justification
- Prefer official Microsoft libraries
- No MediatR, no AutoMapper, no unnecessary abstractions
- CommunityToolkit.Mvvm for MVVM infrastructure
- Microsoft.Extensions.DependencyInjection for DI
- Microsoft.EntityFrameworkCore.Sqlite for persistence

## Testing Rules

- Every meaningful business rule must have automated tests
- Use xUnit + FluentAssertions
- Test naming: `MethodName_Condition_ExpectedResult`
- Do not test framework behavior
- Do not create tests that merely assert constructor assignment

## Security Rules

- Never commit secrets or credentials
- Never hardcode connection strings or API keys
- Never log sensitive user data
- Never introduce telemetry without explicit requirements
- Never execute arbitrary downloaded code
- Validate all external input

## Development Workflow

1. Read this file and relevant documentation
2. Identify affected architecture layer
3. Make the smallest coherent change
4. Run `dotnet build` — must succeed
5. Run `dotnet test` — must pass
6. Run `dotnet format --verify-no-changes` — must pass
7. Update documentation if behavior/architecture changed
8. Report validation results honestly

## Definition of Done

- [ ] Implementation exists and compiles
- [ ] Architecture boundaries respected
- [ ] Tests added/updated where appropriate
- [ ] All tests pass
- [ ] Formatting passes
- [ ] No unnecessary warnings introduced
- [ ] Documentation updated if needed
- [ ] No secrets introduced
- [ ] No unrelated files changed

## Prohibited Behavior

- Do NOT invent requirements or features
- Do NOT delete working code without justification
- Do NOT rewrite entire files unnecessarily
- Do NOT modify unrelated modules
- Do NOT disable tests to make CI pass
- Do NOT hide or suppress errors
- Do NOT create speculative abstractions
- Do NOT introduce network calls without explicit requirements

## Commit Convention

Use [Conventional Commits](https://www.conventionalcommits.org/):
`feat:`, `fix:`, `refactor:`, `docs:`, `test:`, `build:`, `chore:`, `perf:`, `style:`

## Documentation Hierarchy

| Topic | Location |
|-------|----------|
| Product requirements | `/docs/product/` |
| Architecture | `/docs/architecture/` |
| UI/UX | `/docs/ui/` |
| ADRs | `/docs/adr/` |
| Project state | `/docs/project-state.md` |
| Knowledge base | `/knowledge/` |
| Agent skills | `/skills/` |

## Context Loading Strategy

**Always load**: This file (AGENTS.md)

**Load on demand** (only when relevant to the task):
- `/docs/architecture/` — for architectural changes
- `/docs/ui/` — for UI work
- `/docs/product/` — for feature planning
- `/knowledge/domain.md` — for domain logic
- `/skills/` — for specialized workflows
- `/docs/adr/` — when making architectural decisions
