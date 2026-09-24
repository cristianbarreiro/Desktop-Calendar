# UI Design System

## Design Philosophy

The UI adheres to modern Windows Fluent Design principles with minimal visual noise:
- **Restrained & Professional**: Avoid excessive glassmorphism, heavy gradients, or glowing neon accents.
- **Content First**: High typographic legibility, balanced whitespace, and purposeful micro-interactions.
- **Native Harmony**: Feels at home alongside Windows 11 desktop applications.

## Typography

- **Font Family**: `Segoe UI Variable Text`, fallback `Segoe UI`, `sans-serif`.
- **Scale**:
  - `Header Large`: 20pt / Semibold (Month & Year display)
  - `Header Medium`: 14pt / Semibold (Section headers, Today summary)
  - `Body Regular`: 12pt / Normal (Calendar numbers, event titles)
  - `Body Subtle`: 10pt / Normal (Weekday headers, timestamps, secondary labels)

## Color Palette

### Dark Theme (Default)
- **Background Root**: `#1E1E1E`
- **Surface / Card**: `#252526`
- **Surface Hover**: `#2D2D30`
- **Surface Selected**: `#0078D4` (Windows Accent Blue)
- **Border / Divider**: `#3E3E42`
- **Text Primary**: `#FFFFFF`
- **Text Secondary**: `#CCCCCC`
- **Text Disabled / Subtle**: `#858585`
- **Accent Indicator Dot**: `#0078D4` (Events dot)

### Light Theme
- **Background Root**: `#F3F3F3`
- **Surface / Card**: `#FFFFFF`
- **Surface Hover**: `#EAEAEA`
- **Surface Selected**: `#0078D4`
- **Border / Divider**: `#E0E0E0`
- **Text Primary**: `#1F1F1F`
- **Text Secondary**: `#5C5C5C`
- **Text Disabled / Subtle**: `#8A8A8A`
- **Accent Indicator Dot**: `#0078D4`

## Spacing & Metrics
- Base grid: 4px
- Standard padding: 8px, 12px, 16px
- Corner Radius:
  - Widget window: 8px
  - Day cells: 4px
  - Buttons / Inputs: 4px
- Day Cell Size: 32x32px minimum touch/pointer target
