/**
 * Global keyboard shortcuts for undo/redo functionality
 * Registers Ctrl/Cmd+Z for undo and Ctrl/Cmd+Y or Ctrl/Cmd+Shift+Z for redo
 */

import { useMemo } from 'react';
import { ShortcutHandler } from '../services/keyboardShortcutManager';
import { useUndoManager } from '../state/undoManager';
import { useKeyboardShortcuts } from './useKeyboardShortcuts';

/**
 * Hook to register global undo/redo keyboard shortcuts
 */
export function useGlobalUndoShortcuts(): void {
  const { undo, redo, canUndo, canRedo } = useUndoManager();

  const shortcuts: ShortcutHandler[] = useMemo(
    () => [
      {
        id: 'global-undo',
        keys: 'Ctrl+Z',
        description: 'Undo last action',
        context: 'global',
        enabled: canUndo,
        handler: () => {
          undo();
        },
        preventDefault: true,
        stopPropagation: true,
      },
      {
        id: 'global-redo-y',
        keys: 'Ctrl+Y',
        description: 'Redo last undone action',
        context: 'global',
        enabled: canRedo,
        handler: () => {
          redo();
        },
        preventDefault: true,
        stopPropagation: true,
      },
      {
        id: 'global-redo-shift-z',
        keys: 'Ctrl+Shift+Z',
        description: 'Redo last undone action (alternative)',
        context: 'global',
        enabled: canRedo,
        handler: () => {
          redo();
        },
        preventDefault: true,
        stopPropagation: true,
      },
      {
        id: 'global-history',
        keys: 'Ctrl+Shift+U',
        description: 'Show action history',
        context: 'global',
        handler: () => {
          useUndoManager.getState().toggleHistory();
        },
        preventDefault: true,
        stopPropagation: true,
      },
    ],
    [undo, redo, canUndo, canRedo]
  );

  useKeyboardShortcuts(shortcuts);
}
