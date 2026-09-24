---
name: wpf-ui
description: Best practices for XAML design, MVVM bindings, theming, and accessibility in WPF.
---

# WPF UI Skill

## Purpose
Guide the development of responsive, accessible, and elegant WPF interfaces in `CalendarWidget.Presentation` and `CalendarWidget.App`.

## When to Use
- Creating or editing XAML views, user controls, styles, or templates.
- Writing ViewModels with `CommunityToolkit.Mvvm`.
- Adjusting color palettes, themes, and animations.

## Prerequisites
- Review `docs/ui/design-system.md` and `docs/ui/interaction-rules.md`.

## Workflow
1. Inherit ViewModels from `ObservableObject`.
2. Use `[ObservableProperty]` and `[RelayCommand]` source generators.
3. Keep code-behind files (`.xaml.cs`) minimal—restricted to view-only logic (focus, window dragging).
4. Organize styles into resource dictionaries under `Presentation/Themes/`.
5. Provide `AutomationProperties.Name` on all interactive elements.

## Constraints
- No business logic inside View code-behind.
- No direct database access or EF Core references in presentation layer.

## Validation
- Verify XAML compiles cleanly during `dotnet build`.
- Verify key navigation and tab traversal.
