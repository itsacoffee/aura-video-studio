/**
 * Focus Management Utilities
 *
 * Provides utilities for managing keyboard focus, including:
 * - Focus trapping for modals
 * - Focus restoration
 * - Focusable element queries
 * - Tab order management
 */

/**
 * Query selector for focusable elements
 */
const FOCUSABLE_ELEMENTS_SELECTOR = [
  'a[href]',
  'area[href]',
  'input:not([disabled]):not([type="hidden"])',
  'select:not([disabled])',
  'textarea:not([disabled])',
  'button:not([disabled])',
  'iframe',
  'object',
  'embed',
  '[contenteditable]',
  '[tabindex]:not([tabindex="-1"])',
].join(',');

/**
 * Get all focusable elements within a container
 */
export function getFocusableElements(container: HTMLElement): HTMLElement[] {
  const elements = Array.from(container.querySelectorAll(FOCUSABLE_ELEMENTS_SELECTOR));
  return elements.filter((el) => {
    // Filter out invisible or inert elements
    const element = el as HTMLElement;
    return (
      element.offsetParent !== null &&
      window.getComputedStyle(element).visibility !== 'hidden' &&
      !element.hasAttribute('inert')
    );
  }) as HTMLElement[];
}

/**
 * Get the first focusable element within a container
 */
export function getFirstFocusableElement(container: HTMLElement): HTMLElement | null {
  const elements = getFocusableElements(container);
  return elements[0] || null;
}

/**
 * Get the last focusable element within a container
 */
export function getLastFocusableElement(container: HTMLElement): HTMLElement | null {
  const elements = getFocusableElements(container);
  return elements[elements.length - 1] || null;
}

/**
 * Focus trap class for managing focus within a modal or dialog
 */
export class FocusTrap {
  private container: HTMLElement;
  private previousFocus: HTMLElement | null = null;
  private handleKeyDown: (e: KeyboardEvent) => void;

  constructor(container: HTMLElement) {
    this.container = container;
    this.handleKeyDown = this.onKeyDown.bind(this);
  }

  /**
   * Activate the focus trap
   */
  activate(): void {
    // Save the currently focused element
    this.previousFocus = document.activeElement as HTMLElement;

    // Focus the first focusable element in the container
    const firstFocusable = getFirstFocusableElement(this.container);
    if (firstFocusable) {
      // Small delay to ensure the container is visible
      setTimeout(() => firstFocusable.focus(), 50);
    }

    // Add keyboard listener
    this.container.addEventListener('keydown', this.handleKeyDown);
  }

  /**
   * Deactivate the focus trap
   */
  deactivate(): void {
    // Remove keyboard listener
    this.container.removeEventListener('keydown', this.handleKeyDown);

    // Restore focus to the previously focused element
    if (this.previousFocus && document.body.contains(this.previousFocus)) {
      this.previousFocus.focus();
    }
  }

  /**
   * Handle keyboard events to trap focus
   */
  private onKeyDown(e: KeyboardEvent): void {
    if (e.key !== 'Tab') return;

    const focusableElements = getFocusableElements(this.container);
    if (focusableElements.length === 0) return;

    const firstFocusable = focusableElements[0];
    const lastFocusable = focusableElements[focusableElements.length - 1];

    // Shift + Tab (backward)
    if (e.shiftKey) {
      if (document.activeElement === firstFocusable) {
        e.preventDefault();
        lastFocusable.focus();
      }
    }
    // Tab (forward)
    else {
      if (document.activeElement === lastFocusable) {
        e.preventDefault();
        firstFocusable.focus();
      }
    }
  }
}

/**
 * Create and activate a focus trap
 */
export function createFocusTrap(container: HTMLElement): FocusTrap {
  const trap = new FocusTrap(container);
  trap.activate();
  return trap;
}

/**
 * Restore focus to a previously focused element
 */
export function restoreFocus(element: HTMLElement | null): void {
  if (element && document.body.contains(element)) {
    element.focus();
  }
}

/**
 * Move focus to the next focusable element
 */
export function focusNext(container: HTMLElement = document.body): void {
  const focusableElements = getFocusableElements(container);
  const currentIndex = focusableElements.indexOf(document.activeElement as HTMLElement);

  if (currentIndex === -1 || currentIndex === focusableElements.length - 1) {
    focusableElements[0]?.focus();
  } else {
    focusableElements[currentIndex + 1]?.focus();
  }
}

/**
 * Move focus to the previous focusable element
 */
export function focusPrevious(container: HTMLElement = document.body): void {
  const focusableElements = getFocusableElements(container);
  const currentIndex = focusableElements.indexOf(document.activeElement as HTMLElement);

  if (currentIndex === -1 || currentIndex === 0) {
    focusableElements[focusableElements.length - 1]?.focus();
  } else {
    focusableElements[currentIndex - 1]?.focus();
  }
}

/**
 * Check if an element is focusable
 */
export function isFocusable(element: HTMLElement): boolean {
  return getFocusableElements(element.parentElement || document.body).includes(element);
}
