# EditorLayout Visual Guide - Premiere-Style Workspace

## Layout Structure Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ Top Menu Bar / Menu Bar (Project Name, File, Edit, View, Help)             │
├────────┬───────────────────────────────────────────────┬─────────┬──────────┤
│ Media  │                                               │ Props   │ History  │
│ Library│                Preview Panel                  │ Panel   │ Panel    │
│        │              (Top Region 60%)                 │         │          │
│ [Grid  │                                               │ [Props] │ [Undo]   │
│  of    │         Video Preview with Playback           │ [Tabs]  │ [Redo]   │
│ assets]│         Controls and Fullscreen               │ [Forms] │ [List]   │
│        │                                               │         │          │
├────────┤                                               ├─────────┴──────────┤
│ Effects│                                               │                    │
│ Library│                                               │                    │
│        │                                               │                    │
│ [FX    │                                               │                    │
│  Grid] │                                               │                    │
│        │                                               │                    │
├────────┼───────────────────────────────────────────────┴────────────────────┤
│        │                                                                     │
│        ├─────────────────────────────────────────────────────────────────────┤
│        │                   Timeline Panel                                    │
│        │                (Bottom Region 40%)                                  │
│        │                                                                     │
│        │  Toolbar: [Play] [Stop] [Tools] [Zoom] [Snap]                      │
│        │  ┌────────────────────────────────────────────────────────────┐    │
│        │  │ Ruler: 00:00  00:05  00:10  00:15  00:20  00:25  00:30     │    │
│        │  ├────────────────────────────────────────────────────────────┤    │
│        │  │ Video 1 ▶ [━━━Clip 1━━━]    [━━━Clip 2━━━]                │    │
│        │  │ Video 2 ▶ [Empty]                                          │    │
│        │  │ Audio 1 ▶ [∿∿∿Audio∿∿∿]     [∿∿∿Audio∿∿∿]                │    │
│        │  │ Audio 2 ▶ [Empty]                                          │    │
│        │  │           │ ← Red Playhead                                 │    │
│        │  └────────────────────────────────────────────────────────────┘    │
└────────┴─────────────────────────────────────────────────────────────────────┘
```

## Region Breakdown

### Top Region (60% vertical height)
**Preview Panel** - Full-width video preview
- Centered video player with aspect ratio maintained
- Playback controls (play, pause, step forward/backward)
- Timeline scrubber below video
- Fullscreen toggle button
- Effects preview overlay
- Timecode display

### Bottom Region (40% vertical height)  
**Timeline Panel** - Full-width timeline editor
- **Toolbar** (top): Play/pause, tool selection, zoom controls, snap toggle
- **Ruler** (below toolbar): Time markers with current time indicator
- **Tracks** (main area): Video and audio tracks with clips
- **Playhead** (vertical red line): Current playback position
- **Track Headers** (left): Track name, visibility toggle, lock toggle
- Horizontal scroll for long projects
- Vertical scroll for many tracks

### Right Region (Sidebar Panels)

#### Left Sidebar
1. **Media Library Panel** (280px default)
   - Grid view of imported media files
   - Thumbnails for video/image assets
   - Waveforms for audio files
   - Import button and search
   - Drag-and-drop to timeline

2. **Effects Library Panel** (280px default)
   - Categorized effects list
   - Preset effects cards
   - Search and filter
   - Drag-and-drop to clips
   - Preview thumbnails

#### Right Sidebar
1. **Properties Panel** (320px default)
   - Selected clip properties
   - Transform controls (position, scale, rotation)
   - Effect parameters
   - Opacity slider
   - Blend mode selector
   - Delete button

2. **History Panel** (320px default)
   - Undo/redo stack visualization
   - Command list with timestamps
   - Current position indicator
   - Jump to any history state

## Resizer Details

### Vertical Dividers (Horizontal Panel Separation)
- **Width**: 4px interactive area
- **Visual indicator**: 2px line (gray → cyan on hover)
- **Cursor**: `ew-resize` (↔️)
- **Keyboard**: Arrow Left/Right to adjust (±10px increments)
- **Drag**: Smooth resize with snap points
- **Limits**: Enforced min/max widths per panel

**Locations**:
- Between Media Library and Effects Library
- Between Effects Library and Center Region
- Between Center Region and Properties Panel
- Between Properties Panel and History Panel

### Horizontal Divider (Vertical Panel Separation)
- **Height**: 4px interactive area
- **Visual indicator**: 2px line (gray → cyan on hover)
- **Cursor**: `ns-resize` (↕️)
- **Keyboard**: Arrow Up/Down to adjust (±5% increments)
- **Drag**: Smooth resize with snap to common proportions
- **Snap Points**: 40%, 50%, 60%, 66%, 70%, 75%, 80%
- **Limits**: Preview 40-80%, Timeline 20-60%

**Location**:
- Between Preview Panel (top) and Timeline Panel (bottom)

## Collapse Behavior

### Panel Headers
Every collapsible panel has a header:
```
┌────────────────────────┐
│ PANEL TITLE          ◀│  ← Collapse button (chevron icon)
├────────────────────────┤
│ Panel content...       │
│                        │
```

### Collapsed State
When collapsed, panel shrinks to 48px width showing only icons:
```
┌──┐
│≡ │
│▶ │  ← Icon-only sidebar
│  │
└──┘
```

### Expand Button
Clicking the chevron or header expands back to previous width

## Color Scheme (from video-editor-theme.css)

### Backgrounds
- **Primary**: `#1a1a1a` (deep dark, main editor background)
- **Secondary**: `#252525` (panel backgrounds)
- **Panel Header**: `#2a2a2a` (slightly lighter for headers)
- **Timeline**: `#1a1a1a` (matches primary)

