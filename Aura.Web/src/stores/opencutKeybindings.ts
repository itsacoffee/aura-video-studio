/**
 * OpenCut Keybindings Store
 *
 * Manages keyboard shortcuts for the OpenCut editor including JKL playback
 * control, mark in/out, ripple editing shortcuts, and customizable keybindings.
 * Uses persistence to save user customizations.
 */

import { create } from 'zustand';
import { persist } from 'zustand/middleware';

/**
 * Modifier keys for keybindings
 */
export interface KeyModifiers {
  ctrl?: boolean;
  shift?: boolean;
  alt?: boolean;
  meta?: boolean;
}

/**
 * Categories for organizing keybindings
 */
export type KeybindingCategory =
  | 'playback'
  | 'editing'
  | 'navigation'
  | 'selection'
  | 'markers'
  | 'view'
  | 'file';

/**
 * Individual keybinding definition
 */
export interface Keybinding {
  /** Unique identifier */
  id: string;
  /** Action name to dispatch */
  action: string;
  /** Key code or character */
  key: string;
  /** Modifier keys required */
  modifiers: KeyModifiers;
  /** Category for grouping in UI */
  category: KeybindingCategory;
  /** Human-readable description */
  description: string;
  /** Whether keybinding is enabled */
  enabled: boolean;
}

/**
 * Default keybindings following professional NLE conventions
 */
