# Architectural Invariants & Patterns

## Core Invariants

1. **Dependency Direction**:
   - `Core` is the center of the application. It knows nothing about databases, Windows APIs, XAML, or UI.
   - `Infrastructure` implements contracts from `Core`.
   - `Presentation` depends solely on `Core`. It never accesses `Infrastructure` directly.
   - `App` is the only project that links all layers together into an executable bundle.

2. **Persistence Boundary**:
   - All persistence logic is encapsulated behind repository interfaces (`ICalendarEventRepository`, `INoteRepository`).
   - The UI communicates with persistence only via these repository contracts or dedicated domain services.
   - EF Core `DbContext` must never leak into ViewModels.

3. **Window Model**:
   - The widget and the full application are two presentation views of the same state and persistence backing.
   - Both can coexist or toggle based on user preference, with shared event dispatching.