### Interactive Elements
- **Accent (Cyan)**: `#0ea5e9` (hover states, active dividers)
- **Accent Hover**: `#38bdf8` (lighter cyan on hover)
- **Accent Ring**: `#0ea5e980` (semi-transparent glow)

### Clips
- **Video Clips**: `#4a5568` (blue-gray with gradient)
- **Audio Clips**: `#2d7a8f` (teal with gradient)
- **Image Clips**: `#6b46c1` (purple with gradient)
- **Selected**: `#0ea5e9` (cyan border, 2px)

### Playhead
- **Color**: `#ff4444` (high-visibility red)
- **Shadow**: `#ff444480` (red glow)
- **Triangle**: Red triangle indicator at top

### Borders & Dividers
- **Panel Border**: `#3a3a3a` (subtle gray)
- **Divider**: `#3a3a3a` → `#0ea5e9` (gray to cyan on hover)
- **Track Border**: `#2d2d2d` (very subtle separation)

### Text
- **Primary**: `#e8e8e8` (high contrast white)
- **Secondary**: `#a0a0a0` (medium gray for labels)
- **Tertiary**: `#707070` (low contrast for hints)
- **Disabled**: `#505050` (very low contrast)

## Spacing & Typography

### Spacing Scale (8px base)
- **XS**: 4px (tight spacing)
- **SM**: 8px (standard gap)
- **MD**: 12px (panel padding)
- **LG**: 16px (major sections)
- **XL**: 24px (large gaps)
- **2XL**: 32px (maximum spacing)

### Font Sizes
- **XS**: 11px (timecodes, metadata)
- **SM**: 12px (panel headers, labels)
- **Base**: 13px (body text)
- **LG**: 14px (titles)
- **XL**: 16px (large headings)

### Font Weights
- **Normal**: 400 (body text)
- **Medium**: 500 (emphasis)
- **Semibold**: 600 (headers)
- **Bold**: 700 (strong emphasis)

## Transitions & Animations

### Transition Timing
- **Fast**: 150ms (button hover, quick feedback)
- **Base**: 250ms (panel resize, layout changes)
- **Slow**: 350ms (complex animations)

### Easing
- All transitions use `cubic-bezier(0.4, 0, 0.2, 1)` for smooth, natural motion

### GPU-Accelerated Properties
- `transform` (scale, translate)
- `opacity`
- Avoid animating width/height directly (uses flex instead)

## Responsive Behavior

