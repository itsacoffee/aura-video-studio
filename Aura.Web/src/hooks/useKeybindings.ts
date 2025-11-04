/**
 * Custom hook for handling keyboard shortcuts in Aura Video Studio
 * Provides a simple interface for registering keyboard shortcut handlers
 */

import { useEffect, useCallback } from 'react';
import { useKeybindingsStore } from '../state/keybindings';
import type { TimelineAction } from '../types/keybinding';

interface UseKeybindingsOptions {
  /**
   * Whether keyboard shortcuts are enabled
   * @default true
   */
  enabled?: boolean;

  /**
   * Whether to prevent default browser behavior for matched shortcuts
   * @default true
   */
  preventDefault?: boolean;

  /**
   * Whether to stop event propagation for matched shortcuts
   * @default false
   */
  stopPropagation?: boolean;
}

type ActionHandler = (event: KeyboardEvent) => void;
type ActionHandlers = Partial<Record<TimelineAction, ActionHandler>>;

/**
 * Hook for handling keyboard shortcuts based on the keybindings store
 *
 * @param handlers - Object mapping timeline actions to handler functions
 * @param options - Configuration options
 *
 * @example
 * ```tsx
 * useKeybindings({
 *   'toggle-play': () => setIsPlaying(!isPlaying),
 *   'split-element': () => splitAtPlayhead(),
 *   'delete-selected': () => deleteSelectedElements(),
 * });
 * ```
 */
export function useKeybindings(
  handlers: ActionHandlers,
  options: UseKeybindingsOptions = {}
): void {
  const { enabled = true, preventDefault = true, stopPropagation = false } = options;

  const { keybindingsEnabled, getKeybindingString, getActionForKey } = useKeybindingsStore();

  const handleKeyDown = useCallback(
    (event: KeyboardEvent) => {
      // Skip if keybindings are disabled globally or locally
      if (!enabled || !keybindingsEnabled) {
        return;
      }

      // Get the shortcut key string from the event
      const shortcutKey = getKeybindingString(event);
      if (!shortcutKey) {
        return;
      }

      // Get the action for this shortcut key
      const action = getActionForKey(shortcutKey);
      if (!action) {
        return;
      }

      // Check if we have a handler for this action
      const handler = handlers[action];
      if (!handler) {
        return;
      }

      // Prevent default browser behavior if requested
      if (preventDefault) {
        event.preventDefault();
      }

      // Stop event propagation if requested
      if (stopPropagation) {
        event.stopPropagation();
      }

      // Execute the handler
      handler(event);
    },
    [
      enabled,
      keybindingsEnabled,
      getKeybindingString,
      getActionForKey,
      handlers,
      preventDefault,
      stopPropagation,
    ]
  );

  useEffect(() => {
    if (!enabled || !keybindingsEnabled) {
      return;
    }

    // Add event listener to the document
    document.addEventListener('keydown', handleKeyDown);

    // Cleanup
    return () => {
      document.removeEventListener('keydown', handleKeyDown);
    };
  }, [enabled, keybindingsEnabled, handleKeyDown]);
}

/**
 * Hook for handling keyboard shortcuts scoped to a specific element
 *
 * @param ref - React ref to the element to attach the event listener to
 * @param handlers - Object mapping timeline actions to handler functions
 * @param options - Configuration options
 *
 * @example
 * ```tsx
 * const timelineRef = useRef<HTMLDivElement>(null);
 *
 * useScopedKeybindings(timelineRef, {
 *   'split-element': () => splitAtPlayhead(),
 *   'delete-selected': () => deleteSelectedElements(),
 * });
 * ```
 */
export function useScopedKeybindings(
  ref: React.RefObject<HTMLElement>,
  handlers: ActionHandlers,
  options: UseKeybindingsOptions = {}
): void {
  const { enabled = true, preventDefault = true, stopPropagation = false } = options;

  const { keybindingsEnabled, getKeybindingString, getActionForKey } = useKeybindingsStore();

  const handleKeyDown = useCallback(
    (event: KeyboardEvent) => {
      // Skip if keybindings are disabled globally or locally
      if (!enabled || !keybindingsEnabled) {
        return;
      }

      // Get the shortcut key string from the event
      const shortcutKey = getKeybindingString(event);
      if (!shortcutKey) {
        return;
      }

      // Get the action for this shortcut key
      const action = getActionForKey(shortcutKey);
      if (!action) {
        return;
      }

      // Check if we have a handler for this action
      const handler = handlers[action];
      if (!handler) {
        return;
      }

      // Prevent default browser behavior if requested
      if (preventDefault) {
        event.preventDefault();
      }

      // Stop event propagation if requested
      if (stopPropagation) {
        event.stopPropagation();
      }

      // Execute the handler
      handler(event);
    },
    [
      enabled,
      keybindingsEnabled,
      getKeybindingString,
      getActionForKey,
      handlers,
      preventDefault,
      stopPropagation,
    ]
  );

  useEffect(() => {
    if (!enabled || !keybindingsEnabled || !ref.current) {
      return;
    }

    const element = ref.current;

    // Add event listener to the element
    element.addEventListener('keydown', handleKeyDown as EventListener);

    // Cleanup
    return () => {
      element.removeEventListener('keydown', handleKeyDown as EventListener);
    };
  }, [enabled, keybindingsEnabled, ref, handleKeyDown]);
}
