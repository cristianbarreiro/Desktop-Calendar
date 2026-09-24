# Architecture Review Prompt

Review the proposed code or architectural change against the system boundaries:

1. **Verify Layer Invariants**:
   - `Core`: Does it remain zero-dependency and independent of WPF/EF Core?
   - `Presentation`: Does it depend only on `Core`? Does it avoid references to `Infrastructure`?
   - `Infrastructure`: Does it cleanly implement interfaces defined in `Core`?
2. **Review Extensibility**:
   - Can future features (e.g. recurrence, reminders, categories) be introduced without refactoring the core domain?
3. **Assess Complexity**:
   - Are there speculative abstractions or unnecessary patterns (e.g. MediatR, CQRS)?
4. **Identify ADR Need**:
   - Does this decision warrant a new Architecture Decision Record in `docs/adr/`?
