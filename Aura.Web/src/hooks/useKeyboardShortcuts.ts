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
 * @param shortcuts - Array of shortcut handlers to register (should be memoized or stable)
 * @param enabled - Whether shortcuts are enabled (default: true)
 *
 * Note: The shortcuts array should be stable (e.g., defined outside component or memoized with useMemo)
 * to avoid unnecessary re-registration. If shortcuts need to change dynamically, memoize them first.
 */
export function useKeyboardShortcuts(shortcuts: ShortcutHandler[], enabled = true): void {
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
  }, [shortcuts, enabled]);
}
