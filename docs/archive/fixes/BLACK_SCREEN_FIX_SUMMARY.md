# Black Screen Fix - Implementation Summary

## Problem
The application screen randomly turned black when:
- Losing/regaining window focus
- Minimizing and restoring the window
- Switching between applications (Alt-Tab)
- On some systems, seemingly randomly during use

## Root Cause Analysis

### 1. Redundant Event Handlers (Electron + React)
- `Aura.Desktop/electron/window-manager.js` had event handlers for `focus`, `blur`, `restore`, `show`
- `Aura.Web/src/App.tsx` had event handlers for `focus`, `blur`, `visibilitychange`
- Both injected JavaScript and manipulated the DOM simultaneously
- Created race conditions with competing timers (10ms, 50ms, 100ms, 5000ms intervals)

### 2. Excessive DOM Manipulation
On every focus event, the old code:
- Forced background color changes on root and body
- Dispatched synthetic `resize` events
- Dispatched synthetic `visibilitychange` events
- Accessed `offsetHeight` to force layout recalculation
- Checked for "black screen" by inspecting backgroundColor
- Had a recursive prevention flag that could deadlock

### 3. WebContents Rendering Pipeline Interference
The constant JavaScript execution during window state transitions interrupted Chromium's rendering pipeline, especially:
- During minimize/restore operations
- When switching between apps
- With hardware acceleration edge cases
- Multi-monitor setups with different DPI

## Solution Implementation

### Strategy: Coordinated Single-Source Prevention

#### Changes to `Aura.Desktop/electron/window-manager.js`:

**Removed** (154 lines):
- Complex focus handler (lines 403-461) with JavaScript injection
- Blur handler doing nothing (lines 463-468)
- Restore handler with complex JavaScript (lines 470-520)
- Show handler with more JavaScript injection (lines 522-556)

**Added** (40 lines):
- Single coordinated visibility handler
- Proper state tracking to avoid unnecessary operations
- 300ms debouncing to prevent rapid-fire repaints
- `webContents.invalidate()` - Electron's proper API instead of JavaScript hacks
- No DOM manipulation via JavaScript injection

**Updated BrowserWindow webPreferences**:
```javascript
backgroundThrottling: false,  // Don't throttle when backgrounded
offscreen: false,              // Use normal rendering
```

#### Changes to `Aura.Web/src/App.tsx`:

**Removed** (163 lines):
- All black screen detection code (lines 389-549):
  - `handleVisibilityChange` function
  - `handleFocus` function
  - `handleBlur` function
  - `blackScreenCheck` setInterval (5 second polling)
  - `handleWindowFocus` with recursive prevention flag
  - All event listeners for these handlers
- Duplicate black screen monitoring (lines 759-804):
  - `checkForBlackScreen` function
  - Its setInterval (3 second polling)

**Kept**:
- Simple background color management for theme changes only

#### Changes to `Aura.Desktop/electron/main.js`:

**Added** Chromium command line switches:
```javascript
app.commandLine.appendSwitch('disable-backgrounding-occluded-windows');
app.commandLine.appendSwitch('disable-renderer-backgrounding');
```

## Why This Works

✅ **Eliminates race conditions** - No more competing handlers between Electron and React
✅ **Removes competing DOM manipulation** - Single source of truth for window state
✅ **Uses proper Electron APIs** - `webContents.invalidate()` instead of JavaScript workarounds
✅ **Prevents rendering pipeline interruption** - No more synthetic events during state transitions
✅ **Single debounced coordination point** - 300ms debounce prevents rapid-fire repaints
✅ **No more recursive/deadlock scenarios** - Removed complex prevention flags
✅ **Better performance** - No constant polling (was checking every 3-5 seconds)

## Code Reduction
- **Before**: 354 lines of black screen detection code
- **After**: 46 lines of coordinated visibility handling
- **Net reduction**: 308 lines (87% reduction)

## Testing Checklist

- [ ] Window focus/blur no longer causes black screen
- [ ] Minimize/restore works correctly
- [ ] Alt-Tab between applications works
- [ ] No black screen on startup
- [ ] Multi-monitor scenarios work
- [ ] Theme switching still works correctly
- [ ] No console errors related to rendering
- [ ] Performance is improved (no constant JavaScript execution)

## Technical Details

### Debouncing Strategy
The 300ms debounce was chosen because:
- Long enough to batch rapid state changes (minimize/restore, Alt-Tab)
- Short enough to feel responsive to users
- Prevents the rendering pipeline from being interrupted mid-transition

### webContents.invalidate()
This Electron API:
- Triggers a proper repaint through Chromium's rendering pipeline
- Doesn't interrupt ongoing rendering operations
- Works correctly with hardware acceleration
- Doesn't require injecting JavaScript into the renderer process

### Chromium Flags
- `disable-backgrounding-occluded-windows`: Prevents Chromium from throttling rendering when window is occluded
- `disable-renderer-backgrounding`: Prevents renderer process from being deprioritized when backgrounded

These flags ensure Chromium maintains full rendering capability even when the window is not in focus.

## Migration Notes

If black screen issues occur in the future:
1. **Don't add polling intervals** - These mask the real problem and hurt performance
2. **Don't manipulate DOM from Electron** - Use proper Electron APIs
3. **Don't add redundant event handlers** - Coordinate at a single point
4. **Do use debouncing** - Window events can fire rapidly
5. **Do use proper APIs** - `webContents.invalidate()`, not JavaScript injection

## References
- Electron Documentation: https://www.electronjs.org/docs/latest/api/web-contents#contentsinvalidate
- Chromium Command Line Switches: https://peter.sh/experiments/chromium-command-line-switches/
