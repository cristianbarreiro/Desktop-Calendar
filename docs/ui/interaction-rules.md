# Interaction Rules

## Widget Interactions

### Day Cell States
- **Normal**: Transparent background, text primary color.
- **Current Day ("Today")**: Accent border (1px solid `#0078D4`) with bold text.
- **Hover**: Subtle background tint (`#2D2D30` dark, `#EAEAEA` light) with 100ms transition.
- **Selected**: Accent background (`#0078D4`), high-contrast text (`#FFFFFF`).
- **Has Events**: Small circular badge/dot (4px diameter) centered directly below the date number.
- **Outside Current Month**: Text disabled color (`#858585`), opacity 0.5.

### Expand / Collapse Day Detail Tray
- Selecting a day animates height expansion (180ms ease-out).
- Clicking the selected day again toggles collapse back to compact state.
- Keyboard navigation (Arrow keys) selects adjacent days and dynamically updates the tray.

## Window Management Interactions
- **Drag & Drop**: Entire title area of widget serves as drag handle for window repositioning.
- **Switch to Full App**: Single click on `[APP]` header button smoothly transitions to main application view.
- **Switch to Widget**: Clicking `[WIDGET]` from main app minimizes or docks back to widget mode.
- **Escape Key**: Dismisses open popups, date pickers, or active day expansion in widget.
