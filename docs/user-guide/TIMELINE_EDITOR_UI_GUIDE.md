# Timeline Editor UI Guide

## Visual Layout

**Note**: The default layout shown below uses the "Editing" workspace preset (60/30/10 proportions). The layout can be changed using workspace presets in the View menu. See [Workspace Presets](#workspace-presets) section for details.

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ Timeline Editor                            [Back] [Save] [Generate Preview]  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                               │
│                         PREVIEW PANEL (60%)                                  │
│                                                                               │
│  ┌─────────────────────────────────────────────────────────────┐            │
│  │                                                               │            │
│  │                    Video Player (16:9)                       │  60%       │
│  │              [Play/Pause] [<<] [>>]                          │            │
│  │         ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━              │            │
│  │         00:00:00 / 00:02:30    [🔊 ━━━━━] [1x ▼] [⛶]       │            │
│  └─────────────────────────────────────────────────────────────┘            │
│                                                                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                               │
│                         TIMELINE PANEL (30%)                                 │
│  [Zoom In] [Zoom Out] Zoom: 50px/s                                          │
│                                                                               │  30%
│  ┌───────┬─────┬────────┬──────┬───────┬─────────┐                         │
│  │Scene 1│Sc 2 │ Scene 3│ Sc 4 │Scene 5│  Sc 6   │  ← Scene blocks         │
│  │5.2s   │3.8s │  6.1s  │4.5s  │ 5.9s  │  4.2s   │                         │
│  └───────┴─────┴────────┴──────┴───────┴─────────┘                         │
│       ▲ Playhead                                                             │
│                                                                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                      PROPERTIES PANEL (10%)                                  │
│                                                                               │  10%
│  Scene 2 Properties  │  [Duplicate] [Delete]                                │
│  Heading: [Introduction Scene                     ]                          │
│  Duration: [3.8] seconds    Transition: [Fade ▼] [0.5]s                    │
├─────────────────────────────────────────────────────────────────────────────┘
```

## Panel Descriptions

### 1. Header Bar
- **Title**: "Timeline Editor" on the left
- **Actions** (right side):
  - "Back to Jobs" - Returns to job list
  - "Save" - Manual save button (shows state: "Saving...", "Saved", "Unsaved changes")
  - "Generate Preview" - Triggers preview rendering

### 2. Preview Panel (60% of viewport)

**Video Player Features:**
```
┌─────────────────────────────────────────────┐
│                                             │
│         Video Display (16:9 aspect)         │
│      Black letterboxing if needed           │
│                                             │
└─────────────────────────────────────────────┘
┌─────────────────────────────────────────────┐
│ [▶️/⏸️] [◀] [▶]  ━━━━━━━━━━━━━━━━━━━━━━━  │
│                   00:12:45 / 02:30:15       │
│                   [🔊 ━━━] [1x ▼] [⛶]      │
└─────────────────────────────────────────────┘
```

**Controls:**
- **Play/Pause Button** - Toggle playback (spacebar hotkey)
- **Previous Frame** (◀) - Step back 1 frame (~33ms at 30fps)
- **Next Frame** (▶) - Step forward 1 frame
- **Seek Bar** - Click to jump, shows current position
- **Timecode** - MM:SS:FF format (minutes:seconds:frames)
- **Volume Control** - Slider + mute button
- **Speed Selector** - Dropdown (0.25x, 0.5x, 1x, 2x)
- **Fullscreen** - Expand to full window

**Before Preview Generated:**
```
┌─────────────────────────────────────────────┐
│                                             │
│   Preview will appear after rendering       │
│                                             │
│   [Generate Preview] button                 │
│                                             │
└─────────────────────────────────────────────┘
```

### 3. Timeline Panel (30% of viewport)

**Timeline Controls:**
```
[+ Zoom In] [- Zoom Out]  Zoom: 50px/s
```
- Adjust horizontal scale (10px/s to 200px/s)
- More zoom = more detail, less visible timeline

**Scene Blocks:**
```
┌─────────────┬──────────┬─────────────┬──────────┐
│  Scene 1    │ Scene 2  │  Scene 3    │ Scene 4  │
│  Intro      │ Main     │  Details    │ Outro    │
│  5.2s       │  3.8s    │  6.1s       │  4.5s    │
└─────────────┴──────────┴─────────────┴──────────┘
      ▲
   Playhead (red line)
```

**Scene Block Details:**
- **Width**: Proportional to duration (duration × zoom)
- **Background**: Light gray (selected = blue highlight)
- **Border**: 2px solid (selected = 3px blue)
- **Content**:
  - Top: Scene heading (truncated if too long)
  - Bottom: Duration in seconds
- **Hover**: Blue border
- **Click**: Selects scene, shows properties below

**Playhead:**
- Red vertical line (2px wide)
- Moves during playback
- Shows current playback position
- Synchronized with video player

**Layout:**
- Horizontal scroll if timeline wider than viewport
- Scenes laid out left-to-right
- Start times calculated automatically

### 4. Properties Panel (10% of viewport, expandable)

**When Scene Selected:**

```
┌─────────────────────────────────────────────────────────┐
│ Scene Properties                  [Duplicate] [Delete]   │
├─────────────────────────────────────────────────────────┤
│ Scene 2                                                  │
│ Heading: [Introduction to the topic_____________]        │
│                                                          │
│ Script:                                                  │
│ ┌────────────────────────────────────────────────────┐  │
│ │ Welcome to this comprehensive guide on...         │  │
│ │ In this video we'll explore...                    │  │
│ │                                                    │  │
│ └────────────────────────────────────────────────────┘  │
│                                                          │
│ Duration: [3.8] seconds                                  │
│ Transition: [Fade ▼]  Duration: [0.5] seconds          │
└─────────────────────────────────────────────────────────┘
```

**Visual Assets Section:**

```
┌─────────────────────────────────────────────────────────┐
│ Visual Assets              [+ Import Asset]              │
├─────────────────────────────────────────────────────────┤
│ ┌───┬─────────────────────────────────────────┬────┐    │
│ │📷 │ Image • Z: 1 • Opacity: 100%           │ 🗑️ │    │
│ └───┴─────────────────────────────────────────┴────┘    │
│ ┌───┬─────────────────────────────────────────┬────┐    │
│ │🎬 │ Video • Z: 0 • Opacity: 80%            │ 🗑️ │    │
│ └───┴─────────────────────────────────────────┴────┘    │
└─────────────────────────────────────────────────────────┘
```

**When Asset Selected:**

```
┌─────────────────────────────────────────────────────────┐
│ Asset Properties                                         │
├─────────────────────────────────────────────────────────┤
│ Position X (%):  ━━━━●━━━━━━  50                       │
│ Position Y (%):  ━━━━━●━━━━━  40                       │
│ Width (%):       ━━━━━━━━●━━  80                       │
│ Height (%):      ━━━━━━━━●━━  75                       │
│                                                          │
│ Opacity:         ━━━━━━━━━●━  90%                      │
│ Z-Index:         [1_____]                               │
│                                                          │
│ Effects:                                                 │
│ Brightness:      ━━━━━●━━━━━  1.2                      │
│ Contrast:        ━━━━━●━━━━━  1.0                      │
│ Saturation:      ━━━━━●━━━━━  1.1                      │
└─────────────────────────────────────────────────────────┘
```

## User Interactions

### Scene Editing Flow

1. **Select Scene**
   - Click scene block in timeline
   - Block highlights with blue border
   - Properties panel updates

2. **Edit Properties**
   - Type in heading field → auto-saves after 5s
   - Edit script in textarea → auto-saves after 5s
   - Change duration → updates timeline width
   - Select transition → configures transition

3. **Save Indicator**
   - Shows "Unsaved changes" when dirty
   - Shows "Saving..." during save
   - Shows "Saved at HH:MM:SS" after save

### Asset Management Flow

1. **Import Asset**
   - Click "Import Asset" button
   - File picker opens (images/videos only)
   - Upload to server
   - Asset added to scene
   - Auto-saves

2. **Select Asset**
   - Click asset in list
   - Asset properties appear below
   - Can adjust position, size, effects

3. **Adjust Properties**
   - Drag sliders to change values
   - Updates save automatically after 5s
   - Visual feedback on slider movement

4. **Delete Asset**
   - Click delete (🗑️) icon
   - Asset removed from scene
   - Auto-saves

### Preview Generation Flow

1. **Generate Preview**
   - Click "Generate Preview" button
   - Button shows "Generating..."
   - Progress could be shown (future)
   - Preview video loads in player

2. **Playback**
   - Click play or press spacebar
   - Video plays from current position
   - Playhead moves in sync
   - Can seek, adjust volume, speed

3. **Final Render**
   - Click "Render Final" (could be added to header)
   - High-quality render generates
   - Can download when complete

## Color Scheme (Dark Theme)

**Backgrounds:**
- Primary: `#1e1e1e` (dark gray)
- Secondary: `#252526` (lighter dark gray)
- Accent: `#2d2d30` (lightest gray)

**Borders:**
- Default: `#3e3e42` (subtle gray)
- Selected: `#0078d4` (Microsoft blue)
- Hover: `#005a9e` (darker blue)

**Text:**
- Primary: `#cccccc` (light gray)
- Secondary: `#999999` (medium gray)
- Accent: `#0078d4` (blue for links/actions)

**Scene Blocks:**
- Background: `#2d2d30`
- Border: `#3e3e42` → `#0078d4` (hover/selected)
- Text: `#cccccc`

**Playhead:**
- Color: `#d13438` (red for visibility)

## Responsive Behavior

**Desktop (≥1200px):**
- Full three-panel layout
- Properties panel 10% minimum height
- Timeline scrolls horizontally if needed

**Tablet (768px - 1199px):**
- Maintain three-panel layout
- Reduce panel heights slightly
- Ensure controls remain accessible

**Mobile (≤767px):**
- Stack panels vertically
- Preview takes full width
- Timeline scrolls horizontally
- Properties become modal/sheet

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| Space | Play/Pause |
| ← | Previous frame |
| → | Next frame |
| Ctrl+S | Save timeline |
| Ctrl+Z | Undo (future) |
| Ctrl+Y | Redo (future) |

## Loading States

**Timeline Loading:**
```
┌─────────────────────────────┐
│                             │
│     ⟳ Loading timeline...  │
│                             │
└─────────────────────────────┘
```

**Generating Preview:**
```
┌─────────────────────────────┐
│                             │
│   ⟳ Generating preview...  │
│      Please wait            │
│                             │
└─────────────────────────────┘
```

## Error States

**Load Error:**
```
┌─────────────────────────────┐
│   ⚠️ Error loading timeline │
│   Failed to load timeline   │
│   [Back to Jobs]            │
└─────────────────────────────┘
```

**Save Error:**
```
╔═══════════════════════════╗
║ ⚠️ Failed to save         ║
║ Retrying automatically... ║
╚═══════════════════════════╝
```

## Design Principles

1. **Professional Appearance** - Dark theme like Adobe Premiere
2. **Clear Visual Hierarchy** - Preview > Timeline > Properties
3. **Immediate Feedback** - Hover states, selected states, saving indicators
4. **Keyboard Accessible** - All actions available via keyboard
5. **Responsive** - Works on various screen sizes
6. **Forgiving** - Auto-save prevents data loss
7. **Informative** - Clear labels, tooltips, timecodes
8. **Efficient** - Minimal clicks to accomplish tasks

## Implementation Status

✅ **Fully Implemented:**
- Three-panel layout
- Video player with controls
- Timeline visualization
- Scene property editing
- Asset property controls
- Auto-save mechanism
- Loading/error states

⚠️ **Partially Implemented:**
- Scene blocks (basic rendering, no drag-drop)
- Playhead (visual only, no interaction)
- Zoom controls (UI present, basic functionality)

❌ **Not Implemented:**
- Drag-and-drop scene reordering
- Edge dragging for duration
- Scene thumbnails
- Audio waveforms
- Undo/redo
- Advanced keyboard shortcuts

These can be added in future iterations with appropriate libraries and additional development.

## Workspace Presets

The video editor supports multiple workspace presets (similar to Adobe Premiere Pro) to optimize the layout for different tasks. Access presets via:
- **View → Workspace Switcher** dropdown (toolbar)
- **Keyboard shortcuts**: Alt+1 through Alt+5
- **View → Reset Layout** to restore default

### Available Presets

**1. Editing (Default)** - `Alt+1`
- 60% preview, 30% timeline, 10% properties
- All side panels collapsed for focused editing
- Balanced layout for general video editing

**2. Focus: Preview**
- 75% preview, 25% timeline
- Maximized preview area for detailed viewing
- Ideal for color grading and visual inspection
- All side panels hidden

**3. Focus: Timeline**
- 40% preview, 60% timeline
- Maximized timeline for precise multi-track editing
- Perfect for complex edits with many clips
- All side panels hidden

**4. Minimal Sidebar**
- Compact panel widths (240-280px)
- Reduced visual clutter
- Easy access to tools when needed

**5. Color** - `Alt+2`
- 70% preview for accurate color evaluation
- Properties panel visible (350px)
- Optimized for color grading work

**6. Audio** - `Alt+4`
- 50/50 preview/timeline split
- Properties and Media Library visible
- Optimal for audio editing workflows

**7. Effects** - `Alt+3`
- Effects Library panel expanded (300px)
- Properties panel visible for adjustments
- Efficient effects application workflow

**8. Assembly** - `Alt+5`
- Media Library visible (300px)
- 55/45 preview/timeline split
- Ideal for rough cuts and organization

### Panel Visibility Controls

Individual panels can be shown/hidden via **View → Panels**:

- ✓ **Properties Panel** - Clip properties and effects controls
- ✓ **Media Library Panel** - Asset browser and import
- ✓ **Effects Panel** - Effect presets and library
- ✓ **History Panel** - Undo/redo visualization

**Note**: Preview and Timeline panels are always visible (critical for editing).

### Reset Layout

To restore default layout and panel sizes:
- **View → Reset Layout** or press `Alt+0`
- Clears all customized panel sizes
- Returns to "Editing" preset
- Shows confirmation notification

### Layout Persistence

- Panel sizes and positions persist across sessions
- Collapsed/visible states saved automatically
- Custom adjustments maintained until reset
- Active preset tracked and restored on reload
