/**
 * App Store
 * Global application state for UI, notifications, and app-wide settings
 */

import { create } from 'zustand';
import { persist, createJSONStorage } from 'zustand/middleware';

export interface Notification {
  id: string;
  type: 'success' | 'error' | 'warning' | 'info';
  title: string;
  message: string;
  timestamp: string;
  duration?: number;
  action?: {
    label: string;
    onClick: () => void;
  };
}

export interface AppSettings {
  theme: 'light' | 'dark' | 'auto';
  language: string;
  autoSave: boolean;
  autoSaveInterval: number; // in seconds
  enableNotifications: boolean;
  enableSounds: boolean;
  compactMode: boolean;
  developerMode: boolean;
}

export interface AppState {
  // UI State
  isSidebarOpen: boolean;
  isCommandPaletteOpen: boolean;
  activeModal: string | null;

  // Notifications
  notifications: Notification[];
  maxNotifications: number;

  // Settings
  settings: AppSettings;

  // Network status
  isOnline: boolean;

  // Loading states
  globalLoading: boolean;
  loadingMessage: string | null;

  // Actions - UI
  toggleSidebar: () => void;
  setSidebarOpen: (isOpen: boolean) => void;
  toggleCommandPalette: () => void;
  setCommandPaletteOpen: (isOpen: boolean) => void;
  openModal: (modalId: string) => void;
  closeModal: () => void;

  // Actions - Notifications
  addNotification: (notification: Omit<Notification, 'id' | 'timestamp'>) => string;
  removeNotification: (id: string) => void;
  clearNotifications: () => void;

  // Actions - Settings
  updateSettings: (updates: Partial<AppSettings>) => void;
  resetSettings: () => void;

  // Actions - Network
  setOnlineStatus: (isOnline: boolean) => void;

  // Actions - Loading
  setGlobalLoading: (isLoading: boolean, message?: string) => void;
}

const defaultSettings: AppSettings = {
  theme: 'auto',
  language: 'en-US',
  autoSave: true,
  autoSaveInterval: 30,
  enableNotifications: true,
  enableSounds: false,
  compactMode: false,
  developerMode: false,
};

export const useAppStore = create<AppState>()(
  persist(
    (set, get) => ({
      // Initial state
      isSidebarOpen: true,
      isCommandPaletteOpen: false,
      activeModal: null,

      notifications: [],
      maxNotifications: 5,

      settings: defaultSettings,

      isOnline: navigator.onLine,

      globalLoading: false,
      loadingMessage: null,

      // UI Actions
      toggleSidebar: () => {
        set((state) => ({ isSidebarOpen: !state.isSidebarOpen }));
      },

      setSidebarOpen: (isOpen) => {
        set({ isSidebarOpen: isOpen });
      },

      toggleCommandPalette: () => {
        set((state) => ({ isCommandPaletteOpen: !state.isCommandPaletteOpen }));
      },

      setCommandPaletteOpen: (isOpen) => {
        set({ isCommandPaletteOpen: isOpen });
      },

      openModal: (modalId) => {
        set({ activeModal: modalId });
      },

      closeModal: () => {
        set({ activeModal: null });
      },

      // Notification Actions
      addNotification: (notification) => {
        const id = crypto.randomUUID();
        const timestamp = new Date().toISOString();

        const newNotification: Notification = {
          ...notification,
          id,
          timestamp,
        };

        set((state) => {
          const notifications = [newNotification, ...state.notifications];

          // Keep only max notifications
          if (notifications.length > state.maxNotifications) {
            notifications.pop();
          }

          return { notifications };
        });

        // Auto-dismiss after duration
        if (notification.duration) {
          setTimeout(() => {
            get().removeNotification(id);
          }, notification.duration);
        }

        return id;
      },

      removeNotification: (id) => {
        set((state) => ({
          notifications: state.notifications.filter((n) => n.id !== id),
        }));
      },

      clearNotifications: () => {
        set({ notifications: [] });
      },

      // Settings Actions
      updateSettings: (updates) => {
        set((state) => ({
          settings: { ...state.settings, ...updates },
        }));
      },

      resetSettings: () => {
        set({ settings: defaultSettings });
      },

      // Network Actions
      setOnlineStatus: (isOnline) => {
        set({ isOnline });

        // Show notification when coming back online
        if (isOnline) {
          get().addNotification({
            type: 'success',
            title: 'Back Online',
            message: 'Your connection has been restored',
            duration: 3000,
          });
        } else {
          get().addNotification({
            type: 'warning',
            title: 'Connection Lost',
            message: 'You are currently offline',
            duration: 5000,
          });
        }
      },

      // Loading Actions
      setGlobalLoading: (isLoading, message) => {
        set({
          globalLoading: isLoading,
          loadingMessage: message || null,
        });
      },
    }),
    {
      name: 'app-store',
      storage: createJSONStorage(() => localStorage),
      partialize: (state) => ({
        isSidebarOpen: state.isSidebarOpen,
        settings: state.settings,
        // Don't persist notifications, modals, or loading states
      }),
    }
  )
);

// Listen to online/offline events
if (typeof window !== 'undefined') {
  window.addEventListener('online', () => {
    useAppStore.getState().setOnlineStatus(true);
  });

  window.addEventListener('offline', () => {
    useAppStore.getState().setOnlineStatus(false);
  });
}
