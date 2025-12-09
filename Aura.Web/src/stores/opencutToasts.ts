/**
 * OpenCut Toast Store
 *
 * Zustand store for managing toast notifications in the OpenCut editor.
 * Provides a simple API for showing success, error, warning, and info toasts.
 */

import { create } from 'zustand';

/** Toast notification types */
export type ToastType = 'success' | 'error' | 'warning' | 'info';

/** Toast notification data */
export interface Toast {
  /** Unique identifier for the toast */
  id: string;
  /** Type determines the icon and color scheme */
  type: ToastType;
  /** Title text displayed prominently */
  title: string;
  /** Optional message with additional details */
  message?: string;
  /** Auto-dismiss duration in milliseconds (0 for persistent) */
  duration?: number;
  /** Optional action button */
  action?: {
    label: string;
    onClick: () => void;
  };
}

interface ToastsState {
  /** Array of currently visible toasts */
  toasts: Toast[];
}

interface ToastsActions {
  /**
   * Add a new toast notification
   * @returns The ID of the created toast
   */
  addToast: (toast: Omit<Toast, 'id'>) => string;

  /**
   * Remove a toast by ID
   */
  removeToast: (id: string) => void;

  /**
   * Clear all toasts
   */
  clearToasts: () => void;

  /**
   * Show a success toast
   * @returns The ID of the created toast
   */
  success: (title: string, message?: string) => string;

  /**
   * Show an error toast
   * @returns The ID of the created toast
   */
  error: (title: string, message?: string) => string;

  /**
   * Show a warning toast
   * @returns The ID of the created toast
   */
  warning: (title: string, message?: string) => string;

  /**
   * Show an info toast
   * @returns The ID of the created toast
   */
  info: (title: string, message?: string) => string;
}

/**
 * Generate a unique toast ID
 */
function generateToastId(): string {
  return `toast-${Date.now()}-${Math.random().toString(36).substring(2, 9)}`;
}

/**
 * Default toast duration in milliseconds
 */
const DEFAULT_TOAST_DURATION = 5000;

/**
 * Maximum number of toasts to display at once
 */
const MAX_TOASTS = 5;

/**
 * OpenCut toast notifications store
 */
export const useOpenCutToastsStore = create<ToastsState & ToastsActions>((set, get) => ({
  toasts: [],

  addToast: (toast) => {
    const id = generateToastId();
    const duration = toast.duration ?? DEFAULT_TOAST_DURATION;
    const newToast: Toast = { ...toast, id, duration };

    set((state) => {
      // Keep only the most recent toasts up to the max limit
      const updatedToasts = [...state.toasts, newToast].slice(-MAX_TOASTS);
      return { toasts: updatedToasts };
    });

    // Auto-dismiss if duration is set
    if (duration > 0) {
      setTimeout(() => {
        get().removeToast(id);
      }, duration);
    }

    return id;
  },

  removeToast: (id) => {
    set((state) => ({
      toasts: state.toasts.filter((t) => t.id !== id),
    }));
  },

  clearToasts: () => {
    set({ toasts: [] });
  },

  success: (title, message) => {
    return get().addToast({ type: 'success', title, message });
  },

  error: (title, message) => {
    return get().addToast({ type: 'error', title, message });
  },

  warning: (title, message) => {
    return get().addToast({ type: 'warning', title, message });
  },

  info: (title, message) => {
    return get().addToast({ type: 'info', title, message });
  },
}));

/**
 * Hook alias for convenience
 */
export const useToasts = useOpenCutToastsStore;
