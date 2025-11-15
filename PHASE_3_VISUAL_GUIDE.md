# Phase 3 Video Editor UI - Visual Guide

## Overview

This visual guide documents the Phase 3 enhancements that bring Aura Video Studio to Adobe Premiere Pro and CapCut level user experience. Phase 3 adds professional navigation, playback controls, smooth animations, and advanced editing modes.

---

## 1. Timeline Mini-Map

### Purpose
Provides bird's-eye view of entire timeline for instant navigation, similar to Premiere Pro's timeline overview and CapCut's navigator.

### Visual Design

```
┌─────────────────────────────────────────────────────────────┐
│ ▓▓▓░░░ ▓▓ ░░░░▓▓▓ ░░░ ▓▓▓   ░░░ ▓▓  ░░░▓▓▓  ░░░ ▓▓     [+] │ ← Mini-map header
│ ████░░ ██ ░░░████ ░░░ ███   ░░░ ██  ░░████  ░░░ ██         │
│ ████░░ ██ ░░░████ ░░░ ███   ░░░ ██  ░░████  ░░░ ██         │
│      └──────┬─────┘                                         │
│             │ Current viewport indicator                    │
│             ▼ (accent blue border + glow)                   │
└─────────────────────────────────────────────────────────────┘
         ▲ Playhead (red line)
         
Legend:
▓ = Video clips (darker gray-blue)
░ = Audio clips (cyan-teal)  
█ = Image clips (purple)
```

### Features

**Default State (Collapsed - 48px height)**:
- Shows all clips color-coded by type
- Playhead indicator (red vertical line)
- Viewport rectangle (accent blue with transparency)
- Toggle button (+) in top-right corner

**Expanded State (80px height)**:
- Larger view for better visibility
- More vertical space per track
- Clearer clip separation
- Toggle button (−) to collapse

**Interaction States**:

Hover:
```
┌─────────────────────┐
│     ┌───────┐       │ ← Tooltip appears
│     │01:25:15│       │   Shows timecode at cursor
│     └───────┘       │
│ ▓▓▓░░░ ▓▓      ▼    │   Cursor position
└─────────────────────┘
```

Click:
- Instant jump to clicked timeline position
- Playhead moves to new position
- Timeline viewport follows

### Color Coding

```
Video Clips:  ▓▓▓  (#4a5568 - slate gray-blue)
Audio Clips:  ░░░  (#2d7a8f - cyan-teal)
Image Clips:  ███  (#6b46c1 - purple)
Playhead:     │    (#ff4444 - red)
Viewport:     ┌┐   (#0ea5e9 - accent blue)
```

### Layout Position

```
┌─────────────────────────────────────────────┐
│ Timeline Toolbar                             │
├─────────────────────────────────────────────┤
│                                              │
│ Timeline Ruler                               │
│                                              │
│ Video Track 1  ▓▓▓  ░░░  ▓▓▓                │
│ Video Track 2     ▓▓▓        ░░░            │
│ Audio Track 1  ░░░░░░░░░░░░░░░░░            │
│                                              │
├─────────────────────────────────────────────┤
│ ▓▓▓░░░ ▓▓ ░░░▓▓▓  [Mini-Map]            [+] │ ← Bottom of timeline
└─────────────────────────────────────────────┘
```

---

## 2. Enhanced Playback Controls

### Purpose
Professional NLE-standard playback controls with J-K-L shuttle, frame stepping, and speed control.

### Visual Design

```
┌────────────────────────────────────────────────────────────────────────┐
│ [⏮]  [◄]  [▶ Play]  [►]  [⏭]  ┊  [00:01:24:15 / 00:03:45:22]  ┊  [1x] │
│  ↑    ↑      ↑       ↑     ↑          ↑ Timecode                  ↑   │
│  │    │      │       │     │          (HH:MM:SS:FF)                │   │
│  │    │      │       │     └─ Jump to end                    Speed  │   │
│  │    │      │       └─ Next frame (.)                     selector │   │
│  │    │      └─ Play/Pause (Space/K)                                │   │
│  │    └─ Previous frame (,)                                         │   │
│  └─ Jump to start                                                   │   │
│                                                                      │   │
│ J-K-L: Shuttle • Space: Play/Pause • , .: Frame Step               │   │
│  ↑ Keyboard shortcut hints                                         │   │
└────────────────────────────────────────────────────────────────────────┘
```