### Window Width < 1280px
- Sidebar panels auto-collapse to icon-only mode
- Preview maintains aspect ratio
- Timeline toolbar becomes dropdown menu

### Window Width > 1920px
- Panels expand to comfortable max widths
- More workspace for timeline
- Preview scales to fill available space

### Fullscreen Mode (F11 or button)
- Hides menu bar (available via hamburger menu)
- Preview expands to full container
- Timeline remains visible at bottom
- Sidebars remain collapsible
- ESC key exits fullscreen

## Keyboard Shortcuts

### Workspace Navigation
- `Alt+1`: Editing workspace (default 60/40)
- `Alt+2`: Color workspace (properties expanded)
- `Alt+3`: Effects workspace (effects expanded)
- `Alt+4`: Audio workspace (media + properties)
- `Alt+5`: Assembly workspace (media expanded)
- `Alt+0`: Reset to default layout

### Panel Focus
- `Tab`: Cycle through focusable elements
- `Shift+Tab`: Reverse cycle
- Arrow keys on focused divider: Resize panel
- `Enter` on panel header: Toggle collapse

### Playback (when timeline focused)
- `Space`: Play/Pause
- `J`: Reverse playback (press multiple times for faster)
- `K`: Pause and reset speed
- `L`: Forward playback (press multiple times for faster)
- `Left/Right Arrow`: Previous/Next frame
- `I`: Set In point
- `O`: Set Out point
- `/`: Play around current position

### Editing
- `Ctrl+Z`: Undo
- `Ctrl+Y` or `Ctrl+Shift+Z`: Redo
- `Delete` or `Backspace`: Delete selected clip
- `V`: Select tool
- `C`: Razor tool
- `H`: Hand tool

## Accessibility Features

### Keyboard Navigation
- All interactive elements keyboard accessible
- Logical tab order (left-to-right, top-to-bottom)
- Focus indicators (2px cyan outline with 4px shadow ring)
- Skip links to main content areas

### Screen Reader Support
- Semantic HTML (`<main>`, `<nav>`, `<section>`)
- ARIA labels on all controls
- ARIA roles (`separator` for dividers)
- Live regions for status updates

### Visual Accessibility
- WCAG AA contrast ratios (4.5:1 minimum)
- No information conveyed by color alone
- Clear focus indicators
- Reduced motion option support (prefers-reduced-motion)

## Performance Characteristics

### Rendering
- React.memo prevents unnecessary re-renders
- useCallback/useMemo for expensive operations
- Virtual scrolling for long clip lists
- RequestAnimationFrame for smooth animations

### Resize Performance
- CSS transforms (GPU-accelerated)
- Debounced localStorage writes
- Snap-to-breakpoint reduces jank
- Transition uses will-change hint

### Memory
- Event listeners cleaned up in useEffect
- No memory leaks in resize handlers
- Refs properly managed
- Component unmount cleanup

## Comparison with Premiere Pro

### Similarities ✓
- 60/40 Preview/Timeline split by default
- Collapsible sidebar panels
- Resizable dividers with snap points
- Dark theme with cyan accents
- Playhead with red triangle indicator
- Keyboard shortcuts (J/K/L, I/O, etc.)
- Tool switching (V/C/H)
- Workspace presets

### Differences
- Simpler effects panel (no nested categories yet)
- No tabbed panel groups (planned enhancement)
- Fixed left/right sidebar positions (not customizable)
- No floating panels (desktop-only)
- Simplified track controls (no advanced automation)

## Future Enhancements

### Planned
1. **Tab Groups**: Multiple panels in tabs
2. **Drag-and-Drop Panels**: Rearrange panel positions
3. **Panel Zoom**: Maximize individual panels
4. **Custom Regions**: Define custom layout regions
5. **Vertical Stacking**: Stack panels vertically in sidebars

### Under Consideration
- Light theme variant
- Color customization
- Panel animation presets
- Mini-map for long timelines
- Multi-monitor support

---

**Last Updated**: 2025-11-18
**Version**: 1.0 (Initial Refactor)
**Maintainer**: Aura Video Studio Team
