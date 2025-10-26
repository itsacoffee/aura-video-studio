# Icon System Guidelines

## Overview
The application uses Fluent UI React Icons (@fluentui/react-icons) for a consistent, professional icon set throughout the interface.

## Icon Sizes
Follow these standard sizes for icons:
- **16px**: Small inline icons, compact UI elements
- **20px**: Default size for most UI elements, menu items, buttons
- **24px**: Larger icons for prominent actions, navigation, headers

## Usage in Components

### Importing Icons
```tsx
import { IconName20Regular, IconName24Regular } from '@fluentui/react-icons';
```

### Naming Convention
Icons follow the pattern: `{Name}{Size}{Variant}`
- Example: `Save24Regular`, `Play20Regular`, `Settings16Regular`
- Variant is typically `Regular` for outlined icons or `Filled` for solid icons

### Layout Components
Current layout components use:
- TopMenuBar: Not using icons in text menus (desktop convention)
- StatusFooter: Uses 20px icons (ChevronUp20Regular, ChevronDown20Regular)
- PanelTabs: Uses 20px icons (Dismiss20Regular)

### Best Practices
1. **Consistency**: Use the same size icon throughout a component or section
2. **Optical Alignment**: Icons may need slight vertical adjustment for visual alignment
3. **Accessibility**: Always provide aria-label or aria-labelledby for icon buttons
4. **Color**: Icons inherit text color by default; use theme colors for consistency

### Common Icons by Function
- **Navigation**: ArrowLeft, ArrowRight, ChevronUp, ChevronDown
- **Actions**: Play, Pause, Stop, Save, Delete, Edit
- **Status**: Checkmark, Error, Warning, Info
- **File Operations**: Folder, FolderOpen, Document, DocumentAdd
- **UI Controls**: Settings, More, Dismiss, Search

## Theme Integration
Icons automatically adapt to the current theme through CSS color inheritance:
```tsx
color: 'var(--color-text-primary)' // Icons will match text color
color: 'var(--color-primary)'      // Icons will use accent color
```

## Accessibility
- Always provide meaningful labels for icon-only buttons
- Use aria-label for screen readers
- Ensure sufficient color contrast (WCAG AA: 4.5:1 for normal text, 3:1 for large text)

## Resources
- [Fluent UI Icons Gallery](https://aka.ms/fluent-icons)
- [Fluent Design System](https://fluent2.microsoft.design/)