### Button States

**Default State**:
```
┌──────┐
│ [⏮] │  Background: --editor-panel-bg
│      │  Border: --editor-panel-border
└──────┘  Color: --editor-text-primary
```

**Hover State**:
```
┌──────┐
│ [⏮] │  Background: --editor-panel-hover
│  ↑   │  Color: --editor-accent
│  └─ Lifts 1px with shadow
└──────┘
```

**Active State**:
```
┌──────┐
│ [⏮] │  Transform: scale(0.98)
│  ↓   │  Tactile press feedback
└──────┘
```

**Play Button (Primary)**:
```
┌─────────┐
│ ▶ Play  │  Background: --editor-accent
│         │  Color: white
│  44x44  │  Larger than other buttons
└─────────┘
```

### Speed Selector

**Closed State**:
```
┌──────┐
│ 1x ▼ │  Shows current speed
│      │  Click to open menu
└──────┘
```

**Open State**:
```
     ┌──────┐
     │ 4x   │ ← Hover: slide right 2px
     ├──────┤
     │ 2x   │
     ├──────┤
     │ 1.5x │
     ├──────┤
     │ 1x   │ ← Active (accent background)
     ├──────┤
     │ 0.5x │
     ├──────┤
     │ 0.25x│
     └──────┘
     ▲
     Menu appears above button
     Smooth fade-in animation
```

### J-K-L Shuttle Behavior

```
Press J once:   ◄─── Play backwards 1x
Press J twice:  ◄◄── Play backwards 2x
Press J 3x:     ◄◄◄─ Play backwards 4x

Press K:        ■ Stop/Pause

Press L once:   ───► Play forwards 1x
Press L twice:  ──►► Play forwards 2x
Press L 3x:     ─►►► Play forwards 4x
```

### Frame Stepping Behavior

```
Current frame: [12]

Press comma (,):   [11] ← Step back one frame
Press period (.):  [13] ← Step forward one frame

Visual feedback:
  Timeline playhead moves one frame
  Timecode updates immediately
  Preview updates to new frame
```

### Timecode Display

```
┌─────────────────────────┐
│ 00:01:24:15 / 00:03:45:22│
│  ↑        ↑    ↑        ↑ │
│  Hours    Mins Secs Frames│
│           │              │
│    Current position      │
│                          │
│              Total duration│
└─────────────────────────┘

Background: --editor-bg-elevated
Font: Monospace
Weight: Semibold
```

---

## 3. Panel Animation System

### Purpose
Spring-based physics animations for natural, fluid panel transitions.

### Spring Physics Visualization

```
Target: 320px

     Stiff Preset:
320 ┤     ╭────────
    │    ╱
240 ┤   ╱
    │  ╱
160 ┤ ╱
    │╱
 80 ┤
    └─────────────► time

     Gentle Preset:
320 ┤       ╭───╮
    │      ╱    ╰─
240 ┤     ╱
    │    ╱
160 ┤  ╱
    │ ╱
 80 ┤╱
    └─────────────► time

     Wobbly Preset:
320 ┤    ╭─╮╭╮
    │   ╱  ╰╯╰──
240 ┤  ╱
    │ ╱
160 ┤╱
    │
 80 ┤
    └─────────────► time
```

### Panel Collapse Animation

**Frame-by-Frame Breakdown**:

```
Frame 0ms (Start):
┌────────────────────────────────┐
│ Media Library                   │
│                                 │
│ [Asset thumbnails]              │
│ [File list]                     │
│                                 │
│                                 │
│ Width: 320px                    │
└────────────────────────────────┘

Frame 50ms:
┌──────────────────────┐
│ Media Library         │
│                      │
│ [Assets fading]      │
│ [List fading]        │
│                      │
│ Width: 240px         │
└──────────────────────┘

Frame 100ms:
┌────────────┐
│ Media Lib  │
│            │
│ [Icons]    │
│            │
│ Width: 120px│
└────────────┘

Frame 150ms:
┌──────┐
│ ML   │
│      │
│ [I]  │
│      │
│ 80px │
└──────┘

Frame 200ms (End):
┌───┐
│ML │ ← Collapsed state
│   │    Width: 48px
│🎬 │    Icon-only view
│   │
└───┘
```

