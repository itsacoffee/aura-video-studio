# Smart Uniform UI Scaling

This document describes the smart uniform UI scaling feature that maintains exact layout proportions across all window sizes.

## Overview

The smart uniform UI scaling feature allows the entire application UI to scale uniformly, similar to how a video scales in a media player. The layout maintains its exact proportions whether the window is maximized or taking only 30% of the screen.

## Key Features

- ✅ **Uniform Scaling**: The entire app UI scales proportionally based on window dimensions
- ✅ **Aspect Ratio Preservation**: Relative proportions of all UI elements remain constant
- ✅ **Fill Behavior**: Content always fills the available window space (no letterboxing)
- ✅ **Smooth Performance**: Debounced resize events for optimal performance
- ✅ **Interactive Elements**: All controls remain functional at all scales
- ✅ **User Preference**: Can be enabled/disabled via localStorage

## Technical Implementation

### Components

#### 1. `useUIScale` Hook

Located in `src/hooks/useUIScale.ts`, this hook calculates the appropriate scale factor based on window dimensions.

**Usage:**

```typescript
import { useUIScale } from '@/hooks/useUIScale';

function MyComponent() {
  const { scale, scaledWidth, scaledHeight } = useUIScale({
    baseWidth: 1920,  // Reference resolution
    baseHeight: 1080,
    mode: 'fill',     // or 'contain'
    debounceDelay: 150
  });

  return (
    <div style={{
      transform: `scale(${scale})`,
      transformOrigin: 'top left',
      width: `${scaledWidth}px`,
      height: `${scaledHeight}px`
    }}>
      {children}
    </div>
  );
}
```

#### 2. `useUIScaleCSS` Hook

Automatically sets CSS custom properties for use in stylesheets:

**CSS Variables Set:**

- `--ui-scale`: The current scale factor
- `--ui-scaled-width`: The scaled width in pixels
- `--ui-scaled-height`: The scaled height in pixels

**Usage:**

```typescript
import { useUIScaleCSS } from '@/hooks/useUIScale';

function MyComponent() {
  useUIScaleCSS(); // Sets CSS custom properties

  // Now use in CSS:
  // transform: scale(var(--ui-scale));
  // width: var(--ui-scaled-width);
}
```

#### 3. `ScaledUIContainer` Component

Located in `src/components/ScaledUIContainer.tsx`, this component wraps the application content to apply scaling.

**Props:**

- `baseWidth` (default: 1920): Reference width for scaling calculations
- `baseHeight` (default: 1080): Reference height for scaling calculations
- `mode` (default: 'fill'): Use 'fill' to fill viewport or 'contain' for letterboxing
- `enabled` (default: true): Whether scaling is enabled

**Usage in App.tsx:**

```typescript
<ScaledUIContainer enabled={uiScalingEnabled}>
  <div style={{ height: '100vh', display: 'flex', flexDirection: 'column' }}>
    {/* App content */}
  </div>
</ScaledUIContainer>
```

### Configuration

#### Base Resolution

The default base resolution is **1920×1080** (Full HD). This means:

- At 1920×1080, scale factor = 1.0 (no scaling)
- At 3840×2160 (4K), scale factor = 2.0 (2x larger)
- At 960×540, scale factor = 0.5 (half size)

To change the base resolution, pass custom values:

```typescript
<ScaledUIContainer baseWidth={2560} baseHeight={1440}>
  {/* App content */}
</ScaledUIContainer>
```

#### Scaling Modes

**Fill Mode (Default)**

- Uses `Math.max(scaleX, scaleY)` for scale calculation
- Content fills entire viewport
- No letterboxing or pillarboxing
- May crop content at edges for non-matching aspect ratios

**Contain Mode**

- Uses `Math.min(scaleX, scaleY)` for scale calculation
- Content fits within viewport
- May show letterboxing/pillarboxing for non-matching aspect ratios
- No content is cropped

