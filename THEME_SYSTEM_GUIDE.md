# Theme System Implementation Guide

This document describes the dark/light theme system implementation in Aura Video Studio.

## Overview

The application supports both light and dark themes with automatic OS detection and manual toggle capability. The theme system is integrated across three layers:

1. **React State Management** (Theme Context)
2. **Fluent UI Components** (Design System)
3. **Tailwind CSS** (Utility Classes)

## Architecture

### 1. Theme State Management

**Location**: `Aura.Web/src/App.tsx`

```typescript
const [isDarkMode, setIsDarkMode] = useState(() => {
  const saved = localStorage.getItem('darkMode');
  if (saved !== null) {
    return JSON.parse(saved);
  }
  // Detect system preference if no saved preference
  return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
});
```

**Initialization Priority**:
1. User's saved preference (localStorage)
2. System/OS preference (prefers-color-scheme)
3. Default: Light mode

### 2. Theme Application

**Location**: `Aura.Web/src/App.tsx` (useEffect)

```typescript
useEffect(() => {
  const root = document.documentElement;
  if (isDarkMode) {
    root.classList.add('dark');
  } else {
    root.classList.remove('dark');
  }
  localStorage.setItem('darkMode', JSON.stringify(isDarkMode));
  console.log(`Theme switched to: ${isDarkMode ? 'dark' : 'light'} mode`);
}, [isDarkMode]);
```

**What Happens**:
1. Adds/removes `dark` class on `<html>` element
2. Saves preference to localStorage
3. Logs theme change for debugging

### 3. OS Theme Change Detection

**Location**: `Aura.Web/src/App.tsx` (useEffect)

```typescript
useEffect(() => {
  const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
  
  const handleChange = (e: MediaQueryListEvent) => {
    const saved = localStorage.getItem('darkMode');
    // Only update if user hasn't explicitly saved a preference
    if (saved === null) {
      setIsDarkMode(e.matches);
    }
  };
  
  if (mediaQuery.addEventListener) {
    mediaQuery.addEventListener('change', handleChange);
    return () => mediaQuery.removeEventListener('change', handleChange);
  }
}, []);
```

**Behavior**:
- Listens for system theme changes
- Only applies if user hasn't set a manual preference
- Respects user choice over system preference

### 4. Theme Context

**Location**: `Aura.Web/src/App.tsx`

```typescript
export const ThemeContext = createContext<ThemeContextType>({
  isDarkMode: false,
  toggleTheme: () => {},
});

export const useTheme = () => useContext(ThemeContext);
```

**Usage in Components**:
```typescript
import { useTheme } from './App';

function MyComponent() {
  const { isDarkMode, toggleTheme } = useTheme();
  
  return (
    <button onClick={toggleTheme}>
      Switch to {isDarkMode ? 'Light' : 'Dark'} Mode
    </button>
  );
}
```

## CSS Implementation

### CSS Custom Properties

**Location**: `Aura.Web/src/index.css`

```css
:root {
  /* Light mode colors */
  --color-primary: #0ea5e9;
  --color-secondary: #a855f7;
  --color-success: #22c55e;
  --color-warning: #f59e0b;
  --color-error: #ef4444;
  --color-background: #ffffff;
  --color-surface: #f8f9fa;
  --color-text-primary: #1f2937;
  --color-text-secondary: #6b7280;
  --color-border: #e5e7eb;
}

.dark {
  /* Dark mode color overrides */
  --color-primary: #38bdf8;
  --color-secondary: #c084fc;
  --color-success: #4ade80;
  --color-warning: #fbbf24;
  --color-error: #f87171;
  --color-background: #0f172a;
  --color-surface: #1e293b;
  --color-text-primary: #f1f5f9;
  --color-text-secondary: #cbd5e1;
  --color-border: #334155;
}
```

**Usage**:
```css
.my-element {
  background-color: var(--color-background);
  color: var(--color-text-primary);
  border: 1px solid var(--color-border);
}
```

### Tailwind CSS Dark Mode

**Location**: `Aura.Web/tailwind.config.js`

```javascript
export default {
  darkMode: 'class', // Use class-based dark mode
  // ... rest of config
}
```

