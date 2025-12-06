/**
 * Focus Management Hooks
 *
 * Utilities for managing focus in modals, dialogs, and other interactive components.
 * Includes focus trapping and focus restoration for accessibility.
 */

import { useCallback, useRef, useEffect } from 'react';

/**
 * Selectors for focusable elements
 */
const FOCUSABLE_SELECTORS = [
  'button:not([disabled])',
  '[href]',
  'input:not([disabled])',
  'select:not([disabled])',
  'textarea:not([disabled])',
  '[tabindex]:not([tabindex="-1"])',
].join(', ');

/**
 * Hook to create a focus trap within a container.
 * Focus will cycle through focusable elements when using Tab/Shift+Tab.
 *
 * @returns Ref to attach to the container element
 *
 * @example
 * ```tsx
 * function Modal({ isOpen, onClose }) {
 *   const containerRef = useFocusTrapContainer<HTMLDivElement>();
 *
 *   useEffect(() => {
 *     if (isOpen && containerRef.current) {
 *       const firstFocusable = containerRef.current.querySelector(FOCUSABLE_SELECTORS);
 *       (firstFocusable as HTMLElement)?.focus();
 *     }
 *   }, [isOpen]);
 *
 *   return (
 *     <div ref={containerRef} role="dialog">
 *       <h2>Modal Title</h2>
 *       <button onClick={onClose}>Close</button>
 *     </div>
 *   );
 * }
 * ```
 */
export function useFocusTrapContainer<T extends HTMLElement>() {
  const containerRef = useRef<T>(null);

  useEffect(() => {
    const container = containerRef.current;
    if (!container) return;

    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key !== 'Tab') return;

      const focusableElements = container.querySelectorAll<HTMLElement>(FOCUSABLE_SELECTORS);

      if (focusableElements.length === 0) return;

      const firstElement = focusableElements[0];
      const lastElement = focusableElements[focusableElements.length - 1];

      if (e.shiftKey && document.activeElement === firstElement) {
        e.preventDefault();
        lastElement?.focus();
      } else if (!e.shiftKey && document.activeElement === lastElement) {
        e.preventDefault();
        firstElement?.focus();
      }
    };

    container.addEventListener('keydown', handleKeyDown);
    return () => container.removeEventListener('keydown', handleKeyDown);
  }, []);

  return containerRef;
}

/**
 * Hook to save and restore focus.
 * Useful when opening/closing modals or dialogs.
 *
 * @returns Object with saveFocus and restoreFocus functions
 *
 * @example
 * ```tsx
 * function Dialog({ isOpen, onClose }) {
 *   const { saveFocus, restoreFocus } = useRestoreFocus();
 *
 *   useEffect(() => {
 *     if (isOpen) {
 *       saveFocus();
 *     } else {
 *       restoreFocus();
 *     }
 *   }, [isOpen, saveFocus, restoreFocus]);
 *
 *   return isOpen ? <div>Dialog content</div> : null;
 * }
 * ```
 */
export function useRestoreFocus() {
  const previousFocusRef = useRef<HTMLElement | null>(null);

  const saveFocus = useCallback(() => {
    previousFocusRef.current = document.activeElement as HTMLElement;
  }, []);

  const restoreFocus = useCallback(() => {
    if (previousFocusRef.current && typeof previousFocusRef.current.focus === 'function') {
      previousFocusRef.current.focus();
      previousFocusRef.current = null;
    }
  }, []);

  return { saveFocus, restoreFocus };
}

/**
 * Combined hook for focus trap with automatic focus restoration.
 *
 * @param isActive - Whether the focus trap is active
 * @returns Ref to attach to the container element
 *
 * @example
 * ```tsx
 * function Modal({ isOpen }) {
 *   const containerRef = useFocusManagement<HTMLDivElement>(isOpen);
 *
 *   return (
 *     <div ref={containerRef} role="dialog">
 *       <button>Action</button>
 *       <button>Close</button>
 *     </div>
 *   );
 * }
 * ```
 */
export function useFocusManagement<T extends HTMLElement>(isActive: boolean) {
  const containerRef = useRef<T>(null);
  const previousFocusRef = useRef<HTMLElement | null>(null);

  useEffect(() => {
    const container = containerRef.current;
    if (!container) return;

    if (isActive) {
      // Save current focus
      previousFocusRef.current = document.activeElement as HTMLElement;

      // Focus first focusable element
      const focusableElements = container.querySelectorAll<HTMLElement>(FOCUSABLE_SELECTORS);
      focusableElements[0]?.focus();

      // Set up focus trap
      const handleKeyDown = (e: KeyboardEvent) => {
        if (e.key !== 'Tab') return;

        const elements = container.querySelectorAll<HTMLElement>(FOCUSABLE_SELECTORS);
        if (elements.length === 0) return;

        const firstElement = elements[0];
        const lastElement = elements[elements.length - 1];

        if (e.shiftKey && document.activeElement === firstElement) {
          e.preventDefault();
          lastElement?.focus();
        } else if (!e.shiftKey && document.activeElement === lastElement) {
          e.preventDefault();
          firstElement?.focus();
        }
      };

      container.addEventListener('keydown', handleKeyDown);
      return () => container.removeEventListener('keydown', handleKeyDown);
    } else {
      // Restore focus when deactivating
      if (previousFocusRef.current && typeof previousFocusRef.current.focus === 'function') {
        previousFocusRef.current.focus();
        previousFocusRef.current = null;
      }
    }
  }, [isActive]);

  return containerRef;
}

/**
 * Hook to announce content changes to screen readers.
 *
 * @returns Function to announce a message
 *
 * @example
 * ```tsx
 * function SaveButton() {
 *   const announce = useScreenReaderAnnounce();
 *
 *   const handleSave = () => {
 *     // ... save logic
 *     announce('Document saved successfully');
 *   };
 *
 *   return <button onClick={handleSave}>Save</button>;
 * }
 * ```
 */
export function useScreenReaderAnnounce() {
  const announce = useCallback((message: string, priority: 'polite' | 'assertive' = 'polite') => {
    const announcement = document.createElement('div');
    announcement.setAttribute('role', 'status');
    announcement.setAttribute('aria-live', priority);
    announcement.setAttribute('aria-atomic', 'true');
    announcement.style.cssText = `
      position: absolute;
      width: 1px;
      height: 1px;
      padding: 0;
      margin: -1px;
      overflow: hidden;
      clip: rect(0, 0, 0, 0);
      white-space: nowrap;
      border: 0;
    `;

    document.body.appendChild(announcement);

    // Delay to ensure screen reader picks up the announcement
    setTimeout(() => {
      announcement.textContent = message;
    }, 100);

    // Clean up after announcement
    setTimeout(() => {
      document.body.removeChild(announcement);
    }, 1000);
  }, []);

  return announce;
}
