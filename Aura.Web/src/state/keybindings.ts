/**
 * Keyboard shortcuts state management for Aura Video Studio
 * Provides customizable keyboard shortcuts with conflict detection and persistence
 */

import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type {
  KeybindingConfig,
  KeybindingConflict,
  ShortcutKey,
  TimelineAction,
  ShortcutMetadata,
} from '../types/keybinding';
import { generateKeybindingString } from '../utils/keybinding-utils';

/**
 * Default keyboard shortcuts configuration
 * Based on industry-standard video editing shortcuts
 */
export const defaultKeybindings: KeybindingConfig = {
  // Playback controls (JKL shuttle)
  // Note: Both Space and K trigger toggle-play (industry standard)
  space: 'toggle-play',
  j: 'seek-backward',
  k: 'toggle-play', // K is pause in JKL shuttle (same as Space)
  l: 'seek-forward',

  // Frame navigation
  left: 'frame-step-backward',
  right: 'frame-step-forward',
  'shift+left': 'jump-backward',
  'shift+right': 'jump-forward',

  // Jump to start/end
  home: 'goto-start',
  end: 'goto-end',

  // Editing operations
  s: 'split-element',

  // Toggle features
  n: 'toggle-snapping',
  r: 'toggle-ripple-edit',

  // Selection and clipboard
  'ctrl+a': 'select-all',
  'ctrl+shift+a': 'deselect-all',
  'ctrl+d': 'duplicate-selected',
  'ctrl+c': 'copy-selected',
  'ctrl+v': 'paste-selected',
  'ctrl+x': 'cut-selected',
  delete: 'delete-selected',
  backspace: 'delete-selected',

  // Undo/redo
  'ctrl+z': 'undo',
  'ctrl+shift+z': 'redo',
  'ctrl+y': 'redo',

  // Zoom controls
  'ctrl+=': 'zoom-in',
  'ctrl+-': 'zoom-out',
  'ctrl+0': 'zoom-to-fit',

  // In/Out points
  i: 'set-in-point',
  o: 'set-out-point',
  'ctrl+shift+i': 'clear-in-point',
  'ctrl+shift+o': 'clear-out-point',

  // Marker navigation
  m: 'add-marker',
  'shift+m': 'next-marker',
  'ctrl+shift+m': 'prev-marker',

  // Element properties
  'shift+h': 'toggle-hide-selected',
};

/**
 * Metadata for all available shortcuts
 */
export const shortcutMetadata: ShortcutMetadata[] = [
  { action: 'toggle-play', key: 'space', description: 'Play/Pause', category: 'playback' },
  { action: 'seek-backward', key: 'j', description: 'Play Reverse', category: 'playback' },
  { action: 'seek-forward', key: 'l', description: 'Play Forward', category: 'playback' },
  {
    action: 'frame-step-backward',
    key: 'left',
    description: 'Previous Frame',
    category: 'navigation',
  },
  { action: 'frame-step-forward', key: 'right', description: 'Next Frame', category: 'navigation' },
  {
    action: 'jump-backward',
    key: 'shift+left',
    description: 'Jump Back 10 Frames',
    category: 'navigation',
  },
  {
    action: 'jump-forward',
    key: 'shift+right',
    description: 'Jump Forward 10 Frames',
    category: 'navigation',
  },
  { action: 'goto-start', key: 'home', description: 'Go to Start', category: 'navigation' },
  { action: 'goto-end', key: 'end', description: 'Go to End', category: 'navigation' },
  { action: 'split-element', key: 's', description: 'Split Clip at Playhead', category: 'editing' },
  { action: 'toggle-snapping', key: 'n', description: 'Toggle Snapping', category: 'view' },
  {
    action: 'toggle-ripple-edit',
    key: 'r',
    description: 'Toggle Ripple Edit',
    category: 'editing',
  },
  { action: 'select-all', key: 'ctrl+a', description: 'Select All', category: 'selection' },
  {
    action: 'deselect-all',
    key: 'ctrl+shift+a',
    description: 'Deselect All',
    category: 'selection',
  },
  {
    action: 'duplicate-selected',
    key: 'ctrl+d',
    description: 'Duplicate Selected',
    category: 'editing',
  },
  { action: 'copy-selected', key: 'ctrl+c', description: 'Copy Selected', category: 'selection' },
  { action: 'paste-selected', key: 'ctrl+v', description: 'Paste', category: 'selection' },
  { action: 'cut-selected', key: 'ctrl+x', description: 'Cut Selected', category: 'selection' },
  { action: 'delete-selected', key: 'delete', description: 'Delete Selected', category: 'editing' },
  { action: 'undo', key: 'ctrl+z', description: 'Undo', category: 'editing' },
  { action: 'redo', key: 'ctrl+shift+z', description: 'Redo', category: 'editing' },
  { action: 'zoom-in', key: 'ctrl+=', description: 'Zoom In', category: 'view' },
  { action: 'zoom-out', key: 'ctrl+-', description: 'Zoom Out', category: 'view' },
  { action: 'zoom-to-fit', key: 'ctrl+0', description: 'Zoom to Fit', category: 'view' },
  { action: 'set-in-point', key: 'i', description: 'Set In Point', category: 'editing' },
  { action: 'set-out-point', key: 'o', description: 'Set Out Point', category: 'editing' },
  {
    action: 'clear-in-point',
    key: 'ctrl+shift+i',
    description: 'Clear In Point',
    category: 'editing',
  },
  {
    action: 'clear-out-point',
    key: 'ctrl+shift+o',
    description: 'Clear Out Point',
    category: 'editing',
  },
  { action: 'add-marker', key: 'm', description: 'Add Marker', category: 'editing' },
  { action: 'next-marker', key: 'shift+m', description: 'Next Marker', category: 'navigation' },
  {
    action: 'prev-marker',
    key: 'ctrl+shift+m',
    description: 'Previous Marker',
    category: 'navigation',
  },
  {
    action: 'toggle-hide-selected',
    key: 'shift+h',
    description: 'Toggle Hide Selected',
    category: 'editing',
  },
];

