/**
 * Settings Store
 * Persisted store for operating mode and user settings
 */

import { create } from 'zustand';
import { persist, createJSONStorage } from 'zustand/middleware';

export type OperatingMode = 'online' | 'offline';

export interface OperatingModeInfo {
  mode: OperatingMode;
  isOffline: boolean;
  allowedLlmProviders: string[];
  allowedTtsProviders: string[];
  allowedImageProviders: string[];
}

export interface SettingsStoreState {
  // Operating mode
  operatingMode: OperatingMode;
  isOfflineMode: boolean;

  // Offline provider lists (cached from backend)
  allowedLlmProviders: string[];
  allowedTtsProviders: string[];
  allowedImageProviders: string[];

  // Loading state
  isLoading: boolean;
  error: string | null;

  // Last sync timestamp
  lastSyncedAt: string | null;

  // Actions
  setOperatingMode: (mode: OperatingMode) => void;
  toggleOfflineMode: () => void;
  syncWithBackend: () => Promise<void>;
  updateFromBackend: (info: OperatingModeInfo) => void;
  setLoading: (isLoading: boolean) => void;
  setError: (error: string | null) => void;
}

// Default offline providers (fallback if backend unavailable)
const DEFAULT_OFFLINE_LLM_PROVIDERS = ['Ollama', 'RuleBased'];
const DEFAULT_OFFLINE_TTS_PROVIDERS = ['Windows', 'WindowsSAPI', 'Piper', 'Mimic3'];
const DEFAULT_OFFLINE_IMAGE_PROVIDERS = ['Placeholder'];

export const useSettingsStore = create<SettingsStoreState>()(
  persist(
    (set, get) => ({
      // Initial state
      operatingMode: 'online',
      isOfflineMode: false,

      allowedLlmProviders: DEFAULT_OFFLINE_LLM_PROVIDERS,
      allowedTtsProviders: DEFAULT_OFFLINE_TTS_PROVIDERS,
      allowedImageProviders: DEFAULT_OFFLINE_IMAGE_PROVIDERS,

      isLoading: false,
      error: null,
      lastSyncedAt: null,

      // Actions
      setOperatingMode: (mode: OperatingMode) => {
        const isOffline = mode === 'offline';
        set({
          operatingMode: mode,
          isOfflineMode: isOffline,
        });

        // Sync with backend
        get().syncWithBackend();
      },

      toggleOfflineMode: () => {
        const newMode = get().isOfflineMode ? 'online' : 'offline';
        get().setOperatingMode(newMode);
      },

      syncWithBackend: async () => {
        const { isOfflineMode } = get();

        set({ isLoading: true, error: null });

        try {
          const response = await fetch('/api/settings/operating-mode', {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ isOffline: isOfflineMode }),
          });

          if (!response.ok) {
            throw new Error(`Failed to sync: ${response.statusText}`);
          }

          set({
            lastSyncedAt: new Date().toISOString(),
            isLoading: false,
          });
        } catch (error: unknown) {
          const errorMessage = error instanceof Error ? error.message : 'Unknown error';
          console.error('Failed to sync operating mode with backend:', errorMessage);
          set({
            error: errorMessage,
            isLoading: false,
          });
        }
      },

      updateFromBackend: (info: OperatingModeInfo) => {
        set({
          operatingMode: info.mode,
          isOfflineMode: info.isOffline,
          allowedLlmProviders:
            info.allowedLlmProviders.length > 0
              ? info.allowedLlmProviders
              : DEFAULT_OFFLINE_LLM_PROVIDERS,
          allowedTtsProviders:
            info.allowedTtsProviders.length > 0
              ? info.allowedTtsProviders
              : DEFAULT_OFFLINE_TTS_PROVIDERS,
          allowedImageProviders:
            info.allowedImageProviders.length > 0
              ? info.allowedImageProviders
              : DEFAULT_OFFLINE_IMAGE_PROVIDERS,
          lastSyncedAt: new Date().toISOString(),
        });
      },

      setLoading: (isLoading: boolean) => set({ isLoading }),
      setError: (error: string | null) => set({ error }),
    }),
    {
      name: 'aura-settings-store',
      storage: createJSONStorage(() => localStorage),
      partialize: (state) => ({
        operatingMode: state.operatingMode,
        isOfflineMode: state.isOfflineMode,
        lastSyncedAt: state.lastSyncedAt,
      }),
    }
  )
);

/**
 * Load operating mode from backend on app startup
 */
export async function loadOperatingModeFromBackend(): Promise<void> {
  const store = useSettingsStore.getState();
  store.setLoading(true);

  try {
    const response = await fetch('/api/settings/operating-mode');

    if (response.ok) {
      const info: OperatingModeInfo = await response.json();
      store.updateFromBackend(info);
    }
  } catch (error: unknown) {
    const errorMessage = error instanceof Error ? error.message : 'Unknown error';
    console.warn('Failed to load operating mode from backend:', errorMessage);
    // Keep local state if backend is unavailable
  } finally {
    store.setLoading(false);
  }
}

/**
 * Check if a provider is allowed in the current operating mode
 */
export function isProviderAllowed(
  providerName: string,
  providerType: 'llm' | 'tts' | 'images'
): boolean {
  const { isOfflineMode, allowedLlmProviders, allowedTtsProviders, allowedImageProviders } =
    useSettingsStore.getState();

  // In online mode, all providers are allowed
  if (!isOfflineMode) {
    return true;
  }

  // In offline mode, check against allowed lists
  const normalizedName = providerName?.toLowerCase() ?? '';

  switch (providerType) {
    case 'llm':
      return allowedLlmProviders.some((p) => p.toLowerCase() === normalizedName);
    case 'tts':
      return allowedTtsProviders.some((p) => p.toLowerCase() === normalizedName);
    case 'images':
      return allowedImageProviders.some((p) => p.toLowerCase() === normalizedName);
    default:
      return false;
  }
}

/**
 * Filter a list of providers based on the current operating mode
 */
export function filterProvidersByMode<T extends { value: string; isLocal?: boolean }>(
  providers: readonly T[],
  providerType: 'llm' | 'tts' | 'images'
): T[] {
  const { isOfflineMode } = useSettingsStore.getState();

  if (!isOfflineMode) {
    return [...providers];
  }

  return providers.filter((provider) => {
    // Always include local providers in offline mode
    if (provider.isLocal) {
      return true;
    }

    // Check against explicit allowed lists
    return isProviderAllowed(provider.value, providerType);
  });
}
