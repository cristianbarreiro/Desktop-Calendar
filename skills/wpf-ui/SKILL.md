---
name: wpf-ui
description: Best practices for XAML design, MVVM bindings, theming, and accessibility in WPF.
---

# WPF UI Decision Aid

Use for XAML, view models, themes, accessibility, or WPF lifecycle changes. Load relevant design-system and interaction guidance only.

## Choose the Owner

- Keep view state and commands in Presentation view models; reserve code-behind for view-specific interaction such as focus or dragging.
- Keep persistence and EF Core behind Core contracts and Infrastructure services.
- Follow nearby resource dictionaries and theme behavior; avoid a new styling pattern without a concrete need.
- Give interactive controls accessible names and preserve keyboard navigation.

For WPF tests, account for process-wide `Application.Current`, STA thread, and Dispatcher state. Exercise lifecycle isolation in test helpers; do not add production workarounds for test-host initialization problems. Validate XAML with a build and test affected interactions.
