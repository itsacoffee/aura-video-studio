# Splash Screen Design Notes - Orange/Blue Particle Theme

## Overview

This document describes the implementation of the redesigned splash/loading screen for Aura Video Studio, featuring an orange/blue particle-based visual theme that aligns with the premium brand identity.

## Design Intent

The new splash screen replaces the previous purple-themed loading experience with a visually rich, icon-inspired design that:

- Uses the existing orange/blue color palette (`#FF6B35`, `#6366F1`, `#3B82F6`)
- Presents Aura as a premium, Apple/Adobe-level product
- Preserves all existing startup behavior and status reporting
- Maintains the same backend initialization logic, progress events, and startup timing

## Implementation Details

### Color Palette

The splash screen uses the following brand colors from the design system:

- **Orange**: `#FF6B35`, `#FF8960`, `#E85D2F`
- **Blue**: `#3B82F6`, `#60A5FA`, `#2563EB`
- **Purple** (accent): `#6366F1`, `#818CF8`, `#4F46E5`

### Background

The background uses a deep gradient transitioning from dark navy to deep violet:

```css
background: linear-gradient(180deg, #0a0f1a 0%, #1a1f2e 25%, #1a1626 50%, #0f0f1a 75%, #050510 100%);
```

An animated radial gradient overlay creates an orange/blue bloom effect around the app icon at 40-45% vertical height:

```css
background: radial-gradient(
  ellipse at 50% 42%,
  rgb(255 107 53 / 15%) 0%,
  rgb(59 130 246 / 12%) 20%,
  rgb(255 107 53 / 8%) 35%,
  rgb(59 130 246 / 6%) 50%,
  transparent 100%
);
```

### Particle System

- **Canvas-based particle animation** with ~300 particles
- Colors sampled from the orange/blue palette
- Particles drift slowly using deterministic movement patterns
- GPU-optimized for smooth 60fps performance
- Respects `prefers-reduced-motion` for accessibility

### Logo & Typography

- **Logo position**: Centered at approximately 40-45% of vertical height
- **Product name**: "Aura" - Large, bold (72px), with subtle glow/shadow
- **Subtitle**: "AI Video Generation Suite" - Medium-weight, muted accent color
- Typography uses system fonts for optimal rendering across platforms

### Progress Indicator

- Slim, wide progress bar with rounded corners
- Gradient fill using orange → purple → blue colors
- Subtle glow effect for depth
- Smooth transitions (0.3s ease)
- Status text displays context-aware messages during initialization

### Layout Structure

```
┌──────────────────────────────────────────┐
│                                          │
│                                          │
│                                          │
│         [Icon with flames]               │
│            Aura                          │
│     AI Video Generation Suite            │
│                                          │
│    Starting backend server...            │
│    ████████████░░░░░░░░░░░░░  60%       │
│                                          │
│                                          │
└──────────────────────────────────────────┘
```

## File Locations

### Web Application

- **Component**: `Aura.Web/src/components/SplashScreen/SplashScreen.tsx`
- **Styles**: `Aura.Web/src/components/SplashScreen/SplashScreen.css`
- **Tests**: `Aura.Web/src/components/SplashScreen/SplashScreen.test.tsx`
- **Usage**: Imported in `Aura.Web/src/App.tsx` and shown on first load

### Desktop Application

- **Electron splash**: `Aura.Desktop/electron/splash.html` (already orange/blue themed)
- **Assets splash**: `Aura.Desktop/assets/splash.html` (updated with orange/blue theme)
- **Window manager**: `Aura.Desktop/electron/window-manager.js` (loads splash.html)

## Animation Parameters

### Particle Animation
- **Particle count**: 300
- **Movement speed**: 0.3 pixels/frame (slow drift)
- **Opacity range**: 0.2 - 0.8
- **Size range**: 0.5 - 2.5 pixels

### Background Pulse
- **Duration**: 8 seconds
- **Easing**: ease-in-out
- **Scale variation**: 1.0 - 1.1

### Logo Float
- **Duration**: 4 seconds
- **Vertical movement**: ±15px
- **Scale variation**: 1.0 - 1.05

### Title Glow
- **Duration**: 3 seconds
- **Glow intensity**: Variable (30% - 40% opacity)

## Performance Considerations

1. **Canvas optimization**: Particles use `requestAnimationFrame` for smooth 60fps
2. **Reduced motion**: All animations respect `prefers-reduced-motion` media query
3. **Lightweight**: No heavy animation libraries; pure CSS and Canvas API
4. **GPU acceleration**: Transform and opacity animations use hardware acceleration

## Initialization Logic Preservation

**Important**: All initialization logic remains unchanged. Only visual presentation (CSS/styling) was modified:

- ✅ Progress stages and timing unchanged
- ✅ Status messages unchanged
- ✅ `onComplete` callback behavior unchanged
- ✅ Backend initialization checks unchanged
- ✅ Error handling unchanged

## Safe Customization Guidelines

When modifying the splash screen, follow these guidelines to avoid breaking startup logic:

### ✅ Safe to Modify

- Colors and gradients in CSS
- Animation durations and easing curves
- Typography (font sizes, weights, spacing)
- Logo/icon positioning (within reasonable bounds)
- Particle count and movement patterns

### ⚠️ Do NOT Modify

- Component props and callbacks (`onComplete`, `minDisplayTime`)
- Progress stage logic and timing
- Status message updates
- Component lifecycle hooks (useEffect dependencies)
- Error handling or initialization checks

## Testing Checklist

- [ ] Splash appears promptly on app startup
- [ ] Animations play smoothly at 60fps
- [ ] Progress indicators update correctly
- [ ] Status messages display during initialization
- [ ] Smooth fade transition to main UI (250-400ms)
- [ ] Works at multiple resolutions (1366×768, 1920×1080, 2560×1440)
- [ ] Respects Windows display scaling (100%, 125%, 150%)
- [ ] Accessibility: respects `prefers-reduced-motion`
- [ ] No performance regression (time-to-interactive unchanged)
- [ ] Works identically in development and production builds

## Browser/Platform Support

- ✅ Chrome/Edge (Chromium-based)
- ✅ Firefox
- ✅ Safari
- ✅ Electron (Windows, macOS, Linux)

## Future Enhancements

Potential future improvements (not implemented):

- Optional real-time backend status integration (if available)
- Adaptive particle count based on device performance
- Customizable animation intensity settings
- Integration with theme system for light/dark variants

---

**Last Updated**: 2024
**PR**: AURA UX 002 – Splash & Loading Screen Redesign (Orange/Blue Particle Theme)

