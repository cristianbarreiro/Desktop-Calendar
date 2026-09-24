# UI Review Prompt

Review user interface components against the project design system:

1. **Visual Restraint**:
   - Does the design align with modern Windows Fluent design?
   - Is it free of excessive glassmorphism, heavy gradients, or unnecessary glow?
2. **Design Tokens**:
   - Are colors drawn from the palette defined in `docs/ui/design-system.md`?
   - Is the Segoe UI Variable font used?
   - Are padding and spacing on the 4px base grid?
3. **Accessibility**:
   - Is keyboard focus indicator visible (2px minimum)?
   - Can the component be navigated fully via keyboard (Tab, Arrows)?
   - Are `AutomationProperties.Name` set for screen reader compatibility?
   - Does text meet minimum 4.5:1 contrast ratio?
4. **State Handling**:
   - Are normal, hover, pressed, selected, and disabled states properly defined?