**Usage**:
```jsx
<div className="bg-white dark:bg-slate-800 text-gray-900 dark:text-gray-100">
  This adapts to theme
</div>
```

### Scrollbar Styling

**Location**: `Aura.Web/src/index.css`

```css
::-webkit-scrollbar {
  width: 10px;
  height: 10px;
}

::-webkit-scrollbar-track {
  background: var(--color-surface);
}

::-webkit-scrollbar-thumb {
  background: var(--color-border);
  border-radius: 5px;
}

::-webkit-scrollbar-thumb:hover {
  background: var(--color-text-secondary);
}
```

**Benefits**:
- Automatically adapts to theme via CSS variables
- No need for separate `.dark` overrides
- Consistent with overall theme

## Fluent UI Integration

**Location**: `Aura.Web/src/App.tsx`

```typescript
import { FluentProvider, webLightTheme, webDarkTheme } from '@fluentui/react-components';

<FluentProvider theme={isDarkMode ? webDarkTheme : webLightTheme}>
  {/* App content */}
</FluentProvider>
```

**What This Does**:
- Applies Fluent UI design tokens to all Fluent components
- Ensures buttons, inputs, dialogs, etc. match the theme
- Works alongside Tailwind and custom CSS

## Best Practices

### 1. Use CSS Variables for Colors

✅ **Good**:
```css
.element {
  color: var(--color-primary);
  background: var(--color-background);
}
```

❌ **Bad**:
```css
.element {
  color: #0ea5e9;
  background: #ffffff;
}
```

### 2. Use Tailwind Dark Mode Utilities

✅ **Good**:
```jsx
<div className="bg-white dark:bg-gray-900">Content</div>
```

❌ **Bad**:
```jsx
<div style={{ backgroundColor: isDarkMode ? '#111827' : '#ffffff' }}>Content</div>
```

### 3. Test Both Themes

Always test UI changes in both light and dark mode to ensure:
- Text is readable
- Contrast is sufficient
- No visual glitches
- Icons are visible

### 4. Avoid Hard-coded Colors

Replace hard-coded hex/rgb values with:
- CSS custom properties for semantic colors
- Tailwind utility classes with dark mode variants

### 5. Handle Images and Icons

For images that don't adapt well:

```css
.dark .image-that-needs-inversion {
  filter: invert(1);
}
```

For SVG icons, use currentColor:
```svg
<svg fill="currentColor">...</svg>
```

## Troubleshooting

### Theme Not Switching

**Check**:
1. Is `dark` class being added to `<html>`? (Inspect element)
2. Is localStorage updating? (Check Application tab in DevTools)
3. Are CSS custom properties defined in `:root` and `.dark`?
4. Is Tailwind dark mode set to `'class'` in config?

**Debug**:
```javascript
// In browser console
console.log(document.documentElement.classList.contains('dark'));
console.log(localStorage.getItem('darkMode'));
```

### Fluent UI Components Not Theming

**Check**:
1. Is `FluentProvider` wrapping your app?
2. Is the `theme` prop changing with `isDarkMode`?
3. Are you using Fluent UI components (not plain HTML elements)?

### Tailwind Dark Classes Not Working

**Check**:
1. Is `darkMode: 'class'` in `tailwind.config.js`?
2. Is the `dark` class on a parent element?
3. Are you using the `dark:` prefix correctly?

### Colors Not Updating

**Check**:
1. Are you using CSS variables instead of hard-coded colors?
2. Is the element inside the theme provider?
3. Are there any !important rules overriding the theme?

## Testing Checklist

- [ ] Toggle theme button works
- [ ] Theme persists across page reloads
- [ ] OS theme detection works on first load
- [ ] Theme responds to OS theme changes (when no saved preference)
- [ ] All text is readable in both themes
- [ ] Buttons and inputs match theme
- [ ] Scrollbars match theme
- [ ] Images and icons are visible in both themes
- [ ] No flickering during theme switch
- [ ] Console logs show theme changes

## Future Enhancements

1. **Multiple Themes**: Support more than just light/dark (e.g., high contrast)
2. **Custom Theme Editor**: Let users customize colors
3. **Per-Component Themes**: Different themes for different sections
4. **Animation**: Smooth transitions when switching themes
5. **SSR Support**: Prevent flash of wrong theme on server-rendered pages
