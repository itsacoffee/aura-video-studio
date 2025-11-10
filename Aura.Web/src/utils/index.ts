/**
 * Utility Functions Index
 * 
 * Central exports for commonly used utility functions
 */

// Focus Management
export {
  getFocusableElements,
  getFirstFocusableElement,
  getLastFocusableElement,
  FocusTrap,
  createFocusTrap,
  restoreFocus,
  focusNext,
  focusPrevious,
  isFocusable,
} from './focusManagement';

// Keybinding Utilities
export {
  isAppleDevice,
  isTypableElement,
  generateKeybindingString,
  formatShortcutForDisplay,
  getCategoryDescription,
} from './keybinding-utils';