### Panel Swap Animation

**Three-Phase Transition**:

```
Phase 1: Fade Out (0-150ms)
┌─────────────────────┐        ┌─────────────────────┐
│ Panel A             │        │ Panel A             │
│                     │  ═══►  │ (opacity: 0.5)      │
│ [Content visible]   │        │ [Content fading]    │
└─────────────────────┘        └─────────────────────┘

Phase 2: Content Swap (150-200ms)
┌─────────────────────┐
│                     │ ← Blank during swap
│ (opacity: 0)        │    (prevents flash)
│                     │
└─────────────────────┘

Phase 3: Fade In (200-350ms)
┌─────────────────────┐        ┌─────────────────────┐
│ Panel B             │        │ Panel B             │
│ (opacity: 0.5)      │  ═══►  │                     │
│ [New content]       │        │ [Content visible]   │
└─────────────────────┘        └─────────────────────┘
```

---

## 4. Advanced Clip Interactions

### Edit Mode Visual Indicators

**Select Mode (V)**:
```
┌─────────────┐
│ Video Clip  │ ← Standard cursor
│             │   Drag to move
└─────────────┘
    ↑ Selection handles (4 corners)
```

**Ripple Edit Mode (B)**:
```
┌─────────────┐
│ Video Clip  │═►  ┌──────┐  ┌──────┐
│             │    │Clip 2│  │Clip 3│
└─────────────┘    └──────┘  └──────┘
     ↑                 ↑         ↑
     Move this     These shift automatically
```

**Rolling Edit Mode (N)**:
```
┌───────────┐┃┌───────────┐
│  Clip A   │┃│  Clip B   │
└───────────┘┃└───────────┘
             ↑
        Edit point
   (drag left/right)
```

**Slip Edit Mode (Y)**:
```
Source clip: ┤─────[●●●●●●●●●●]─────┤
Timeline:        ┌──────┐
                 │[●●●]  │ ← Visible portion
                 └──────┘
                     ↑
                Drag to "slip" window
```

**Slide Edit Mode (U)**:
```
Before:
┌─────┐    ┌──────┐    ┌─────┐
│Clip1│    │Clip2 │    │Clip3│
└─────┘    └──────┘    └─────┘

After sliding Clip2 right:
┌─────────┐  ┌──────┐  ┌──┐
│ Clip1   │  │Clip2 │  │C3│
└─────────┘  └──────┘  └──┘
    ↑                      ↑
  Extends            Shortens
```

### Magnetic Timeline Behavior

**Snapping Visualization**:

```
No snap (magnetic off):
┌──────┐      ┌──────┐
│Clip1 │  gap │Clip2 │
└──────┘      └──────┘
              ↑ Small gap allowed

With snap (magnetic on):
┌──────┐┌──────┐
│Clip1 ││Clip2 │ ← Snaps together
└──────┘└──────┘
        ╱
       ▼ Snap guide appears
```

**Snap Guide Appearance**:

```
During drag:
┌──────┐        ║
│ Clip │        ║ ← Snap guide (accent color)
└──────┘        ║   Shows snap point
       ↓        ║
   ┌─────────┐  ║
   │Clip Start│  ║ ← Label with name
   └─────────┘  ║
```

### Ghost Preview During Drag

```
Original position:
┌──────────┐
│ Video 1  │ (normal opacity)
└──────────┘

While dragging:
┌─ ─ ─ ─ ─┐  ← Ghost preview
  Video 1      (50% opacity, dashed border)
└─ ─ ─ ─ ─┘    Shows destination

         +
         
┌──────────┐
│ Video 1  │ (30% opacity at original position)
└──────────┘
```

### Trim Handles

**Normal State**:
```
┌──────────────┐
│   Video 1    │
└──────────────┘
↑              ↑
Left trim     Right trim
(resize cursor)
```

**Hover State**:
```
▐▌─────────────┐
││   Video 1    │ ← Left handle highlighted
└──────────────┘
↑
w-resize cursor
```