### User Preference

UI scaling can be toggled on/off via localStorage:

```typescript
// Enable scaling
localStorage.setItem('uiScalingEnabled', 'true');

// Disable scaling
localStorage.setItem('uiScalingEnabled', 'false');

// Check current state
const isEnabled = localStorage.getItem('uiScalingEnabled') === 'true';
```

The preference is read on app startup and persists across sessions.

## Testing

The implementation includes comprehensive tests in `src/hooks/__tests__/useUIScale.test.ts`:

### Test Coverage

- ✅ Default configuration (1920×1080 base)
- ✅ 4K display scaling (3840×2160)
- ✅ Small window scaling (960×540)
- ✅ 30% screen size (576×324)
- ✅ Fill mode behavior
- ✅ Contain mode behavior
- ✅ Custom base dimensions
- ✅ Resize event handling
- ✅ Debounce functionality
- ✅ Edge cases (very small/large windows, portrait orientation)
- ✅ CSS custom property management
- ✅ Cleanup on unmount

**Run tests:**

```bash
npm test -- src/hooks/__tests__/useUIScale.test.ts
```

## Performance Considerations

### Debouncing

Resize events are debounced by default (150ms) to prevent excessive recalculations during window resize operations. This ensures:

- Smooth performance during resize
- No layout thrashing
- Optimized CPU usage

### CSS Transform

The implementation uses CSS `transform: scale()` which:

- Leverages GPU acceleration
- Doesn't trigger layout recalculations
- Maintains crisp rendering with proper `transform-origin`

## Browser Compatibility

The feature uses standard CSS and JavaScript APIs:

- CSS Transforms (supported in all modern browsers)
- CSS Custom Properties (supported in all modern browsers)
- Window resize events (universal support)

## Accessibility

The scaling feature maintains accessibility:

- All interactive elements remain functional at all scales
- Focus indicators scale proportionally
- Keyboard navigation works correctly
- Screen readers function normally (they read the DOM, not the visual representation)

## Examples

### Maximized Window (1920×1080)

```
Scale: 1.0
Visual: Normal size, no scaling
```

### 30% of Screen (576×324)

```
Scale: 0.3
Visual: Proportionally smaller, maintains exact layout
```

### 4K Display (3840×2160)

```
Scale: 2.0
Visual: Proportionally larger, maintains exact layout
```

### Ultrawide Monitor (2560×1080)

```
Fill Mode:
  Scale: max(2560/1920, 1080/1080) = 1.33
  Visual: Scaled to fill width, height matches

Contain Mode:
  Scale: min(2560/1920, 1080/1080) = 1.0
  Visual: Height fills, width may have margins
```

## Troubleshooting

### Scaling Not Working

1. Check that `uiScalingEnabled` is true in localStorage
2. Verify `ScaledUIContainer` is enabled: `<ScaledUIContainer enabled={true}>`
3. Check browser console for any errors

### Content Appears Clipped

If using fill mode with non-standard aspect ratios, some content may be clipped. Switch to contain mode:

```typescript
<ScaledUIContainer mode="contain">
```

### Performance Issues

If experiencing performance issues during resize:

1. Increase debounce delay: `debounceDelay: 300`
2. Check for other resize listeners that may conflict
3. Verify GPU acceleration is enabled in browser

## Future Enhancements

Potential improvements for future versions:

- [ ] User-configurable base resolution in settings
- [ ] Per-component scaling overrides
- [ ] Zoom controls (user-adjustable scale multiplier)
- [ ] Adaptive scaling based on DPI/pixel density
- [ ] Preset scale factors (75%, 100%, 125%, 150%)

## References

- Hook Implementation: `src/hooks/useUIScale.ts`
- Component: `src/components/ScaledUIContainer.tsx`
- Tests: `src/hooks/__tests__/useUIScale.test.ts`
- App Integration: `src/App.tsx`
