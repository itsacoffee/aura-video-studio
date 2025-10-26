# Timeline Professional Editing Features

## Overview

The Aura Video Studio timeline has been enhanced with broadcast-quality professional editing features including frame-accurate operations, magnetic timeline, professional trim modes, and smooth playback controls.

## Features

### Frame-Accurate Editing

All timeline operations are frame-accurate with precise positioning:

- **Frame Snapping**: All clip positions and edits snap to exact frame boundaries
- **Timecode Display**: Shows time in HH:MM:SS:FF format (Hours:Minutes:Seconds:Frames)
- **Frame Navigation**: Use arrow keys to navigate frame-by-frame

### Display Modes

Switch between different timeline display modes:

1. **Timecode** (HH:MM:SS:FF): Professional timecode display
2. **Frames**: Show timeline in frame numbers
3. **Seconds**: Display time in decimal seconds

### Magnetic Timeline

Enable magnetic timeline to automatically close gaps between clips:

- **Auto-Snap**: Clips automatically snap to adjacent clips when moved
- **Gap Closing**: Gaps between clips are automatically eliminated
- **Toggle**: Enable/disable via toolbar switch

### Professional Trim Modes

#### Ripple Edit
- Adjusts all clips following the edit point
- Maintains clip relationships
- Ideal for adding or removing content while keeping timeline synchronized

#### Roll Edit
- Adjusts the boundary between two adjacent clips
- Keeps overall timeline duration unchanged
- Perfect for fine-tuning edit points

#### Slip Edit
- Changes the in/out points of a clip without changing its position or duration
- Allows adjusting which part of the source media is used
- Useful for timing adjustments

#### Slide Edit
- Moves a clip along the timeline without changing its in/out points
- Adjusts adjacent clips to accommodate the move
- Maintains the clip's content while changing its timing

### Tools

#### Select Tool
- Default tool for selecting and moving clips
- Drag clips to reposition
- Click to select clips

#### Razor Tool
- Split clips at any frame position
- Click on timeline to split clip at playhead
- Creates two separate clips with maintained properties

#### Hand Tool
- Pan and navigate the timeline
- Useful for large projects

### Snapping & Guides

Smart snapping with visual feedback:

- **Snap Points**: Clip edges, playhead, markers, in/out points
- **Visual Guides**: Dashed blue lines appear when near snap points
- **Offset Display**: Shows distance in frames when near snap points
- **Toggle**: Enable/disable snapping with toolbar switch

### Playback Controls

#### JKL Shuttle
Professional shuttle controls for variable-speed playback:

- **J**: Reverse playback (press multiple times for faster speed, up to 4x)
- **K**: Pause playback
- **L**: Forward playback (press multiple times for faster speed, up to 4x)

#### Keyboard Shortcuts
- **Spacebar**: Play/Pause
- **Left Arrow**: Step back 1 frame
- **Right Arrow**: Step forward 1 frame
- **Up Arrow**: Jump forward 10 frames
- **Down Arrow**: Jump back 10 frames
- **Shift + Left/Right**: Step back/forward 10 frames

### Zoom Controls

- **Slider**: Adjust zoom from 10-200 pixels per second
- **+/- Buttons**: Increment/decrement zoom
- **Fit to Window**: Automatically zoom to fit all content
- **Mouse-Centered**: Zoom centered on cursor position

### Clip Handles

Visual trim handles on clips:

- **Left Handle**: Trim clip in-point
- **Right Handle**: Trim clip out-point
- **Hover Preview**: Shows trim duration on hover
- **Frame Tooltip**: Displays frame count during trim

### Timeline Ruler

Professional ruler with adaptive tick marks:

- **Major Ticks**: Show primary time intervals
- **Minor Ticks**: Show subdivisions
- **Adaptive**: Tick density adjusts based on zoom level
- **Click to Seek**: Click anywhere on ruler to move playhead

### Playhead

Enhanced playhead indicator:

- **Draggable**: Click and drag to scrub through timeline
- **Frame Snap**: Automatically snaps to frame boundaries
- **Tooltip**: Shows current timecode
- **Keyboard Control**: Navigate with arrow keys

## Performance

Optimized for smooth 60fps performance:

- **Efficient Rendering**: Components update only when needed
- **Smooth Scrolling**: Hardware-accelerated scrolling
- **Large Projects**: Tested with 200+ clips
- **Responsive**: Sub-frame latency for user interactions

## API

### TimelinePanel Props

```typescript
interface TimelinePanelProps {
  clips?: TimelineClip[];
  tracks?: TimelineTrack[];
  currentTime?: number;
  onTimeChange?: (time: number) => void;
  onClipSelect?: (clipId: string | null) => void;
  selectedClipId?: string | null;
  onClipAdd?: (trackId: string, clip: TimelineClip) => void;
  onClipUpdate?: (clipId: string, updates: Partial<TimelineClip>) => void;
  onTrackToggleVisibility?: (trackId: string) => void;
  onTrackToggleLock?: (trackId: string) => void;
}
```

### Timeline Engine Utilities

```typescript
// Frame conversions
secondsToFrames(seconds: number, frameRate?: number): number
framesToSeconds(frames: number, frameRate?: number): number
snapToFrame(seconds: number, frameRate?: number): number

// Formatting
formatTimecode(seconds: number, frameRate?: number): string
formatFrameNumber(seconds: number, frameRate?: number): string
formatSeconds(seconds: number): string

// Snapping
calculateSnapPoints(clips, playheadTime, markers, inPoint?, outPoint?): SnapPoint[]
findNearestSnapPoint(time, snapPoints, threshold?): SnapPoint | null

// Editing operations
applyRippleEdit(clips, editTime, delta): Clip[]
closeGaps(clips): Clip[]
findGaps(clips, trackId?): Gap[]
```

## Examples

### Basic Usage

```typescript
import { TimelinePanel } from './components/EditorLayout/TimelinePanel';

function MyEditor() {
  const [currentTime, setCurrentTime] = useState(0);
  const [clips, setClips] = useState([]);
  
  return (
    <TimelinePanel
      clips={clips}
      currentTime={currentTime}
      onTimeChange={setCurrentTime}
      onClipUpdate={(id, updates) => {
        setClips(clips.map(c => c.id === id ? { ...c, ...updates } : c));
      }}
    />
  );
}
```

### Using Timeline Engine

```typescript
import { snapToFrame, formatTimecode, applyRippleEdit } from './services/timelineEngine';

// Snap time to nearest frame
const snappedTime = snapToFrame(1.234, 30); // 1.233... (37 frames)

// Format as timecode
const timecode = formatTimecode(65.5, 30); // "00:01:05:15"

// Apply ripple edit
const updatedClips = applyRippleEdit(clips, editPoint, delta);
```

## Best Practices

1. **Enable Snapping**: Keep snapping enabled for precise edits
2. **Use Keyboard Shortcuts**: Learn JKL shuttle for efficient editing
3. **Magnetic Timeline**: Enable for quick assembly edits
4. **Trim Modes**: Choose the right mode for each edit type
5. **Frame Navigation**: Use arrow keys for frame-accurate positioning

## Browser Support

- Chrome 90+
- Firefox 88+
- Safari 14+
- Edge 90+

## Known Limitations

- Maximum timeline duration: 24 hours
- Maximum clips per track: Unlimited (tested with 200+)
- Playback speed range: 1x to 4x (forward and reverse)

## Future Enhancements

- Multi-track selection
- Slip/Slide edit visual preview
- Advanced snapping options
- Timeline markers with colors
- Nested timelines
- Timeline search and filter
