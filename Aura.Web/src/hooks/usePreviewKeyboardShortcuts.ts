/**
 * usePreviewKeyboardShortcuts - Custom hook for preview window keyboard shortcuts
 *
 * Provides keyboard shortcut handling for video preview playback controls,
 * marker placement, and navigation.
 */

import { useEffect } from 'react';

export interface PreviewKeyboardShortcutHandlers {
  onTogglePlayback?: () => void;
  onAddMarker?: () => void;
  onSeekForward?: () => void;
  onSeekBackward?: () => void;
  onFrameForward?: () => void;
  onFrameBackward?: () => void;
}

/**
 * Hook to register keyboard shortcuts for preview window controls
 *
 * @param handlers - Object containing callback functions for each keyboard shortcut
 * @param enabled - Whether the shortcuts are currently active (default: true)
 */
export function usePreviewKeyboardShortcuts(
  handlers: PreviewKeyboardShortcutHandlers,
  enabled = true
): void {
  useEffect(() => {
    if (!enabled) return;

    const handleKeyDown = (e: KeyboardEvent) => {
      // Don't trigger shortcuts when typing in input fields
      const target = e.target as HTMLElement;
      if (target.tagName === 'INPUT' || target.tagName === 'TEXTAREA' || target.isContentEditable) {
        return;
      }

      // Spacebar - Toggle playback
      if (e.code === 'Space' || e.key === ' ') {
        e.preventDefault();
        handlers.onTogglePlayback?.();
        return;
      }

      // M key - Add marker at current position
      if (e.key === 'm' || e.key === 'M') {
        e.preventDefault();
        handlers.onAddMarker?.();
        return;
      }

      // Arrow Right - Seek forward (with Shift: larger jump)
      if (e.key === 'ArrowRight') {
        e.preventDefault();
        if (e.shiftKey) {
          handlers.onSeekForward?.();
        } else {
          handlers.onFrameForward?.();
        }
        return;
      }

      // Arrow Left - Seek backward (with Shift: larger jump)
      if (e.key === 'ArrowLeft') {
        e.preventDefault();
        if (e.shiftKey) {
          handlers.onSeekBackward?.();
        } else {
          handlers.onFrameBackward?.();
        }
        return;
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [handlers, enabled]);
}

/**
 * Get preview keyboard shortcuts reference for help display
 */
export function getPreviewKeyboardShortcuts(): Array<{ keys: string; description: string }> {
  return [
    { keys: 'Space', description: 'Play/Pause' },
    { keys: 'M', description: 'Add marker at current time' },
    { keys: 'Left/Right Arrow', description: 'Step one frame backward/forward' },
    { keys: 'Shift + Left/Right', description: 'Seek 5 seconds backward/forward' },
  ];
}
