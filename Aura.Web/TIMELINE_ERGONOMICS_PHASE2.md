# Timeline Ergonomics Phase 2 - Implementation Guide

## Overview

This document describes the Timeline Ergonomics Phase 2 implementation, which adds professional multi-select, enhanced snapping, ripple editing, precise timecode control, and comprehensive undo/redo support to the Aura Video Studio timeline.

## Features Implemented

### 1. Multi-Selection System

#### State Management

The timeline now supports selecting multiple clips simultaneously:

```typescript
// Select multiple clips
useTimelineStore.getState().setSelectedClipIds(['clip1', 'clip2', 'clip3']);

// Toggle individual clip selection (additive)
useTimelineStore.getState().toggleClipSelection('clip4');

// Select range of clips
useTimelineStore.getState().selectClipRange('clip1', 'clip5');

// Clear all selection
useTimelineStore.getState().clearSelection();
```

#### Keyboard Shortcuts

- **Click**: Select single clip
- **Ctrl+Click**: Toggle clip selection (additive)
- **Shift+Click**: Select range from last selected to clicked clip
- **Ctrl+A**: Select all clips
- **Ctrl+D**: Deselect all clips

#### Batch Operations

Multi-selected clips can be operated on together:

- **Delete**: Remove all selected clips (leaves gaps)
- **Shift+Delete**: Ripple delete all selected clips (closes gaps)
- **Arrow Keys**: Move all selected clips together
- **Split (S)**: Split all selected clips at playhead

### 2. Enhanced Snapping System

#### New Snap Point Types

The snapping system now supports:

- **Clips**: Start and end points (existing)
- **Markers**: Chapter markers (existing)
- **Captions**: Start and end of subtitle/caption tracks
- **Audio Peaks**: Prominent audio transients
- **Scene Boundaries**: Detected scene changes
- **Playhead**: Current playhead position (existing)
- **In/Out Points**: Mark in and mark out points (existing)

#### Configuration

```typescript
// Configure snap behavior
useTimelineStore.getState().setSnapConfig({
  enabled: true,
  thresholdMs: 100, // Snap within 100ms
  snapToClips: true,
  snapToMarkers: true,
  snapToCaptions: true,
  snapToAudioPeaks: true,
  snapToPlayhead: true,
});
```

#### Visual Feedback

When dragging clips near snap points:

- Blue dashed guideline appears at snap position
- Label shows what you're snapping to
- Offset indicator shows distance in frames

### 3. Ripple Edit Mode

#### What is Ripple Edit?

Ripple editing automatically closes gaps when deleting or moving clips, maintaining timeline continuity.

#### Usage

```typescript
// Enable ripple mode
useTimelineStore.getState().setRippleEditMode(true);

// Ripple delete single clip
useTimelineStore.getState().rippleDeleteClip('clip1');

// Ripple delete multiple clips
useTimelineStore.getState().rippleDeleteClips(['clip1', 'clip2']);
```

#### Keyboard Shortcut

- **Ctrl+R**: Toggle ripple edit mode
- **Shift+Delete**: Ripple delete (even when mode is off)

#### Visual Indicator

When ripple mode is enabled, a visual indicator appears on the timeline toolbar.

### 4. Precise Timecode Editing

#### TimecodeEditor Component

Frame-accurate timecode input with validation:

```tsx
import { TimecodeEditor } from '@/components/Timeline/TimecodeEditor';

<TimecodeEditor
  value={currentTime}
  onChange={(seconds) => setCurrentTime(seconds)}
  frameRate={30}
  min={0}
  max={maxDuration}
/>;
```

#### Features

- **Format**: HH:MM:SS:FF (Hours:Minutes:Seconds:Frames)
- **Validation**: Real-time validation with error states
- **Navigation**: Arrow up/down to increment/decrement by one frame
- **Keyboard**: Enter to apply, Escape to cancel
- **Auto-format**: Normalizes input to proper format

#### Examples

- `00:00:05:15` = 5 seconds and 15 frames
- `00:01:30:00` = 1 minute 30 seconds
- `01:23:45:29` = 1 hour, 23 minutes, 45 seconds, 29 frames

### 5. Comprehensive Keyboard Shortcuts

#### Playback Controls

- **Space**: Play/Pause
- **J**: Rewind (press multiple times for faster)
- **K**: Pause
- **L**: Fast forward (press multiple times for faster)

#### Navigation

- **Left Arrow**: Move back 1 frame
- **Right Arrow**: Move forward 1 frame
- **Shift+Left**: Move back 1 second
- **Shift+Right**: Move forward 1 second
- **Home**: Jump to start
- **End**: Jump to end

#### Editing

- **S**: Split clip at playhead
- **Delete**: Delete selected clips
- **Shift+Delete**: Ripple delete selected clips
- **I**: Set in point
- **O**: Set out point
- **X**: Clear in/out points
- **M**: Add marker at playhead

