/**
 * OpenCut Keyboard Handler Hook
 *
 * Central keyboard event handler for OpenCut that:
 * - Listens for global keyboard events
 * - Matches against keybindings store
 * - Dispatches actions to appropriate stores
 * - Handles JKL speed ramping for professional playback control
 */

import { useEffect, useCallback, useRef, useState } from 'react';
import { useOpenCutKeybindingsStore, type KeyModifiers } from '../stores/opencutKeybindings';
import { useOpenCutMarkersStore } from '../stores/opencutMarkers';
import { useOpenCutPlaybackStore } from '../stores/opencutPlayback';
import { useOpenCutTimelineStore } from '../stores/opencutTimeline';

export interface UseOpenCutKeyboardHandlerOptions {
  /** Whether the keyboard handler is active */
  enabled?: boolean;
  /** Callback for unhandled actions */
  onUnhandledAction?: (action: string) => void;
}

/**
 * In/Out point state for mark in/out functionality
 */
export interface InOutPoints {
  inPoint: number | null;
  outPoint: number | null;
}

/**
 * Extract modifier state from keyboard event
 */
function getModifiersFromEvent(event: KeyboardEvent): KeyModifiers {
  return {
    ctrl: event.ctrlKey,
    shift: event.shiftKey,
    alt: event.altKey,
    meta: event.metaKey,
  };
}

/**
 * Check if focus is on an input element where we should not intercept keys
 */
function isInputFocused(): boolean {
  const activeElement = document.activeElement;
  if (!activeElement) return false;

  const tagName = activeElement.tagName.toLowerCase();
  if (tagName === 'input' || tagName === 'textarea' || tagName === 'select') {
    return true;
  }

  // Check for contenteditable
  if (activeElement.hasAttribute('contenteditable')) {
    return true;
  }

  return false;
}

/**
 * Hook for handling keyboard shortcuts in the OpenCut editor.
 * Provides professional NLE-style keyboard control including JKL playback.
 *
 * @returns InOutPoints for mark in/out functionality
 */
