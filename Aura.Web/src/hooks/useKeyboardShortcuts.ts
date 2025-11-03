/**
 * Custom hook for registering keyboard shortcuts
 *
 * Provides a simple interface to register and unregister shortcuts
 * within React components, using the centralized keyboardShortcutManager.
 */

import { useEffect } from 'react';
import { keyboardShortcutManager, ShortcutHandler } from '../services/keyboardShortcutManager';

/**
 * Hook to register keyboard shortcuts for a component
 *
 * @param shortcuts - Array of shortcut handlers to register
 * @param enabled - Whether shortcuts are enabled (default: true)
 * @param dependencies - Additional dependencies to trigger re-registration
 */
export function useKeyboardShortcuts(
  shortcuts: ShortcutHandler[],
  enabled = true,
  dependencies: unknown[] = []
): void {
  useEffect(() => {
    if (!enabled) return;

    // Register all shortcuts
    keyboardShortcutManager.registerMultiple(shortcuts);

    // Cleanup: unregister shortcuts when component unmounts or dependencies change
    return () => {
      shortcuts.forEach((shortcut) => {
        keyboardShortcutManager.unregister(shortcut.id, shortcut.context);
      });
    };
    // Dependencies include shortcuts array and any additional deps
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [enabled, ...dependencies]);
}
