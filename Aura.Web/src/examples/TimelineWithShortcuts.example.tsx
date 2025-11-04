/**
 * Example: Timeline component with OpenCut-inspired keyboard shortcuts
 *
 * This example demonstrates how to integrate the keyboard shortcuts system
 * with a timeline editor component. Use this as a reference for implementing
 * shortcuts in your own timeline components.
 *
 * @example
 * ```tsx
 * import { TimelineWithShortcuts } from './examples/TimelineWithShortcuts.example';
 *
 * function App() {
 *   return <TimelineWithShortcuts />;
 * }
 * ```
 */

import { Button, makeStyles } from '@fluentui/react-components';
import React, { useState, useCallback } from 'react';
import { KeyboardShortcutsHelp } from '../components/KeyboardShortcutsHelp';
import { useKeybindings } from '../hooks/useKeybindings';
import { useTimelineStore } from '../state/timeline';
import type { ChapterMarker } from '../state/timeline';

const useStyles = makeStyles({
  container: {
    padding: '20px',
    display: 'flex',
    flexDirection: 'column',
    gap: '16px',
  },
  toolbar: {
    display: 'flex',
    gap: '8px',
    alignItems: 'center',
  },
  timeline: {
    border: '1px solid #ccc',
    borderRadius: '4px',
    padding: '16px',
    minHeight: '200px',
    backgroundColor: '#f5f5f5',
  },
  status: {
    padding: '8px',
    backgroundColor: '#fff',
    borderRadius: '4px',
    fontSize: '14px',
  },
  info: {
    fontSize: '12px',
    color: '#666',
    marginTop: '8px',
  },
});

/**
 * Example timeline component with keyboard shortcuts integration
 */
