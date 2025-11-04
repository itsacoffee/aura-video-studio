/**
 * Utility functions for keyboard shortcut handling
 * Adapted from OpenCut project for cross-platform compatibility
 */

import type { ShortcutKey } from '../types/keybinding';

/**
 * Detects if the current device is an Apple device (macOS, iOS, iPadOS)
 */
export function isAppleDevice(): boolean {
  if (typeof window === 'undefined') return false;

  const platform = window.navigator.platform.toLowerCase();
  const userAgent = window.navigator.userAgent.toLowerCase();

  return (
    platform.includes('mac') ||
    platform.includes('iphone') ||
    platform.includes('ipad') ||
    userAgent.includes('mac') ||
    userAgent.includes('iphone') ||
    userAgent.includes('ipad')
  );
}

/**
 * Checks if a DOM element is typable (input, textarea, contenteditable)
 */
export function isTypableElement(element: Element): boolean {
  const tagName = element.tagName.toLowerCase();

  if (tagName === 'input' || tagName === 'textarea') {
    return true;
  }

  const contentEditable = element.getAttribute('contenteditable');
  return contentEditable === 'true' || contentEditable === '';
}

/**
 * Gets the pressed key from a keyboard event
 */
// eslint-disable-next-line sonarjs/cognitive-complexity
function getPressedKey(ev: KeyboardEvent): string | null {
  const key = (ev.key ?? '').toLowerCase();
  const code = ev.code ?? '';

  // Space key
  if (code === 'Space' || key === ' ' || key === 'spacebar' || key === 'space') {
    return 'space';
  }

  // Arrow keys
  if (key.startsWith('arrow')) {
    return key.slice(5); // Remove "arrow" prefix
  }

  // Special keys
  if (key === 'tab') return 'tab';
  if (key === 'home') return 'home';
  if (key === 'end') return 'end';
  if (key === 'delete') return 'delete';
  if (key === 'backspace') return 'backspace';
  if (key === 'escape' || key === 'esc') return 'escape';
  if (key === 'pageup') return 'pageup';
  if (key === 'pagedown') return 'pagedown';
  if (key === 'enter') return 'enter';

  // Function keys
  if (key.startsWith('f') && key.length >= 2 && key.length <= 3) {
    const num = parseInt(key.slice(1), 10);
    if (!isNaN(num) && num >= 1 && num <= 12) {
      return key; // f1-f12
    }
  }

  // Letter keys (using code for better keyboard layout support)
  if (code.startsWith('Key')) {
    const letter = code.slice(3).toLowerCase();
    if (letter.length === 1 && letter >= 'a' && letter <= 'z') {
      return letter;
    }
  }

  // Number keys (using code for AZERTY support, with fallback)
  if (code.startsWith('Digit')) {
    const digit = code.slice(5);
    if (digit.length === 1 && digit >= '0' && digit <= '9') {
      return digit;
    }
  } else if (key.length === 1 && key >= '0' && key <= '9') {
    // Fallback for other layouts where code is not available
    return key;
  }

  // Other valid keys
  if (key === '/' || key === '?' || key === '.') {
    return key;
  }

  // Not a valid shortcut key
  return null;
}

/**
 * Gets the active modifier key(s) from a keyboard event
 */
function getActiveModifier(ev: KeyboardEvent): string | null {
  const modifierKeys = {
    ctrl: isAppleDevice() ? ev.metaKey : ev.ctrlKey,
    alt: ev.altKey,
    shift: ev.shiftKey,
  };

  // Build modifier string in consistent order: ctrl, alt, shift
  const activeModifier = Object.keys(modifierKeys)
    .filter((key) => modifierKeys[key as keyof typeof modifierKeys])
    .join('+');

  return activeModifier === '' ? null : activeModifier;
}

/**
 * Generates a shortcut key string from a keyboard event
 * Returns null if the event should not trigger a shortcut
 */
export function generateKeybindingString(ev: KeyboardEvent): ShortcutKey | null {
  const target = ev.target;

  // Get modifier and key
  const modifierKey = getActiveModifier(ev);
  const key = getPressedKey(ev);

  if (!key) return null;

  // Modifier-based shortcuts are always valid
  if (modifierKey) {
    // Ignore shift+key when typing in input fields
    if (modifierKey === 'shift' && target instanceof Element && isTypableElement(target)) {
      return null;
    }

    return `${modifierKey}+${key}` as ShortcutKey;
  }

  // Single-key shortcuts are ignored when typing
  if (target instanceof Element && isTypableElement(target)) {
    return null;
  }

  return key as ShortcutKey;
}

/**
 * Formats a shortcut key for display (e.g., "ctrl+s" -> "Ctrl+S" or "⌘S")
 */
export function formatShortcutForDisplay(shortcut: ShortcutKey): string {
  const isApple = isAppleDevice();

  // Split modifier and key
  const parts = shortcut.split('+');

  if (parts.length === 1) {
    // Single key
    return formatKey(parts[0]);
  }

  // Modifier + key
  const modifiers = parts.slice(0, -1);
  const key = parts[parts.length - 1];

  const formattedModifiers = modifiers.map((mod) => {
    switch (mod) {
      case 'ctrl':
        return isApple ? '⌘' : 'Ctrl';
      case 'alt':
        return isApple ? '⌥' : 'Alt';
      case 'shift':
        return isApple ? '⇧' : 'Shift';
      default:
        return mod.charAt(0).toUpperCase() + mod.slice(1);
    }
  });

  const formattedKey = formatKey(key);

  if (isApple) {
    return formattedModifiers.join('') + formattedKey;
  }

  return formattedModifiers.join('+') + '+' + formattedKey;
}

/**
 * Formats a single key for display
 */
function formatKey(key: string): string {
  // Special key names
  const specialKeys: Record<string, string> = {
    space: 'Space',
    enter: 'Enter',
    tab: 'Tab',
    escape: 'Esc',
    esc: 'Esc',
    delete: 'Del',
    backspace: '⌫',
    up: '↑',
    down: '↓',
    left: '←',
    right: '→',
    pageup: 'PgUp',
    pagedown: 'PgDn',
    home: 'Home',
    end: 'End',
  };

  if (key in specialKeys) {
    return specialKeys[key];
  }

  // Function keys
  if (key.startsWith('f') && key.length >= 2) {
    return key.toUpperCase();
  }

  // Regular keys
  return key.toUpperCase();
}

/**
 * Gets the description of a shortcut key category
 */
export function getCategoryDescription(category: string): string {
  const descriptions: Record<string, string> = {
    playback: 'Playback Controls',
    editing: 'Editing Operations',
    navigation: 'Timeline Navigation',
    view: 'View & Display',
    selection: 'Selection & Clipboard',
  };

  return descriptions[category] || category;
}
