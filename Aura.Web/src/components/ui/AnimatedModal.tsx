import { Dismiss24Regular } from '@fluentui/react-icons';
import { motion, AnimatePresence } from 'framer-motion';
import { ReactNode, useEffect } from 'react';
import { useReducedMotion } from '../../hooks/useReducedMotion';
import { backdropVariants, scaleVariants } from '../../utils/animations';

interface AnimatedModalProps {
  isOpen: boolean;
  onClose: () => void;
  title?: string;
  children: ReactNode;
  size?: 'sm' | 'md' | 'lg' | 'xl' | 'full';
  showCloseButton?: boolean;
  closeOnBackdropClick?: boolean;
  closeOnEscape?: boolean;
}

/**
 * Animated modal with smooth enter/exit transitions
 * Supports keyboard navigation and accessibility
 */
export function AnimatedModal({
  isOpen,
  onClose,
  title,
  children,
  size = 'md',
  showCloseButton = true,
  closeOnBackdropClick = true,
  closeOnEscape = true,
}: AnimatedModalProps) {
  const prefersReducedMotion = useReducedMotion();

  const sizeStyles = {
    sm: 'max-w-sm',
    md: 'max-w-md',
    lg: 'max-w-lg',
    xl: 'max-w-xl',
    full: 'max-w-7xl w-full',
  };

  // Handle escape key
  useEffect(() => {
    if (!isOpen || !closeOnEscape) return;

    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        onClose();
      }
    };

    document.addEventListener('keydown', handleEscape);
    return () => document.removeEventListener('keydown', handleEscape);
  }, [isOpen, closeOnEscape, onClose]);

  // Prevent body scroll when modal is open
  useEffect(() => {
    if (isOpen) {
      document.body.style.overflow = 'hidden';
    } else {
      document.body.style.overflow = 'unset';
    }

    return () => {
      document.body.style.overflow = 'unset';
    };
  }, [isOpen]);

  return (
    <AnimatePresence>
      {isOpen && (
        <>
          {/* Backdrop */}
          <motion.div
            className="fixed inset-0 bg-black/50 backdrop-blur-sm z-40"
            variants={prefersReducedMotion ? undefined : backdropVariants}
            initial="hidden"
            animate="visible"
            exit="exit"
            onClick={closeOnBackdropClick ? onClose : undefined}
            aria-hidden="true"
          />

          {/* Modal */}
          <div className="fixed inset-0 z-50 flex items-center justify-center p-4 pointer-events-none">
            <motion.div
              className={`${sizeStyles[size]} w-full bg-white dark:bg-gray-800 rounded-2xl shadow-2xl pointer-events-auto max-h-[90vh] flex flex-col`}
              variants={prefersReducedMotion ? undefined : scaleVariants}
              initial="hidden"
              animate="visible"
              exit="exit"
              role="dialog"
              aria-modal="true"
              aria-labelledby={title ? 'modal-title' : undefined}
            >
              {/* Header */}
              {(title || showCloseButton) && (
                <div className="flex items-center justify-between px-6 py-4 border-b border-gray-200 dark:border-gray-700">
                  {title && (
                    <h2
                      id="modal-title"
                      className="text-xl font-semibold text-gray-900 dark:text-gray-100"
                    >
                      {title}
                    </h2>
                  )}
                  {showCloseButton && (
                    <button
                      onClick={onClose}
                      className="ml-auto p-2 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500"
                      aria-label="Close modal"
                    >
                      <Dismiss24Regular />
                    </button>
                  )}
                </div>
              )}

              {/* Body */}
              <div className="flex-1 overflow-y-auto px-6 py-4">{children}</div>
            </motion.div>
          </div>
        </>
      )}
    </AnimatePresence>
  );
}

interface AnimatedModalFooterProps {
  children: ReactNode;
  className?: string;
}

export function AnimatedModalFooter({ children, className = '' }: AnimatedModalFooterProps) {
  return (
    <div
      className={`flex items-center justify-end gap-3 px-6 py-4 border-t border-gray-200 dark:border-gray-700 ${className}`}
    >
      {children}
    </div>
  );
}