export function useOpenCutKeyboardHandler(
  options: UseOpenCutKeyboardHandlerOptions = {}
): InOutPoints {
  const { enabled = true, onUnhandledAction } = options;

  const keybindingsStore = useOpenCutKeybindingsStore();
  const playbackStore = useOpenCutPlaybackStore();
  const timelineStore = useOpenCutTimelineStore();
  const markersStore = useOpenCutMarkersStore();

  // In/Out points state
  const [inPoint, setInPoint] = useState<number | null>(null);
  const [outPoint, setOutPoint] = useState<number | null>(null);

  // Track last J/L press time for speed ramping
  const lastJPressRef = useRef<number>(0);
  const lastLPressRef = useRef<number>(0);
  const jklTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // Speed ramp threshold in milliseconds
  const SPEED_RAMP_THRESHOLD = 500;

  /**
   * Handle JKL playback control with speed ramping
   */
  const handleJKLControl = useCallback(
    (key: 'j' | 'k' | 'l') => {
      const now = Date.now();
      const { jklDirection, setJKLSpeed, setJKLDirection, incrementJKLSpeed } = keybindingsStore;

      // Clear any pending reset timeout
      if (jklTimeoutRef.current) {
        clearTimeout(jklTimeoutRef.current);
        jklTimeoutRef.current = null;
      }

      switch (key) {
        case 'j': // Reverse playback
          if (jklDirection === -1 && now - lastJPressRef.current < SPEED_RAMP_THRESHOLD) {
            // Speed ramp: double the speed
            incrementJKLSpeed();
          } else {
            // Start reverse or reset speed
            setJKLSpeed(1);
          }
          setJKLDirection(-1);
          lastJPressRef.current = now;

          // Apply to playback
          playbackStore.setSpeed(keybindingsStore.jklSpeed);
          playbackStore.play();
          // Reverse playback would need custom implementation
          break;

        case 'k': // Pause
          setJKLDirection(0);
          setJKLSpeed(1);
          playbackStore.pause();
          break;

        case 'l': // Forward playback
          if (jklDirection === 1 && now - lastLPressRef.current < SPEED_RAMP_THRESHOLD) {
            // Speed ramp: double the speed
            incrementJKLSpeed();
          } else {
            // Start forward or reset speed
            setJKLSpeed(1);
          }
          setJKLDirection(1);
          lastLPressRef.current = now;

          // Apply to playback
          playbackStore.setSpeed(keybindingsStore.jklSpeed);
          playbackStore.play();
          break;
      }

      // Reset JKL state after inactivity
      jklTimeoutRef.current = setTimeout(() => {
        keybindingsStore.resetJKL();
      }, 2000);
    },
    [keybindingsStore, playbackStore]
  );

  /**
   * Execute action based on keybinding
   */
  const executeAction = useCallback(
    (action: string): boolean => {
      switch (action) {
        // JKL Playback Controls
        case 'playReverse':
          handleJKLControl('j');
          return true;
        case 'pause':
          handleJKLControl('k');
          return true;
        case 'playForward':
          handleJKLControl('l');
          return true;
        case 'togglePlayPause':
          playbackStore.toggle();
          return true;
        case 'stop':
          playbackStore.pause();
          playbackStore.seek(0);
          return true;

        // Navigation
        case 'goToStart':
          playbackStore.goToStart();
          return true;
        case 'goToEnd':
          playbackStore.goToEnd();
          return true;
        case 'prevFrame':
          playbackStore.seek(playbackStore.currentTime - 1 / 30); // 30fps
          return true;
        case 'nextFrame':
          playbackStore.seek(playbackStore.currentTime + 1 / 30);
          return true;
        case 'nudgeLeft':
          playbackStore.seek(playbackStore.currentTime - 1 / 30);
          return true;
        case 'nudgeRight':
          playbackStore.seek(playbackStore.currentTime + 1 / 30);
          return true;
        case 'prevEdit':
          // Navigate to previous edit point (clip boundary)
          {
            const clips = timelineStore.clips;
            const currentTime = playbackStore.currentTime;
            const editPoints = clips
              .flatMap((c) => [c.startTime, c.startTime + c.duration])
              .filter((t) => t < currentTime - 0.01)
              .sort((a, b) => b - a);
            if (editPoints.length > 0) {
              playbackStore.seek(editPoints[0]);
            }
          }
          return true;
        case 'nextEdit':
          // Navigate to next edit point (clip boundary)
          {
            const clips = timelineStore.clips;
            const currentTime = playbackStore.currentTime;
            const editPoints = clips
              .flatMap((c) => [c.startTime, c.startTime + c.duration])
              .filter((t) => t > currentTime + 0.01)
              .sort((a, b) => a - b);
            if (editPoints.length > 0) {
              playbackStore.seek(editPoints[0]);
            }
          }
          return true;

        // Marking (in/out points stored locally in this hook)
        case 'markIn':
          setInPoint(playbackStore.currentTime);
          return true;
        case 'markOut':
          setOutPoint(playbackStore.currentTime);
          return true;
        case 'clearIn':
          setInPoint(null);
          return true;
        case 'clearOut':
          setOutPoint(null);
          return true;
        case 'goToIn':
          if (inPoint !== null) {
            playbackStore.seek(inPoint);
          }
          return true;
        case 'goToOut':
          if (outPoint !== null) {
            playbackStore.seek(outPoint);
          }
          return true;

        // Editing
        case 'splitAtPlayhead':
          {
            const selectedId = timelineStore.selectedClipIds[0];
            if (selectedId) {
              timelineStore.splitClip(selectedId, playbackStore.currentTime);
            }
          }
          return true;
        case 'delete':
          timelineStore.selectedClipIds.forEach((id) => {
            timelineStore.removeClip(id);
          });
          return true;
        case 'rippleDelete':
          // Ripple delete - delete and close gap
          timelineStore.selectedClipIds.forEach((id) => {
            timelineStore.removeClip(id);
          });
          // Gap closing would need additional implementation
          return true;
        case 'duplicate':
          timelineStore.selectedClipIds.forEach((id) => {
            timelineStore.duplicateClip(id);
          });
          return true;

        // Selection
        case 'selectAll':
          // Select all clips by getting all clip IDs
          timelineStore.selectClips(timelineStore.clips.map((c) => c.id));
          return true;
        case 'deselectAll':
          timelineStore.clearSelection();
          return true;
        case 'selectAtPlayhead':
          {
            const currentTime = playbackStore.currentTime;
            const clipsAtTime = timelineStore.clips.filter(
              (c) => c.startTime <= currentTime && c.startTime + c.duration > currentTime
            );
            if (clipsAtTime.length > 0) {
              timelineStore.selectClip(clipsAtTime[0].id, false);
            }
          }
          return true;

        // Markers
        case 'addMarker':
          markersStore.addMarker(playbackStore.currentTime, {
            name: 'Marker',
            color: 'orange',
          });
          return true;
        case 'prevMarker':
          {
            const markers = markersStore.markers;
            const currentTime = playbackStore.currentTime;
            const prevMarkers = markers
              .filter((m) => m.time < currentTime - 0.01)
              .sort((a, b) => b.time - a.time);
            if (prevMarkers.length > 0) {
              playbackStore.seek(prevMarkers[0].time);
            }
          }
          return true;
        case 'nextMarker':
          {
            const markers = markersStore.markers;
            const currentTime = playbackStore.currentTime;
            const nextMarkers = markers
              .filter((m) => m.time > currentTime + 0.01)
              .sort((a, b) => a.time - b.time);
            if (nextMarkers.length > 0) {
              playbackStore.seek(nextMarkers[0].time);
            }
          }
          return true;

        // View
        case 'zoomIn':
          timelineStore.setZoom(Math.min(timelineStore.zoom * 1.25, 10));
          return true;
        case 'zoomOut':
          timelineStore.setZoom(Math.max(timelineStore.zoom / 1.25, 0.1));
          return true;
        case 'fitTimeline':
          // Fit timeline to view would need viewport width
          timelineStore.setZoom(1);
          return true;

        default:
          // Unhandled action
          onUnhandledAction?.(action);
          return false;
      }
    },
    [
      handleJKLControl,
      playbackStore,
      timelineStore,
      markersStore,
      inPoint,
      outPoint,
      onUnhandledAction,
    ]
  );

  /**
   * Main keyboard event handler
   */
  const handleKeyDown = useCallback(
    (event: KeyboardEvent) => {
      // Don't intercept if disabled
      if (!enabled) return;

      // Don't intercept if typing in an input
      if (isInputFocused()) return;

      // Get key and modifiers
      const key = event.key;
      const modifiers = getModifiersFromEvent(event);

      // Find matching keybinding
      const keybinding = keybindingsStore.findKeybindingByKey(key, modifiers);

      if (keybinding) {
        // Prevent default browser behavior
        event.preventDefault();
        event.stopPropagation();

        // Execute the action
        executeAction(keybinding.action);
      }
    },
    [enabled, keybindingsStore, executeAction]
  );

  // Set up event listener
  useEffect(() => {
    if (!enabled) return;

    document.addEventListener('keydown', handleKeyDown);

    return () => {
      document.removeEventListener('keydown', handleKeyDown);
      // Clear any pending JKL timeout
      if (jklTimeoutRef.current) {
        clearTimeout(jklTimeoutRef.current);
      }
    };
  }, [enabled, handleKeyDown]);

  // Return in/out point state
  return { inPoint, outPoint };
}

export default useOpenCutKeyboardHandler;
