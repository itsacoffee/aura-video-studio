# Timeline Editor and Overlays

## Overview

The Aura Video Studio timeline editor provides professional video editing capabilities including multi-track editing, clip operations, chapter markers, and text overlays.

## Features

### Multi-Track Timeline

The timeline consists of four tracks:
- **V1** (Video 1): Primary video track
- **V2** (Video 2): Secondary video track for compositing
- **A1** (Audio 1): Primary audio track
- **A2** (Audio 2): Secondary audio track for music/effects

### Timeline Operations

#### Split (S)
Splits a clip at the playhead position, creating two separate clips.

**Usage:**
1. Select a clip
2. Position the playhead where you want to split
3. Press `S` or click the Split button

**Behavior:**
- Creates two clips with adjusted source in/out points
- First clip ends at split point, second clip begins at split point
- Both clips maintain their original source material

#### Ripple Trim
Trims a clip and automatically shifts all subsequent clips to close or open gaps.

**Behavior:**
- Adjusts clip in-point or out-point
- Shifts all clips after the trimmed clip by the trim delta
- Maintains no gaps in the timeline

#### Slip
Changes the source in/out points of a clip without changing its timeline position or duration.

**Behavior:**
- Adjusts which portion of the source material is shown
- Timeline position and duration remain unchanged
- Useful for adjusting timing within a clip

#### Slide
Moves a clip along the timeline while adjusting adjacent clips to accommodate the move.

**Behavior:**
- Changes the clip's timeline position
- Adjusts surrounding clips to fill gaps or make space
- Maintains the clip's source in/out points

#### Roll
Adjusts the edit point between two adjacent clips, extending one and trimming the other.

**Behavior:**
- Extends the first clip's out-point
- Advances the second clip's in-point
- Edit point moves but overall duration stays the same

### Text Overlays

Three types of text overlays are supported:

#### Title
Large, prominent text typically displayed at the top center of the frame.

**Default Settings:**
- Font Size: 72px
- Position: Top Center
- Background: Black with 70% opacity
- Border: 2px white

#### Lower Third
Smaller text displayed at the bottom of the frame, often used for speaker names or captions.

