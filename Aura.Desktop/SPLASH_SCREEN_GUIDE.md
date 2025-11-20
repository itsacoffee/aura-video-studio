# Backend Startup Splash Screen - Visual Guide

## Overview

This document describes the visual appearance and behavior of the new startup splash screen.

## Splash Screen Design

### Visual Elements

```
┌──────────────────────────────────────────┐
│                                          │
│              ✨ Aura                     │
│        AI Video Generation Suite        │
│                                          │
│    Starting backend server...            │
│                                          │
│    ████████████░░░░░░░░░░░░░  60%       │
│                                          │
│              ◌ (spinning)                │
│                                          │
└──────────────────────────────────────────┘
```

### Dimensions
- Width: 600px
- Height: 400px
- Frame: None (frameless window)
- Background: Gradient (purple: #667eea to #764ba2)
- Always on top: Yes
- Transparent: Yes

### Text Elements

1. **Logo**: "✨ Aura"
   - Font size: 48px
   - Font weight: bold
   - Color: white
   - Shadow: 2px 2px 4px rgba(0,0,0,0.3)

2. **Tagline**: "AI Video Generation Suite"
   - Font size: 14px
   - Opacity: 0.9
   - Color: white

3. **Status Message**: Dynamic text showing current stage
   - Font size: 14px
   - Min height: 20px
   - Color: white

4. **Progress Bar**:
   - Width: 100%
   - Height: 4px
   - Background: rgba(255, 255, 255, 0.3)
   - Fill: white
   - Border radius: 2px
   - Smooth transition: 0.3s ease

5. **Spinner**: Animated loading indicator
   - Size: 20px × 20px
   - Border: 2px solid rgba(255, 255, 255, 0.3)
   - Top border: white
   - Animation: Continuous rotation (1s)

## Status Messages and Progress

### Stage 1: Initial (10%)
```
Message: "Starting application..."
Progress: 10%
Duration: < 1 second
```

### Stage 2: Backend Starting (15%)
```
Message: "Starting backend server..."
Progress: 15%
Duration: 1-2 seconds
```

### Stage 3: Backend Initializing (30-90%)
```
Message: "Waiting for backend to be ready..."
         or "Initializing: database, configuration..."
Progress: 30% → 90% (scales with readiness)
Duration: 5-30 seconds (varies by system)
```

Specific messages during this stage:
- "Starting backend server..." (30%)
- "Initializing: database" (40-60%)
- "Initializing: configuration" (60-80%)
- "Backend is ready" (90%)

### Stage 4: Loading UI (95%)
```
Message: "Backend ready! Loading application..."
Progress: 95%
Duration: 1-2 seconds
```

### Stage 5: Complete (100%)
```
Message: "Application loaded"
Progress: 100%
Duration: 0.5 seconds (then closes)
```

## Error State

If backend fails to start, the splash screen is replaced by an error dialog:

```
┌─────────────────────────────────────────────┐
│  ⚠️  Backend Startup Failed                 │
├─────────────────────────────────────────────┤
│                                             │
│  The Aura backend server failed to start.   │
│                                             │
│  Would you like to:                         │
│  • View logs for troubleshooting            │
│  • Retry starting the application           │
│  • Exit                                     │
│                                             │
├─────────────────────────────────────────────┤
│  [View Logs]  [Retry]  [Exit]              │
└─────────────────────────────────────────────┘
```

## Technical Implementation

### IPC Communication

The splash screen receives updates via Electron IPC:

```javascript
// Main process sends:
splashWindow.webContents.send('status-update', {
  message: 'Starting backend server...',
  progress: 15
});

// Splash screen receives:
ipcRenderer.on('status-update', (event, data) => {
  document.getElementById('status').textContent = data.message;
  document.getElementById('progress').style.width = data.progress + '%';
});
```

### File Location

- HTML: `Aura.Desktop/electron/splash.html`
- CSS: Inline in HTML (for self-contained design)
- JavaScript: Inline in HTML

### Browser Window Configuration

```javascript
{
  width: 600,
  height: 400,
  transparent: true,
  frame: false,
  alwaysOnTop: true,
  center: true,
  resizable: false,
  skipTaskbar: true,
  webPreferences: {
    nodeIntegration: true,
    contextIsolation: false
  }
}
```

## Animation Details

### Progress Bar
- Transition: `width 0.3s ease`
- Updates: Real-time as backend initializes
- Smooth: Yes (CSS transitions)

### Spinner
- Rotation: 360° in 1 second
- Direction: Clockwise
- Continuous: Yes (infinite loop)

### Fade-in (future enhancement)
- Not currently implemented
- Can be added with CSS animation

## Accessibility

Current implementation:
- High contrast text (white on purple)
- Large, readable font sizes
- Clear status messages
- Visual progress indicator

Future enhancements:
- Screen reader announcements
- Keyboard navigation (for error dialogs)
- Reduced motion support

## Browser Support

The splash screen uses standard HTML5/CSS3/ES6 features:
- CSS Grid/Flexbox for layout
- CSS animations
- JavaScript ES6 (const, arrow functions)
- Electron IPC (require, ipcRenderer)

All features are fully supported in Electron 32+.

## Testing

### Manual Testing

1. Launch app: `npm start` from Aura.Desktop
2. Observe splash screen appears
3. Watch progress bar fill from 0% to 100%
4. Read status messages as they update
5. Confirm splash closes when app loads

### Visual Testing

- Check: Logo displays correctly
- Check: Progress bar is smooth
- Check: Spinner rotates continuously
- Check: Status text is readable
- Check: Window is centered on screen

### Error Testing

1. Block backend port (e.g., run another app on port 5005)
2. Launch app
3. Wait 90 seconds
4. Confirm error dialog appears
5. Test "Retry" button functionality
6. Test "View Logs" button opens correct folder

## Known Issues

None at this time.

## Future Enhancements

1. Add app icon to splash screen
2. Add fade-in/fade-out transitions
3. Add more granular progress (substeps)
4. Show estimated time remaining
5. Add cancel button for long startups
6. Localization support for messages
7. Theme support (dark/light modes)