#### Selection

- **Ctrl+A**: Select all clips
- **Ctrl+D**: Deselect all

#### Zoom

- **+** or **=**: Zoom in
- **-**: Zoom out

#### Undo/Redo

- **Ctrl+Z**: Undo
- **Ctrl+Y** or **Ctrl+Shift+Z**: Redo

#### Other

- **Ctrl+R**: Toggle ripple edit mode
- **Ctrl+S**: Save timeline
- **?**: Show keyboard shortcuts help

### 6. Undo/Redo System Phase 2

#### Command Pattern

All timeline operations use the Command pattern for undo/redo:

```typescript
import { DeleteClipsCommand } from '@/commands/timelineCommands';
import { useUndoManager } from '@/state/undoManager';

const { execute } = useUndoManager();

// Execute command (adds to undo stack)
const command = new DeleteClipsCommand(['clip1', 'clip2'], useTimelineStore);
await execute(command);

// Undo/redo automatically handled by manager
```

#### Available Commands

- `SelectClipsCommand`: Change selection
- `DeleteClipsCommand`: Delete clips
- `RippleDeleteClipsCommand`: Ripple delete clips
- `MoveClipCommand`: Move single clip
- `MoveClipsCommand`: Move multiple clips
- `SplitClipCommand`: Split clip
- `AddMarkerCommand`: Add marker
- `ToggleRippleEditCommand`: Toggle ripple mode

#### Grouped Operations

Batch multiple commands into a single undo/redo action:

```typescript
import { BatchCommandImpl } from '@/services/commandHistory';

const batch = new BatchCommandImpl('Move and split clips');
batch.addCommand(new MoveClipCommand('clip1', 10, useTimelineStore));
batch.addCommand(new SplitClipCommand('clip1', 15, useTimelineStore));

await execute(batch); // All commands execute, single undo reverts all
```

#### History Panel UI

View and navigate undo/redo history:

```tsx
import { TimelineHistoryPanel } from '@/components/Timeline/TimelineHistoryPanel';

<TimelineHistoryPanel isOpen={historyPanelOpen} onClose={() => setHistoryPanelOpen(false)} />;
```

Features:

- View action history with timestamps
- See action descriptions
- Undo/redo from panel
- Clear all history
- Keyboard shortcut: **Ctrl+Shift+U**

#### Memory Management

The undo manager maintains a configurable history limit (default: 100 actions) to prevent excessive memory usage. Oldest actions are automatically removed when the limit is reached.

## Integration Examples

### Using Keyboard Shortcuts Hook

```typescript
import { useTimelineShortcutsIntegration } from '@/hooks/useTimelineShortcutsIntegration';

function TimelineEditor() {
  const { handlePlayPause, handleUndo, handleRedo } = useTimelineShortcutsIntegration({
    enabled: true,
    onSave: () => saveTimeline(),
    onShowShortcuts: () => setShortcutsVisible(true),
  });

  // Shortcuts are automatically registered
  // Can also call handlers programmatically
  return (
    <div>
      <button onClick={handlePlayPause}>Play/Pause</button>
      <button onClick={handleUndo}>Undo</button>
      <button onClick={handleRedo}>Redo</button>
    </div>
  );
}
```

### Implementing Custom Multi-Select UI

```typescript
import { useTimelineStore } from '@/state/timeline';

function ClipComponent({ clip }) {
  const { selectedClipIds, toggleClipSelection } = useTimelineStore();
  const isSelected = selectedClipIds.includes(clip.id);

  const handleClick = (event: React.MouseEvent) => {
    if (event.ctrlKey || event.metaKey) {
      // Additive selection
      toggleClipSelection(clip.id);
    } else if (event.shiftKey) {
      // Range selection (implement your logic)
    } else {
      // Single selection
      useTimelineStore.getState().setSelectedClipIds([clip.id]);
    }
  };

  return (
    <div
      className={isSelected ? 'clip-selected' : 'clip'}
      onClick={handleClick}
      data-testid="timeline-clip"
      data-selected={isSelected}
    >
      {clip.name}
    </div>
  );
}
```

### Enhanced Snap Points

```typescript
import { calculateEnhancedSnapPoints } from '@/services/timelineEngine';

const snapPoints = calculateEnhancedSnapPoints({
  clips: tracks.flatMap((t) =>
    t.clips.map((c) => ({
      id: c.id,
      startTime: c.timelineStart,
      duration: c.sourceOut - c.sourceIn,
    }))
  ),
  playheadTime: currentTime,
  markers: markers.map((m) => ({ time: m.time, label: m.title })),
  captions: [
    { startTime: 5, endTime: 8, text: 'Hello world' },
    { startTime: 10, endTime: 12, text: 'Second caption' },
  ],
  audioPeaks: [
    { time: 2.5, intensity: 0.8 },
    { time: 7.3, intensity: 0.9 },
  ],
  sceneBoundaries: [{ time: 30 }, { time: 60 }],
  enableCaptions: snapConfig.snapToCaptions,
  enableAudioPeaks: snapConfig.snapToAudioPeaks,
});
```

