/**
 * Custom hook for managing focus traps in modals and dialogs
 *
 * Ensures keyboard focus stays within a container (e.g., modal)
 * and returns to the previously focused element when deactivated.
 */

import React, { useEffect, useRef } from 'react';
import { FocusTrap, createFocusTrap } from '../utils/focusManagement';

interface UseFocusTrapOptions {
  /**
   * Whether the focus trap is active
   */
  isActive: boolean;

  /**
   * Optional callback when focus trap is activated
   */
  onActivate?: () => void;

  /**
   * Optional callback when focus trap is deactivated
   */
  onDeactivate?: () => void;
}

/**
 * Hook to create a focus trap for a container element
 *
 * @param options - Configuration options
 * @returns Ref to attach to the container element
 *
 * @example
 * ```tsx
 * function Modal({ isOpen, onClose }) {
 *   const focusTrapRef = useFocusTrap({ isActive: isOpen });
 *
 *   return (
 *     <div ref={focusTrapRef} role="dialog">
 *       <h2>Modal Title</h2>
 *       <button onClick={onClose}>Close</button>
 *     </div>
 *   );
 * }
 * ```
 */
export function useFocusTrap<T extends HTMLElement = HTMLDivElement>(
  options: UseFocusTrapOptions
): React.RefObject<T> {
  const { isActive, onActivate, onDeactivate } = options;
  const containerRef = useRef<T>(null);
  const focusTrapRef = useRef<FocusTrap | null>(null);

  useEffect(() => {
    if (!containerRef.current) return;

    if (isActive) {
      // Create and activate focus trap
      focusTrapRef.current = createFocusTrap(containerRef.current);
      onActivate?.();
    } else {
      // Deactivate and cleanup focus trap
      if (focusTrapRef.current) {
        focusTrapRef.current.deactivate();
        focusTrapRef.current = null;
        onDeactivate?.();
      }
    }

    // Cleanup on unmount
    return () => {
      if (focusTrapRef.current) {
        focusTrapRef.current.deactivate();
        focusTrapRef.current = null;
      }
    };
  }, [isActive, onActivate, onDeactivate]);

  return containerRef;
}