interface KeybindingsState {
  keybindings: KeybindingConfig;
  isCustomized: boolean;
  keybindingsEnabled: boolean;
  isRecording: boolean;

  // Actions
  updateKeybinding: (key: ShortcutKey, action: TimelineAction) => void;
  removeKeybinding: (key: ShortcutKey) => void;
  resetToDefaults: () => void;
  importKeybindings: (config: KeybindingConfig) => void;
  exportKeybindings: () => KeybindingConfig;
  enableKeybindings: () => void;
  disableKeybindings: () => void;
  setIsRecording: (isRecording: boolean) => void;

  // Validation
  validateKeybinding: (key: ShortcutKey, action: TimelineAction) => KeybindingConflict | null;
  getKeybindingsForAction: (action: TimelineAction) => ShortcutKey[];

  // Utility
  getKeybindingString: (ev: KeyboardEvent) => ShortcutKey | null;
  getActionForKey: (key: ShortcutKey) => TimelineAction | undefined;
}

export const useKeybindingsStore = create<KeybindingsState>()(
  persist(
    (set, get) => ({
      keybindings: { ...defaultKeybindings },
      isCustomized: false,
      keybindingsEnabled: true,
      isRecording: false,

      updateKeybinding: (key: ShortcutKey, action: TimelineAction) => {
        set((state) => {
          const newKeybindings = { ...state.keybindings };
          newKeybindings[key] = action;

          return {
            keybindings: newKeybindings,
            isCustomized: true,
          };
        });
      },

      removeKeybinding: (key: ShortcutKey) => {
        set((state) => {
          const newKeybindings = { ...state.keybindings };
          delete newKeybindings[key];

          return {
            keybindings: newKeybindings,
            isCustomized: true,
          };
        });
      },

      resetToDefaults: () => {
        set({
          keybindings: { ...defaultKeybindings },
          isCustomized: false,
        });
      },

      importKeybindings: (config: KeybindingConfig) => {
        // Validate all keys and actions
        for (const [key, action] of Object.entries(config)) {
          if (typeof key !== 'string' || key.length === 0) {
            throw new Error(`Invalid key format: ${key}`);
          }
          if (typeof action !== 'string' || action.length === 0) {
            throw new Error(`Invalid action format: ${action}`);
          }
        }

        set({
          keybindings: { ...config },
          isCustomized: true,
        });
      },

      exportKeybindings: () => {
        return get().keybindings;
      },

      enableKeybindings: () => {
        set({ keybindingsEnabled: true });
      },

      disableKeybindings: () => {
        set({ keybindingsEnabled: false });
      },

      setIsRecording: (isRecording: boolean) => {
        set({ isRecording });
      },

      validateKeybinding: (key: ShortcutKey, action: TimelineAction) => {
        const { keybindings } = get();
        const existingAction = keybindings[key];

        if (existingAction && existingAction !== action) {
          return {
            key,
            existingAction,
            newAction: action,
          };
        }

        return null;
      },

      getKeybindingsForAction: (action: TimelineAction) => {
        const { keybindings } = get();
        return Object.keys(keybindings).filter(
          (key) => keybindings[key as ShortcutKey] === action
        ) as ShortcutKey[];
      },

      getKeybindingString: (ev: KeyboardEvent) => {
        return generateKeybindingString(ev);
      },

      getActionForKey: (key: ShortcutKey) => {
        const { keybindings } = get();
        return keybindings[key];
      },
    }),
    {
      name: 'aura-keybindings-storage',
      version: 1,
    }
  )
);
