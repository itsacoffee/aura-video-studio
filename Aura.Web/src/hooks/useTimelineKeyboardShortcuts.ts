/**
 * Custom hook for timeline keyboard shortcuts
 */

import { useEffect, useCallback } from 'react';

export interface TimelineShortcutHandlers {
  onPlayPause?: () => void;
  onRewind?: () => void;
  onFastForward?: () => void;
  onFrameBackward?: () => void;
  onFrameForward?: () => void;
  onSecondBackward?: () => void;
  onSecondForward?: () => void;
  onJumpToStart?: () => void;
  onJumpToEnd?: () => void;
  onSetInPoint?: () => void;
  onSetOutPoint?: () => void;
  onMarkInToOut?: () => void;
  onSplice?: () => void;
  onRippleDelete?: () => void;
  onDelete?: () => void;
  onSelectAll?: () => void;
  onDeselectAll?: () => void;
  onCopy?: () => void;
  onPaste?: () => void;
  onDuplicate?: () => void;
  onUndo?: () => void;
  onRedo?: () => void;
  onZoomIn?: () => void;
  onZoomOut?: () => void;
  onAddMarker?: () => void;
  onSave?: () => void;
  onShowShortcuts?: () => void;
}

export function useTimelineKeyboardShortcuts(
  handlers: TimelineShortcutHandlers,
  enabled = true
): void {
  const handleKeyDown = useCallback(
    // eslint-disable-next-line sonarjs/cognitive-complexity -- Comprehensive keyboard shortcut handler with many conditional branches for different key combinations
    (event: KeyboardEvent) => {
      if (!enabled) return;

      // Don't trigger shortcuts when typing in input fields
      const target = event.target as HTMLElement;
      if (target.tagName === 'INPUT' || target.tagName === 'TEXTAREA' || target.isContentEditable) {
        return;
      }

      const isMac = navigator.platform.toUpperCase().indexOf('MAC') >= 0;
      const ctrlOrCmd = isMac ? event.metaKey : event.ctrlKey;

      // Spacebar - Play/Pause
      if (event.code === 'Space') {
        event.preventDefault();
        handlers.onPlayPause?.();
        return;
      }

      // J/K/L - Rewind/Pause/Fast-forward
      if (event.key === 'j' && !ctrlOrCmd) {
        event.preventDefault();
        handlers.onRewind?.();
        return;
      }
      if (event.key === 'k' && !ctrlOrCmd) {
        event.preventDefault();
        handlers.onPlayPause?.();
        return;
      }
      if (event.key === 'l' && !ctrlOrCmd) {
        event.preventDefault();
        handlers.onFastForward?.();
        return;
      }

      // Arrow keys - Frame navigation
      if (event.key === 'ArrowLeft' && !event.shiftKey && !ctrlOrCmd) {
        event.preventDefault();
        handlers.onFrameBackward?.();
        return;
      }
      if (event.key === 'ArrowRight' && !event.shiftKey && !ctrlOrCmd) {
        event.preventDefault();
        handlers.onFrameForward?.();
        return;
      }

      // Shift + Arrow keys - Second navigation
      if (event.key === 'ArrowLeft' && event.shiftKey && !ctrlOrCmd) {
        event.preventDefault();
        handlers.onSecondBackward?.();
        return;
      }
      if (event.key === 'ArrowRight' && event.shiftKey && !ctrlOrCmd) {
        event.preventDefault();
        handlers.onSecondForward?.();
        return;
      }

      // Home/End - Jump to start/end
      if (event.key === 'Home') {
        event.preventDefault();
        handlers.onJumpToStart?.();
        return;
      }
      if (event.key === 'End') {
        event.preventDefault();
        handlers.onJumpToEnd?.();
        return;
      }

      // I/O - Set in/out points
      if (event.key === 'i' && !ctrlOrCmd) {
        event.preventDefault();
        handlers.onSetInPoint?.();
        return;
      }
      if (event.key === 'o' && !ctrlOrCmd) {
        event.preventDefault();
        handlers.onSetOutPoint?.();
        return;
      }

      // X - Mark in to out
      if (event.key === 'x' && !ctrlOrCmd) {
        event.preventDefault();
        handlers.onMarkInToOut?.();
        return;
      }

      // C - Splice/Cut
      if (event.key === 'c' && !ctrlOrCmd) {
        event.preventDefault();
        handlers.onSplice?.();
        return;
      }

      // Delete - Ripple delete
      if (event.key === 'Delete' && !ctrlOrCmd) {
        event.preventDefault();
        handlers.onRippleDelete?.();
        return;
      }

      // Backspace - Delete (leave gap)
      if (event.key === 'Backspace' && !ctrlOrCmd) {
        event.preventDefault();
        handlers.onDelete?.();
        return;
      }

      // A - Select all
      if (event.key === 'a' && ctrlOrCmd) {
        event.preventDefault();
        handlers.onSelectAll?.();
        return;
      }

      // D - Deselect all
      if (event.key === 'd' && ctrlOrCmd) {
        event.preventDefault();
        handlers.onDeselectAll?.();
        return;
      }

      // Ctrl/Cmd + C - Copy
      if (event.key === 'c' && ctrlOrCmd) {
        event.preventDefault();
        handlers.onCopy?.();
        return;
      }

      // Ctrl/Cmd + V - Paste
      if (event.key === 'v' && ctrlOrCmd) {
        event.preventDefault();
        handlers.onPaste?.();
        return;
      }

      // Ctrl/Cmd + D - Duplicate
      if (event.key === 'd' && ctrlOrCmd && !event.shiftKey) {
        event.preventDefault();
        handlers.onDuplicate?.();
        return;
      }

      // Ctrl/Cmd + Z - Undo
      if (event.key === 'z' && ctrlOrCmd && !event.shiftKey) {
        event.preventDefault();
        handlers.onUndo?.();
        return;
      }

      // Ctrl/Cmd + Shift + Z - Redo
      if (event.key === 'z' && ctrlOrCmd && event.shiftKey) {
        event.preventDefault();
        handlers.onRedo?.();
        return;
      }

      // +/- Zoom
      if ((event.key === '=' || event.key === '+') && !ctrlOrCmd) {
        event.preventDefault();
        handlers.onZoomIn?.();
        return;
      }
      if (event.key === '-' && !ctrlOrCmd) {
        event.preventDefault();
        handlers.onZoomOut?.();
        return;
      }

      // M - Add marker
      if (event.key === 'm' && !ctrlOrCmd) {
        event.preventDefault();
        handlers.onAddMarker?.();
        return;
      }

      // Ctrl/Cmd + S - Save
      if (event.key === 's' && ctrlOrCmd) {
        event.preventDefault();
        handlers.onSave?.();
        return;
      }

      // ? - Show shortcuts
      if (event.key === '?' && !ctrlOrCmd) {
        event.preventDefault();
        handlers.onShowShortcuts?.();
        return;
      }
    },
    [handlers, enabled]
  );

  useEffect(() => {
    if (!enabled) return;

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [handleKeyDown, enabled]);
}

/**
 * Get keyboard shortcuts reference
 */
export function getKeyboardShortcuts(): Array<{ keys: string; description: string }> {
  const isMac = navigator.platform.toUpperCase().indexOf('MAC') >= 0;
  const ctrlOrCmd = isMac ? 'Cmd' : 'Ctrl';

  return [
    { keys: 'Space', description: 'Play/Pause' },
    { keys: 'J/K/L', description: 'Rewind/Pause/Fast-forward' },
    { keys: 'Left/Right Arrow', description: 'Move playhead 1 frame' },
    { keys: 'Shift + Left/Right', description: 'Move playhead 1 second' },
    { keys: 'Home/End', description: 'Jump to start/end' },
    { keys: 'I/O', description: 'Set in/out points' },
    { keys: 'X', description: 'Mark in to out' },
    { keys: 'C', description: 'Cut/Splice at playhead' },
    { keys: 'Delete', description: 'Ripple delete selected' },
    { keys: 'Backspace', description: 'Delete selected (leave gap)' },
    { keys: `${ctrlOrCmd} + A`, description: 'Select all' },
    { keys: `${ctrlOrCmd} + D`, description: 'Deselect all / Duplicate' },
    { keys: `${ctrlOrCmd} + C`, description: 'Copy selected' },
    { keys: `${ctrlOrCmd} + V`, description: 'Paste at playhead' },
    { keys: `${ctrlOrCmd} + Z`, description: 'Undo' },
    { keys: `${ctrlOrCmd} + Shift + Z`, description: 'Redo' },
    { keys: '+/-', description: 'Zoom in/out timeline' },
    { keys: 'M', description: 'Add marker' },
    { keys: `${ctrlOrCmd} + S`, description: 'Save timeline' },
    { keys: '?', description: 'Show keyboard shortcuts' },
  ];
}