**Default Settings:**
- Font Size: 36px
- Position: Bottom Left
- Background: Blue (#000080) with 85% opacity
- No border

#### Callout
Attention-grabbing text used to highlight important information.

**Default Settings:**
- Font Size: 48px
- Position: Middle Right
- Font Color: Yellow
- Background: Black with 80% opacity
- Border: 3px yellow

### Safe Area Alignment

Overlays can be positioned using safe area presets:
- **Top:** Left, Center, Right
- **Middle:** Left, Center, Right
- **Bottom:** Left, Center, Right
- **Custom:** Specify exact X/Y coordinates

Safe margins ensure overlays aren't cut off on different displays (50px margin by default).

### Chapter Markers

Chapter markers create timestamps for video chapters, useful for YouTube and other platforms.

**YouTube Format:**
```
0:00 Introduction
2:30 Main Content
15:45 Conclusion
```

**Features:**
- Click "Add Marker" to create a chapter at the current playhead position
- Markers are automatically sorted by time
- Export to YouTube-compatible format
- Visual indicators on the timeline ruler

### Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `Space` | Play/Pause |
| `J` | Rewind (not implemented yet) |
| `K` | Stop (not implemented yet) |
| `L` | Fast Forward (not implemented yet) |
| `+` or `=` | Zoom In |
| `-` | Zoom Out |
| `S` | Split Clip at Playhead |
| `Q` | Seek Backward 1 second |
| `W` | Seek Forward 1 second |

### Snapping

When enabled, the playhead and clip edges snap to:
- Clip boundaries (start/end)
- Chapter markers
- Timeline start (0:00)

**Snap Threshold:** 100ms (configurable)

**Toggle:** Use the Snapping switch in the toolbar

### Zoom

Adjust the timeline zoom level to view more or less detail:
- **Min:** 10% (0.1x)
- **Max:** 300% (3.0x)
- **Default:** 100% (1.0x)

Use the zoom slider or `+`/`-` keys to adjust.

## FFmpeg Integration

### Overlay Rendering

Text overlays are converted to FFmpeg `drawtext` filters with the following features:

**Culture-Invariant:**
- All numeric values use `CultureInfo.InvariantCulture`
- Ensures consistent output across different locales
- Time values formatted with 3 decimal places

**Text Escaping:**
- Special characters are properly escaped for FFmpeg
- `\` → `\\`
- `'` → `\'`
- `:` → `\:`
- `%` → `\%`

**Example Filter:**
```
drawtext=text='My Title':fontsize=72:fontcolor=white:x=960:y=50:box=1:boxcolor=black@0.70:enable='between(t,1.000,5.000)'
```

### Deterministic Output

Overlays are processed in deterministic order:
1. Sort by `inTime`
2. Sort by `id` for same time values
3. Apply filters in sequence

This ensures consistent render output for the same timeline configuration.

## Implementation Details

### Core (C#)

**TimelineModel.cs**
- Main timeline state management
- Track and clip organization
- Undo/redo stack implementation
- Snapping logic

**Operations/*.cs**
- Individual operation implementations
- Atomic clip boundary updates
- State validation

**OverlayModel.cs**
- Overlay data model
- Position calculation
- FFmpeg filter generation
- Culture-invariant serialization

**FFmpegPlanBuilder.cs**
- Extended with `BuildFilterGraphWithOverlays` method
- Integrates overlays into render pipeline
- Maintains deterministic ordering

### Frontend (React + TypeScript)

**state/timeline.ts**
- Zustand store for timeline state
- Client-side operation implementations
- Chapter export logic

**components/Timeline/TimelineView.tsx**
- Visual timeline display
- Track and clip rendering
- Playhead and marker visualization
- Keyboard shortcut handling

**components/Overlays/OverlayPanel.tsx**
- Overlay creation and editing UI
- Style customization
- Preview integration

## Testing

### Unit Tests (C#)

**TimelineOperationsTests.cs**
- Split operation validation
- Ripple trim with subsequent clips
- Slip without position change
- Slide with adjacent adjustment
- Roll operation between clips
- Undo/redo state restoration
- Chapter export formatting
- Snapping behavior

**OverlayTests.cs**
- Culture-invariant formatting
- Special character escaping
- Default overlay creation
- Position calculation
- FFmpeg filter generation
- Deterministic ordering

**Coverage:** 30 tests, all passing

### Frontend Tests

**Vitest**
- State management operations
- Keyboard shortcut dispatch
- Overlay mutations

**Playwright (E2E)**
- Split clip workflow
- Add lower-third overlay
- Verify render plan JSON

## Best Practices

### Timeline Editing

1. **Use Snapping:** Keep snapping enabled for precise edits
2. **Keyboard Shortcuts:** Learn shortcuts for faster editing
3. **Undo/Redo:** Don't hesitate to experiment; undo is always available
4. **Zoom:** Zoom in for precise edits, zoom out for overview

### Overlays

1. **Timing:** Set appropriate in/out times for readability
2. **Safe Areas:** Use presets to ensure overlays aren't cut off
3. **Contrast:** Ensure text is readable against video content
4. **Consistency:** Use similar styles for overlays of the same type

### Chapter Markers

1. **Key Moments:** Place markers at significant content changes
2. **Descriptive Titles:** Use clear, concise chapter names
3. **Reasonable Length:** Aim for chapters 2-5 minutes long
4. **Start at Zero:** Always include a marker at 0:00

## Limitations

- No real-time video preview (UI mockup only)
- J/K/L playback controls not yet implemented
- Drag-and-drop clip reordering not yet implemented
- No waveform visualization for audio tracks

## Future Enhancements

Planned features:
- Real-time video preview
- Drag-and-drop editing
- Audio waveform display
- Transition effects
- Color grading
- Multi-camera editing
- Proxy workflows for large files

## Related Documentation

- [UX Guide](./UX_GUIDE.md)
- [TTS and Captions](./TTS-and-Captions.md)
- [Build and Run](../BUILD_AND_RUN.md)