export const DEFAULT_KEYBINDINGS: Keybinding[] = [
  // Playback - JKL controls
  {
    id: 'play-reverse',
    action: 'playReverse',
    key: 'j',
    modifiers: {},
    category: 'playback',
    description: 'Play reverse',
    enabled: true,
  },
  {
    id: 'pause',
    action: 'pause',
    key: 'k',
    modifiers: {},
    category: 'playback',
    description: 'Pause',
    enabled: true,
  },
  {
    id: 'play-forward',
    action: 'playForward',
    key: 'l',
    modifiers: {},
    category: 'playback',
    description: 'Play forward',
    enabled: true,
  },
  {
    id: 'play-pause',
    action: 'togglePlayPause',
    key: ' ',
    modifiers: {},
    category: 'playback',
    description: 'Play/Pause',
    enabled: true,
  },
  {
    id: 'stop',
    action: 'stop',
    key: 's',
    modifiers: { ctrl: true },
    category: 'playback',
    description: 'Stop',
    enabled: true,
  },

  // Navigation
  {
    id: 'go-to-start',
    action: 'goToStart',
    key: 'Home',
    modifiers: {},
    category: 'navigation',
    description: 'Go to start',
    enabled: true,
  },
  {
    id: 'go-to-end',
    action: 'goToEnd',
    key: 'End',
    modifiers: {},
    category: 'navigation',
    description: 'Go to end',
    enabled: true,
  },
  {
    id: 'prev-frame',
    action: 'prevFrame',
    key: 'ArrowLeft',
    modifiers: {},
    category: 'navigation',
    description: 'Previous frame',
    enabled: true,
  },
  {
    id: 'next-frame',
    action: 'nextFrame',
    key: 'ArrowRight',
    modifiers: {},
    category: 'navigation',
    description: 'Next frame',
    enabled: true,
  },
  {
    id: 'prev-edit',
    action: 'prevEdit',
    key: 'ArrowUp',
    modifiers: {},
    category: 'navigation',
    description: 'Previous edit point',
    enabled: true,
  },
  {
    id: 'next-edit',
    action: 'nextEdit',
    key: 'ArrowDown',
    modifiers: {},
    category: 'navigation',
    description: 'Next edit point',
    enabled: true,
  },
  {
    id: 'nudge-left',
    action: 'nudgeLeft',
    key: ',',
    modifiers: {},
    category: 'navigation',
    description: 'Nudge left 1 frame',
    enabled: true,
  },
  {
    id: 'nudge-right',
    action: 'nudgeRight',
    key: '.',
    modifiers: {},
    category: 'navigation',
    description: 'Nudge right 1 frame',
    enabled: true,
  },

  // Marking
  {
    id: 'mark-in',
    action: 'markIn',
    key: 'i',
    modifiers: {},
    category: 'editing',
    description: 'Set in point',
    enabled: true,
  },
  {
    id: 'mark-out',
    action: 'markOut',
    key: 'o',
    modifiers: {},
    category: 'editing',
    description: 'Set out point',
    enabled: true,
  },
  {
    id: 'clear-in',
    action: 'clearIn',
    key: 'i',
    modifiers: { alt: true },
    category: 'editing',
    description: 'Clear in point',
    enabled: true,
  },
  {
    id: 'clear-out',
    action: 'clearOut',
    key: 'o',
    modifiers: { alt: true },
    category: 'editing',
    description: 'Clear out point',
    enabled: true,
  },
  {
    id: 'go-to-in',
    action: 'goToIn',
    key: 'i',
    modifiers: { shift: true },
    category: 'navigation',
    description: 'Go to in point',
    enabled: true,
  },
  {
    id: 'go-to-out',
    action: 'goToOut',
    key: 'o',
    modifiers: { shift: true },
    category: 'navigation',
    description: 'Go to out point',
    enabled: true,
  },

  // Editing
  {
    id: 'split',
    action: 'splitAtPlayhead',
    key: 's',
    modifiers: {},
    category: 'editing',
    description: 'Split at playhead',
    enabled: true,
  },
  {
    id: 'ripple-delete',
    action: 'rippleDelete',
    key: 'Backspace',
    modifiers: { shift: true },
    category: 'editing',
    description: 'Ripple delete',
    enabled: true,
  },
  {
    id: 'delete',
    action: 'delete',
    key: 'Backspace',
    modifiers: {},
    category: 'editing',
    description: 'Delete',
    enabled: true,
  },
  {
    id: 'delete-alt',
    action: 'delete',
    key: 'Delete',
    modifiers: {},
    category: 'editing',
    description: 'Delete (Del key)',
    enabled: true,
  },
  {
    id: 'duplicate',
    action: 'duplicate',
    key: 'd',
    modifiers: { ctrl: true },
    category: 'editing',
    description: 'Duplicate',
    enabled: true,
  },
  {
    id: 'ripple-trim-start',
    action: 'rippleTrimStart',
    key: 'q',
    modifiers: {},
    category: 'editing',
    description: 'Ripple trim start to playhead',
    enabled: true,
  },
  {
    id: 'ripple-trim-end',
    action: 'rippleTrimEnd',
    key: 'w',
    modifiers: {},
    category: 'editing',
    description: 'Ripple trim end to playhead',
    enabled: true,
  },

  // Selection
  {
    id: 'select-all',
    action: 'selectAll',
    key: 'a',
    modifiers: { ctrl: true },
    category: 'selection',
    description: 'Select all',
    enabled: true,
  },
  {
    id: 'deselect-all',
    action: 'deselectAll',
    key: 'Escape',
    modifiers: {},
    category: 'selection',
    description: 'Deselect all',
    enabled: true,
  },
  {
    id: 'select-clip-at-playhead',
    action: 'selectAtPlayhead',
    key: 'd',
    modifiers: {},
    category: 'selection',
    description: 'Select clip at playhead',
    enabled: true,
  },

  // Markers
  {
    id: 'add-marker',
    action: 'addMarker',
    key: 'm',
    modifiers: {},
    category: 'markers',
    description: 'Add marker',
    enabled: true,
  },
  {
    id: 'prev-marker',
    action: 'prevMarker',
    key: ';',
    modifiers: {},
    category: 'markers',
    description: 'Go to previous marker',
    enabled: true,
  },
  {
    id: 'next-marker',
    action: 'nextMarker',
    key: "'",
    modifiers: {},
    category: 'markers',
    description: 'Go to next marker',
    enabled: true,
  },

  // View
  {
    id: 'zoom-in',
    action: 'zoomIn',
    key: '=',
    modifiers: { ctrl: true },
    category: 'view',
    description: 'Zoom in',
    enabled: true,
  },
  {
    id: 'zoom-out',
    action: 'zoomOut',
    key: '-',
    modifiers: { ctrl: true },
    category: 'view',
    description: 'Zoom out',
    enabled: true,
  },
  {
    id: 'fit-timeline',
    action: 'fitTimeline',
    key: '0',
    modifiers: { ctrl: true },
    category: 'view',
    description: 'Fit timeline to window',
    enabled: true,
  },

  // Undo/Redo
  {
    id: 'undo',
    action: 'undo',
    key: 'z',
    modifiers: { ctrl: true },
    category: 'editing',
    description: 'Undo',
    enabled: true,
  },
  {
    id: 'redo',
    action: 'redo',
    key: 'z',
    modifiers: { ctrl: true, shift: true },
    category: 'editing',
    description: 'Redo',
    enabled: true,
  },
  {
    id: 'redo-alt',
    action: 'redo',
    key: 'y',
    modifiers: { ctrl: true },
    category: 'editing',
    description: 'Redo (Ctrl+Y)',
    enabled: true,
  },
];