**During Trim**:
```
▐▌───────┐
││Video 1│ ← Real-time duration update
└────────┘
 ↑
Snap to frames
```

---

## Color System Reference

### Edit Mode Colors

```
Select Mode:     Default state colors
Ripple Mode:     Accent blue indicators
Rolling Mode:    Split cursor (ew-resize)
Slip Mode:       Move cursor
Slide Mode:      Grab/grabbing cursor
Trim Mode:       Resize cursors (w/e-resize)
```

### Animation Colors

```
Panel Background:      --editor-panel-bg (#1e1e1e)
Panel Hover:           --editor-panel-hover (#2f2f2f)
Panel Border:          --editor-panel-border (#3a3a3a)
Accent:                --editor-accent (#0ea5e9)
Accent Hover:          --editor-accent-hover (#38bdf8)
```

### Clip Type Colors

```
Video:   #4a5568  ▓▓▓  (slate gray-blue)
Audio:   #2d7a8f  ░░░  (cyan-teal)
Image:   #6b46c1  ███  (purple)
```

---

## Keyboard Shortcuts Reference

### Playback Controls
```
Space       Play/Pause
K           Pause (pro shuttle)
J           Play backwards (multi-press for speed)
L           Play forwards (multi-press for speed)
,           Previous frame
.           Next frame
Home        Jump to start
End         Jump to end
```

### Edit Modes
```
V           Select mode
B           Ripple edit mode
N           Rolling edit mode
Y           Slip edit mode
U           Slide edit mode
T           Trim mode
```

### Timeline Navigation
```
Click on mini-map    Jump to position
Drag on mini-map     Scrub through timeline
+ on mini-map        Expand mini-map
− on mini-map        Collapse mini-map
```

---

## Integration Examples

### Complete Editor Layout

```
┌─────────────────────────────────────────────────────────────┐
│ Menu Bar: File Edit View Insert Sequence Clip Window Help   │
├───────┬──────────────────────────────────────────┬──────────┤
│Media  │                                          │Properties│
│Lib    │       Video Preview                      │          │
│       │                                          │ Transform│
│🎬     │   [Video player with playback controls]  │ X: 0     │
│📁     │                                          │ Y: 0     │
│🎵     │                                          │ Scale: 1 │
│       ├──────────────────────────────────────────┤          │
│       │ [Playback Controls Bar]                 │          │
│       │ [⏮][◄][▶][►][⏭] [00:01:24:15] [1x]     │          │
├───────┼──────────────────────────────────────────┤──────────┤
│Effects│ Timeline                                 │ Audio    │
│       │                                          │ Meters   │
│⚡    │ Video 1  ▓▓▓ ░░░ ▓▓▓                     │ ▮▮▮▮     │
│🎨     │ Video 2     ▓▓▓     ░░░                 │ ▮▮▮      │
│       │ Audio 1  ░░░░░░░░░░░░░░                 │          │
│       ├──────────────────────────────────────────┤          │
│       │ [Timeline Mini-Map]              [+]   │          │
└───────┴──────────────────────────────────────────┴──────────┘
```

---

## Animation Timing Reference

```
Fast transitions:    150ms  (button hover, quick feedback)
Base transitions:    250ms  (panel fade, menu appear)
Slow transitions:    350ms  (panel swap, complex animations)

Spring presets:
- gentle:  Natural, smooth (workspace changes)
- stiff:   Quick, responsive (panel resize)
- wobbly:  Playful, bouncy (success feedback)
- slow:    Deliberate (important transitions)
```

---

## Conclusion

Phase 3 brings Aura Video Studio to professional NLE standards with:

✅ Professional timeline navigation (mini-map)
✅ Industry-standard playback controls (J-K-L)
✅ Natural spring-based animations
✅ Advanced editing modes (5 modes)
✅ Magnetic timeline with smart snapping
✅ Complete keyboard workflow

The visual design matches Adobe Premiere Pro and CapCut while maintaining Aura's unique identity through the established dark theme and accent color system.

---

**Document Version**: 1.0  
**Last Updated**: 2025-11-15  
**Related**: PHASE_3_IMPLEMENTATION_SUMMARY.md
