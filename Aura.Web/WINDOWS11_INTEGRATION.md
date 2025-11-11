# Windows 11 Native UI Integration

This document describes the Windows 11 native UI integration features implemented in Aura Video Studio.

## Features

### 1. Platform Detection
- Automatic detection of Windows platform
- Windows 11-specific feature detection
- Chromium browser detection for advanced features

### 2. DPI Scaling Support
- Automatic detection of display scaling (100%, 150%, 200%, 300%+)
- DPI-aware components and layouts
- Responsive sizing based on display density
- Four scaling categories:
  - **Normal** (100%): Standard desktop displays
  - **Medium** (150%): Recommended for most high-DPI displays
  - **High** (200%): Large scaling for readability
  - **Very High** (300%+): Maximum accessibility

### 3. Windows 11 Design Language
- **Rounded Corners**: Modern, softer UI elements
- **Acrylic Materials**: Translucent backgrounds with blur effects
- **Mica Effect**: Subtle window background material
- **Enhanced Shadows**: Layered depth system
- **Smooth Animations**: Windows 11 animation timing

### 4. System Theme Integration
- Automatic detection of Windows light/dark mode
- Real-time synchronization with system theme changes
- Respects user's explicit theme preference
- Seamless theme transitions

### 5. Windows 11 Snap Layouts
- Proper meta tags for snap layout support
- Enables native Windows window management
- Works in Chromium-based browsers on Windows 11

### 6. Native Context Menus
- Windows-style right-click menus
- Fluent UI-based implementation
- Acrylic backdrop effects
- Keyboard shortcut indicators
- Submenu support

## Usage

### In Components

```typescript
import { useWindowsNativeUI } from '../hooks/useWindowsNativeUI';

function MyComponent() {
  const windowsUI = useWindowsNativeUI();
  
  return (
    <div>
      {windowsUI.isWindows11 && <Windows11Features />}
      <p>DPI: {windowsUI.dpiInfo.percentage}%</p>
      <p>Theme: {windowsUI.systemTheme}</p>
    </div>
  );
}
```

### Context Menus

```typescript
import { ContextMenu, useContextMenu } from '../components/ContextMenu';

function MyComponent() {
  const { position, showContextMenu, hideContextMenu } = useContextMenu();
  
  const menuItems = [
    { id: 'cut', label: 'Cut', shortcut: 'Ctrl+X', onClick: handleCut },
    { id: 'copy', label: 'Copy', shortcut: 'Ctrl+C', onClick: handleCopy },
  ];
  
  return (
    <div onContextMenu={showContextMenu}>
      Right-click me
      <ContextMenu position={position} items={menuItems} onClose={hideContextMenu} />
    </div>
  );
}
```

### Utilities

```typescript
import {
  isWindows,
  isWindows11,
  getDevicePixelRatio,
  isHighDPI,
  cssToPhysicalPixels,
  getDPIScaleInfo,
  getSystemThemePreference,
  supportsSnapLayouts,
} from '../utils/windowsUtils';

// Check platform
if (isWindows11()) {
  console.log('Running on Windows 11');
}

// DPI scaling
const dpiInfo = getDPIScaleInfo();
console.log(`Display scaling: ${dpiInfo.percentage}%`);
console.log(`Category: ${dpiInfo.scaleCategory}`);

// Convert measurements
const physicalPixels = cssToPhysicalPixels(100);
console.log(`100 CSS pixels = ${physicalPixels} physical pixels`);

// System theme
const theme = getSystemThemePreference();
console.log(`System theme: ${theme}`);
```

## CSS Classes

The following classes are automatically applied to `<body>`:

### Platform Classes
- `.windows` - Applied on any Windows system
- `.windows-11` - Applied on Windows 11 specifically

### DPI Classes
- `.dpi-normal` - 100% scaling
- `.dpi-medium` - 150% scaling
- `.dpi-high` - 200% scaling
- `.dpi-very-high` - 300%+ scaling

## CSS Variables

Windows 11-specific CSS variables are available in `src/styles/windows11.css`:

```css
--win11-corner-radius-small: 4px;
--win11-corner-radius-medium: 8px;
--win11-corner-radius-large: 12px;
--win11-acrylic-background: rgb(255 255 255 / 70%);
--win11-acrylic-blur: 30px;
--win11-shadow-flyout: 0 8px 16px 0 rgb(0 0 0 / 14%);
--win11-animation-duration-normal: 250ms;
```

## Demo Page

Visit `/windows11-demo` (development mode only) to see all features in action.

## Browser Compatibility

- **Best experience**: Chromium-based browsers on Windows 11 (Edge, Chrome)
- **Partial support**: Firefox, Safari (basic features work)
- **Graceful degradation**: Non-Windows platforms get standard styling

## Testing

Run the Windows utilities test suite:

```bash
npm test -- src/utils/__tests__/windowsUtils.test.ts
```

## File Structure

```
src/
├── utils/
│   ├── windowsUtils.ts          # Windows platform utilities
│   └── __tests__/
│       └── windowsUtils.test.ts # Unit tests
├── hooks/
│   └── useWindowsNativeUI.ts    # React hook for Windows features
├── components/
│   └── ContextMenu/
│       ├── ContextMenu.tsx       # Native context menu component
│       └── index.ts              # Exports
├── styles/
│   └── windows11.css             # Windows 11 design language
└── pages/
    └── Windows11DemoPage.tsx     # Demo/documentation page
```

## Implementation Notes

1. **DPI Detection**: Uses `window.devicePixelRatio` which is reliable across browsers
2. **Theme Detection**: Uses `prefers-color-scheme` media query with event listeners
3. **Windows Detection**: Checks `navigator.platform` and `userAgent`
4. **Snap Layouts**: Requires Windows 11 + Chromium browser + proper meta tags
5. **Performance**: All detections are cached and only recalculated on resize/theme change

## Future Enhancements

Potential improvements for future PRs:
- Tablet mode detection and optimizations
- Touch gesture integration
- Windows Ink support for digital pen users
- HDR display detection and color space handling
- Windows Hello authentication integration
