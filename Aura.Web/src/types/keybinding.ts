/**
 * Keyboard shortcut types for Aura Video Studio
 * Adapted from OpenCut project for cross-platform compatibility
 */

/**
 * Timeline actions that can be triggered by keyboard shortcuts
 */
export type TimelineAction =
  | 'toggle-play'
  | 'seek-backward'
  | 'seek-forward'
  | 'frame-step-backward'
  | 'frame-step-forward'
  | 'jump-backward'
  | 'jump-forward'
  | 'goto-start'
  | 'goto-end'
  | 'split-element'
  | 'toggle-snapping'
  | 'toggle-ripple-edit'
  | 'select-all'
  | 'deselect-all'
  | 'duplicate-selected'
  | 'copy-selected'
  | 'paste-selected'
  | 'cut-selected'
  | 'delete-selected'
  | 'undo'
  | 'redo'
  | 'zoom-in'
  | 'zoom-out'
  | 'zoom-to-fit'
  | 'set-in-point'
  | 'set-out-point'
  | 'clear-in-point'
  | 'clear-out-point'
  | 'add-marker'
  | 'next-marker'
  | 'prev-marker'
  | 'toggle-mute-selected'
  | 'toggle-hide-selected';

/**
 * Alt is also regarded as macOS OPTION (⌥) key
 * Ctrl is also regarded as macOS COMMAND (⌘) key
 */
export type ModifierKeys =
  | 'ctrl'
  | 'alt'
  | 'shift'
  | 'ctrl+shift'
  | 'alt+shift'
  | 'ctrl+alt'
  | 'ctrl+alt+shift';

/**
 * Valid keys for keyboard shortcuts
 */
export type Key =
  | 'a'
  | 'b'
  | 'c'
  | 'd'
  | 'e'
  | 'f'
  | 'g'
  | 'h'
  | 'i'
  | 'j'
  | 'k'
  | 'l'
  | 'm'
  | 'n'
  | 'o'
  | 'p'
  | 'q'
  | 'r'
  | 's'
  | 't'
  | 'u'
  | 'v'
  | 'w'
  | 'x'
  | 'y'
  | 'z'
  | '0'
  | '1'
  | '2'
  | '3'
  | '4'
  | '5'
  | '6'
  | '7'
  | '8'
  | '9'
  | 'up'
  | 'down'
  | 'left'
  | 'right'
  | '/'
  | '?'
  | '.'
  | '='
  | '-'
  | '+'
  | 'enter'
  | 'tab'
  | 'space'
  | 'escape'
  | 'esc'
  | 'backspace'
  | 'delete'
  | 'home'
  | 'end'
  | 'pageup'
  | 'pagedown'
  | 'f1'
  | 'f2'
  | 'f3'
  | 'f4'
  | 'f5'
  | 'f6'
  | 'f7'
  | 'f8'
  | 'f9'
  | 'f10'
  | 'f11'
  | 'f12';

export type ModifierBasedShortcutKey = `${ModifierKeys}+${Key}`;
export type SingleCharacterShortcutKey = `${Key}`;
export type ShortcutKey = ModifierBasedShortcutKey | SingleCharacterShortcutKey;

/**
 * Configuration mapping shortcut keys to timeline actions
 */
export type KeybindingConfig = {
  [key in ShortcutKey]?: TimelineAction;
};

/**
 * Represents a conflict when assigning a keybinding
 */
export interface KeybindingConflict {
  key: ShortcutKey;
  existingAction: TimelineAction;
  newAction: TimelineAction;
}

/**
 * Metadata for a keyboard shortcut
 */
export interface ShortcutMetadata {
  action: TimelineAction;
  key: ShortcutKey;
  description: string;
  category: 'playback' | 'editing' | 'navigation' | 'view' | 'selection';
}