export const TimelineWithShortcuts: React.FC = () => {
  const styles = useStyles();
  const [showHelp, setShowHelp] = useState(false);
  const [statusMessage, setStatusMessage] = useState('Ready');

  // Timeline store state
  const {
    currentTime,
    setCurrentTime,
    isPlaying,
    setPlaying,
    inPoint,
    outPoint,
    setInPoint,
    setOutPoint,
    markers,
    addMarker,
    snappingEnabled,
    setSnappingEnabled,
    rippleEditMode,
    setRippleEditMode,
    selectedClipIds,
    removeClips,
  } = useTimelineStore();

  // Helper to show temporary status messages
  const showStatus = useCallback((message: string) => {
    setStatusMessage(message);
    setTimeout(() => setStatusMessage('Ready'), 2000);
  }, []);

  // Keyboard shortcut handlers
  useKeybindings({
    // Playback controls
    'toggle-play': () => {
      setPlaying(!isPlaying);
      showStatus(isPlaying ? 'Paused' : 'Playing');
    },

    'seek-backward': () => {
      if (isPlaying) {
        showStatus('Reverse playback not implemented in this example');
      }
    },

    'seek-forward': () => {
      if (isPlaying) {
        showStatus('Forward playback not implemented in this example');
      }
    },

    // Frame navigation
    'frame-step-backward': () => {
      const fps = 30; // Example FPS
      setCurrentTime(Math.max(0, currentTime - 1 / fps));
      showStatus('Previous frame');
    },

    'frame-step-forward': () => {
      const fps = 30;
      setCurrentTime(currentTime + 1 / fps);
      showStatus('Next frame');
    },

    'jump-backward': () => {
      const fps = 30;
      setCurrentTime(Math.max(0, currentTime - 10 / fps));
      showStatus('Jumped back 10 frames');
    },

    'jump-forward': () => {
      const fps = 30;
      setCurrentTime(currentTime + 10 / fps);
      showStatus('Jumped forward 10 frames');
    },

    'goto-start': () => {
      setCurrentTime(0);
      showStatus('Go to start');
    },

    'goto-end': () => {
      setCurrentTime(100); // Example max duration
      showStatus('Go to end');
    },

    // Editing operations
    'split-element': () => {
      if (selectedClipIds.length > 0) {
        showStatus(`Split ${selectedClipIds.length} clip(s) at ${currentTime.toFixed(2)}s`);
      } else {
        showStatus('No clips selected to split');
      }
    },

    'delete-selected': () => {
      if (selectedClipIds.length > 0) {
        removeClips(selectedClipIds);
        showStatus(`Deleted ${selectedClipIds.length} clip(s)`);
      } else {
        showStatus('No clips selected to delete');
      }
    },

    'duplicate-selected': () => {
      if (selectedClipIds.length > 0) {
        showStatus(`Duplicate not implemented in this example`);
      } else {
        showStatus('No clips selected to duplicate');
      }
    },

    // Toggle features
    'toggle-snapping': () => {
      setSnappingEnabled(!snappingEnabled);
      showStatus(`Snapping ${!snappingEnabled ? 'enabled' : 'disabled'}`);
    },

    'toggle-ripple-edit': () => {
      setRippleEditMode(!rippleEditMode);
      showStatus(`Ripple editing ${!rippleEditMode ? 'enabled' : 'disabled'}`);
    },

    // In/Out points
    'set-in-point': () => {
      setInPoint(currentTime);
      showStatus(`In point set at ${currentTime.toFixed(2)}s`);
    },

    'set-out-point': () => {
      setOutPoint(currentTime);
      showStatus(`Out point set at ${currentTime.toFixed(2)}s`);
    },

    'clear-in-point': () => {
      setInPoint(undefined);
      showStatus('In point cleared');
    },

    'clear-out-point': () => {
      setOutPoint(undefined);
      showStatus('Out point cleared');
    },

    // Markers
    'add-marker': () => {
      const marker: ChapterMarker = {
        id: `marker-${Date.now()}`,
        title: `Marker ${markers.length + 1}`,
        time: currentTime,
      };
      addMarker(marker);
      showStatus(`Marker added at ${currentTime.toFixed(2)}s`);
    },

    'next-marker': () => {
      const nextMarker = markers.find((m) => m.time > currentTime);
      if (nextMarker) {
        setCurrentTime(nextMarker.time);
        showStatus(`Jumped to marker: ${nextMarker.title}`);
      } else {
        showStatus('No next marker');
      }
    },

    'prev-marker': () => {
      const prevMarker = [...markers].reverse().find((m) => m.time < currentTime);
      if (prevMarker) {
        setCurrentTime(prevMarker.time);
        showStatus(`Jumped to marker: ${prevMarker.title}`);
      } else {
        showStatus('No previous marker');
      }
    },

    // Selection (examples only)
    'select-all': () => {
      showStatus('Select all not implemented in this example');
    },

    'deselect-all': () => {
      showStatus('Deselect all not implemented in this example');
    },

    // Clipboard (examples only)
    'copy-selected': () => {
      showStatus('Copy not implemented in this example');
    },

    'paste-selected': () => {
      showStatus('Paste not implemented in this example');
    },

    'cut-selected': () => {
      showStatus('Cut not implemented in this example');
    },

    // Undo/Redo (examples only)
    undo: () => {
      showStatus('Undo not implemented in this example');
    },

    redo: () => {
      showStatus('Redo not implemented in this example');
    },

    // Zoom (examples only)
    'zoom-in': () => {
      showStatus('Zoom in not implemented in this example');
    },

    'zoom-out': () => {
      showStatus('Zoom out not implemented in this example');
    },

    'zoom-to-fit': () => {
      showStatus('Zoom to fit not implemented in this example');
    },

    // Element properties (examples only)
    'toggle-hide-selected': () => {
      showStatus('Toggle hide not implemented in this example');
    },
  });

  return (
    <div className={styles.container}>
      <h2>Timeline with Keyboard Shortcuts Example</h2>

      <div className={styles.toolbar}>
        <Button appearance="primary" onClick={() => setPlaying(!isPlaying)}>
          {isPlaying ? 'Pause' : 'Play'} (Space)
        </Button>
        <Button onClick={() => setShowHelp(true)}>Show Shortcuts (?)</Button>
        <Button onClick={() => setSnappingEnabled(!snappingEnabled)}>
          Snapping: {snappingEnabled ? 'On' : 'Off'} (N)
        </Button>
        <Button onClick={() => setRippleEditMode(!rippleEditMode)}>
          Ripple: {rippleEditMode ? 'On' : 'Off'} (R)
        </Button>
      </div>

      <div className={styles.status}>
        <strong>Status:</strong> {statusMessage}
      </div>

      <div className={styles.timeline}>
        <p>
          <strong>Current Time:</strong> {currentTime.toFixed(2)}s
        </p>
        <p>
          <strong>In Point:</strong> {inPoint !== undefined ? `${inPoint.toFixed(2)}s` : 'Not set'}
        </p>
        <p>
          <strong>Out Point:</strong>{' '}
          {outPoint !== undefined ? `${outPoint.toFixed(2)}s` : 'Not set'}
        </p>
        <p>
          <strong>Markers:</strong> {markers.length}
        </p>
        <p>
          <strong>Selected Clips:</strong> {selectedClipIds.length}
        </p>
      </div>

      <div className={styles.info}>
        <p>
          <strong>Try these shortcuts:</strong>
        </p>
        <ul>
          <li>
            <kbd>Space</kbd> - Play/Pause
          </li>
          <li>
            <kbd>←</kbd>/<kbd>→</kbd> - Navigate frames
          </li>
          <li>
            <kbd>I</kbd>/<kbd>O</kbd> - Set in/out points
          </li>
          <li>
            <kbd>M</kbd> - Add marker
          </li>
          <li>
            <kbd>N</kbd> - Toggle snapping
          </li>
          <li>
            <kbd>R</kbd> - Toggle ripple edit
          </li>
          <li>
            <kbd>?</kbd> - Show all shortcuts
          </li>
        </ul>
      </div>

      <KeyboardShortcutsHelp open={showHelp} onClose={() => setShowHelp(false)} />
    </div>
  );
};

export default TimelineWithShortcuts;