interface OpenCutKeybindingsState {
  /** All keybindings (defaults + user customizations) */
  keybindings: Keybinding[];
  /** JKL playback speed multiplier */
  jklSpeed: number;
  /** Direction of JKL playback (-1 = reverse, 0 = paused, 1 = forward) */
  jklDirection: number;
}

interface OpenCutKeybindingsActions {
  /**
   * Update a keybinding's properties
   */
  updateKeybinding: (id: string, updates: Partial<Keybinding>) => void;

  /**
   * Reset a specific keybinding to its default
   */
  resetKeybinding: (id: string) => void;

  /**
   * Reset all keybindings to defaults
   */
  resetAllKeybindings: () => void;

  /**
   * Get keybinding for a specific action
   */
  getKeybindingForAction: (action: string) => Keybinding | undefined;

  /**
   * Get all keybindings in a category
   */
  getKeybindingsForCategory: (category: KeybindingCategory) => Keybinding[];

  /**
   * Check if a key combination conflicts with existing bindings
   */
  isKeybindingConflict: (key: string, modifiers: KeyModifiers, excludeId?: string) => boolean;

  /**
   * Find keybinding by key combination
   */
  findKeybindingByKey: (key: string, modifiers: KeyModifiers) => Keybinding | undefined;

  /**
   * Set JKL playback speed
   */
  setJKLSpeed: (speed: number) => void;

  /**
   * Increment JKL speed (double it)
   */
  incrementJKLSpeed: () => void;

  /**
   * Decrement JKL speed (halve it)
   */
  decrementJKLSpeed: () => void;

  /**
   * Set JKL direction
   */
  setJKLDirection: (direction: number) => void;

  /**
   * Reset JKL state
   */
  resetJKL: () => void;
}

export type OpenCutKeybindingsStore = OpenCutKeybindingsState & OpenCutKeybindingsActions;

/**
 * Check if modifier keys match
 */
function modifiersMatch(a: KeyModifiers, b: KeyModifiers): boolean {
  return (
    !!a.ctrl === !!b.ctrl && !!a.shift === !!b.shift && !!a.alt === !!b.alt && !!a.meta === !!b.meta
  );
}

export const useOpenCutKeybindingsStore = create<OpenCutKeybindingsStore>()(
  persist(
    (set, get) => ({
      keybindings: DEFAULT_KEYBINDINGS,
      jklSpeed: 1,
      jklDirection: 0,

      updateKeybinding: (id, updates) => {
        set((state) => ({
          keybindings: state.keybindings.map((kb) => (kb.id === id ? { ...kb, ...updates } : kb)),
        }));
      },

      resetKeybinding: (id) => {
        const defaultKb = DEFAULT_KEYBINDINGS.find((kb) => kb.id === id);
        if (defaultKb) {
          set((state) => ({
            keybindings: state.keybindings.map((kb) => (kb.id === id ? { ...defaultKb } : kb)),
          }));
        }
      },

      resetAllKeybindings: () => set({ keybindings: [...DEFAULT_KEYBINDINGS] }),

      getKeybindingForAction: (action) => {
        return get().keybindings.find((kb) => kb.action === action && kb.enabled);
      },

      getKeybindingsForCategory: (category) => {
        return get().keybindings.filter((kb) => kb.category === category);
      },

      isKeybindingConflict: (key, modifiers, excludeId) => {
        return get().keybindings.some((kb) => {
          if (excludeId && kb.id === excludeId) return false;
          if (!kb.enabled) return false;
          if (kb.key.toLowerCase() !== key.toLowerCase()) return false;
          return modifiersMatch(kb.modifiers, modifiers);
        });
      },

      findKeybindingByKey: (key, modifiers) => {
        return get().keybindings.find(
          (kb) =>
            kb.enabled &&
            kb.key.toLowerCase() === key.toLowerCase() &&
            modifiersMatch(kb.modifiers, modifiers)
        );
      },

      setJKLSpeed: (speed) => set({ jklSpeed: Math.max(0.25, Math.min(8, speed)) }),

      incrementJKLSpeed: () => {
        const { jklSpeed } = get();
        get().setJKLSpeed(Math.min(8, jklSpeed * 2));
      },

      decrementJKLSpeed: () => {
        const { jklSpeed } = get();
        get().setJKLSpeed(Math.max(0.25, jklSpeed / 2));
      },

      setJKLDirection: (direction) => set({ jklDirection: Math.max(-1, Math.min(1, direction)) }),

      resetJKL: () => set({ jklSpeed: 1, jklDirection: 0 }),
    }),
    {
      name: 'opencut-keybindings',
      // Only persist keybindings, not transient JKL state
      partialize: (state) => ({ keybindings: state.keybindings }),
    }
  )
);
