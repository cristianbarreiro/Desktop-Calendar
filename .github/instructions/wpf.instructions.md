---
applyTo: "src/CalendarWidget.Presentation/**/*, src/CalendarWidget.App/**/*"
---

# WPF Instructions

- ViewModels inherit from `CommunityToolkit.Mvvm.ComponentModel.ObservableObject`.
- Use `[ObservableProperty]` and `[RelayCommand]` attributes.
- Avoid code in `.xaml.cs` other than direct visual or focus manipulation.
- Never instantiate or query `AppDbContext` inside Presentation.
- Provide `AutomationProperties.Name` on buttons and icon controls.