## Testing

### Unit Tests

Run unit tests:

```bash
npm test -- src/test/timeline-multiselect.test.ts
npm test -- src/test/timeline-snapping.test.ts
npm test -- src/commands/__tests__/timelineCommands.test.ts
```

### E2E Tests

Run E2E tests:

```bash
npm run playwright -- tests/e2e/timeline-multiselect.spec.ts
```

## Performance Considerations

### Current State

- Multi-select operations are O(n) where n is number of clips
- Snap point calculation is O(m) where m is total snap points
- Undo stack maintains configurable limit (default 100)

### Future Optimizations

Planned for follow-up work:

1. **Virtualization**: Render only visible clips for large timelines
2. **Throttling**: Debounce drag updates to reduce redraws
3. **requestAnimationFrame**: Smooth animation for playhead movement
4. **Spatial Indexing**: R-tree or quadtree for efficient snap point queries
5. **Web Workers**: Offload snap point calculation to background thread

## Migration Guide

### From Single Selection

If you have existing code using `selectedClipId`:

```typescript
// Old
const { selectedClipId } = useTimelineStore();

// New (backwards compatible)
const { selectedClipId, selectedClipIds } = useTimelineStore();

// selectedClipId still works for single selection
// selectedClipIds is primary for multi-select
```

### From Direct State Updates

If you were directly modifying clips:

```typescript
// Old (not undoable)
useTimelineStore.getState().removeClip('clip1');

// New (undoable)
import { DeleteClipsCommand } from '@/commands/timelineCommands';
const command = new DeleteClipsCommand(['clip1'], useTimelineStore);
await useUndoManager.getState().execute(command);
```

## Troubleshooting

### Keyboard Shortcuts Not Working

1. Check that `useTimelineShortcutsIntegration` is called in your component
2. Ensure `enabled` prop is `true`
3. Verify you're not in an input field (shortcuts are disabled there)
4. Check browser console for errors

### Undo/Redo Not Working

1. Verify commands extend `Command` interface
2. Check `execute()` and `undo()` are implemented
3. Ensure command is executed via `useUndoManager.execute()`
4. Check undo stack in TimelineHistoryPanel

### Snapping Not Working

1. Verify `snappingEnabled` is true in timeline state
2. Check `snapConfig` settings
3. Ensure snap points are being calculated
4. Verify threshold is appropriate (try increasing)

### Performance Issues

1. Check number of clips on timeline
2. Review snap point calculation (may need optimization)
3. Enable performance monitoring in DevTools
4. Consider reducing history limit if memory is constrained

## API Reference

### Timeline Store Actions

```typescript
interface TimelineState {
  // Multi-select
  selectedClipIds: string[];
  setSelectedClipIds: (ids: string[]) => void;
  toggleClipSelection: (id: string) => void;
  selectClipRange: (startId: string, endId: string) => void;
  clearSelection: () => void;

  // Batch operations
  removeClips: (clipIds: string[]) => void;
  rippleDeleteClip: (clipId: string) => void;
  rippleDeleteClips: (clipIds: string[]) => void;

  // Modes
  rippleEditMode: boolean;
  setRippleEditMode: (enabled: boolean) => void;
  magneticTimelineEnabled: boolean;
  setMagneticTimelineEnabled: (enabled: boolean) => void;

  // Snap configuration
  snapConfig: SnapConfiguration;
  setSnapConfig: (config: Partial<SnapConfiguration>) => void;
}
```

### Command Classes

All commands implement:

```typescript
interface Command {
  execute(): void;
  undo(): void;
  getDescription(): string;
  getTimestamp(): Date;
}
```

## Contributing

When adding new timeline features:

1. Create command class if operation should be undoable
2. Add unit tests for commands
3. Add E2E tests for user workflows
4. Update keyboard shortcuts if adding new operations
5. Document in this guide

## Future Enhancements

Planned features for future phases:

- **Advanced Selection**: Lasso selection, select by type/track
- **Clipboard**: Copy/paste with keyboard shortcuts
- **Magnetic Timeline**: Auto-snap when dragging near other clips
- **Track Locking**: Prevent accidental edits to locked tracks
- **Nested Timelines**: Edit groups of clips as single entity
- **Performance**: Virtualization and optimization for 1000+ clips
- **Collaborative**: Real-time collaborative editing with conflict resolution

---

**Version**: Phase 2.0  
**Last Updated**: 2024-11-04  
**Status**: Complete and tested
