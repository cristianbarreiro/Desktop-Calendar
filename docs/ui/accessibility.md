# Accessibility Guidelines

## Compliance Baseline

The application targets WCAG 2.1 AA accessibility standards for desktop applications.

## Keyboard Navigation

1. **Tab Traversal**:
   - Logical tab order: Mode Switcher -> Minimize -> Prev Month -> Next Month -> Calendar Grid -> Event List.
2. **Calendar Grid Navigation**:
   - `Left Arrow`: Previous day
   - `Right Arrow`: Next day
   - `Up Arrow`: Previous week (-7 days)
   - `Down Arrow`: Next week (+7 days)
   - `Home`: First day of month
   - `End`: Last day of month
   - `PageUp`: Previous month
   - `PageDown`: Next month
   - `Enter` / `Space`: Select highlighted day and expand detail view.

## Visual Accessibility
- **Contrast Ratio**: Minimum 4.5:1 for normal text and 3:1 for large text/icons against backgrounds.
- **Focus Indicators**: 2px high-visibility focus ring (dashed or solid contrasting accent) on all focused elements. Focus states are never removed via styles.
- **Reduced Motion**: Respects `SystemParameters.ClientAreaAnimation` and Windows Accessibility "Show animations in Windows" setting.
- **Screen Reader Support**: All interactive XAML controls must provide meaningful `AutomationProperties.Name` and `AutomationProperties.HelpText`.
