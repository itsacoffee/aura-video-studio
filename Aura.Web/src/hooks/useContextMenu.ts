/**
 * useContextMenu - Custom hook for context menu interactions
 *
 * Provides a React-friendly interface for triggering context menus through
 * the Electron preload API and listening for context menu action callbacks.
 */

import { useCallback, useEffect } from 'react';
import type { ContextMenuType, ContextMenuActionData } from '../types/electron-context-menu';

/**
 * Hook to show a context menu of a specific type
 *
 * @param type - The type of context menu to show
 * @returns A function to call with the mouse event and context data
 */
export function useContextMenu<T = unknown>(type: ContextMenuType) {
  return useCallback(
    async (event: React.MouseEvent, data: T) => {
      event.preventDefault();
      event.stopPropagation();

      if (!window.electron?.contextMenu) {
        console.warn('Context menu API not available');
        return;
      }

      try {
        const result = await window.electron.contextMenu.show(type, {
          ...(data as Record<string, unknown>),
          position: { x: event.clientX, y: event.clientY },
        });

        if (!result.success) {
          console.error('Failed to show context menu:', result.error);
        }
      } catch (error: unknown) {
        const errorMessage = error instanceof Error ? error.message : String(error);
        console.error('Error showing context menu:', errorMessage);
      }
    },
    [type]
  );
}

/**
 * Hook to listen for context menu action callbacks
 *
 * @param type - The context menu type to listen for
 * @param actionType - The specific action type (e.g., 'onCut', 'onCopy')
 * @param callback - Function to call when the action is triggered
 */
export function useContextMenuAction<T = ContextMenuActionData>(
  type: ContextMenuType,
  actionType: string,
  callback: (data: T) => void
) {
  useEffect(() => {
    if (!window.electron?.contextMenu) {
      return;
    }

    const unsubscribe = window.electron.contextMenu.onAction(type, actionType, callback);

    return () => {
      unsubscribe();
    };
  }, [type, actionType, callback]);
}
