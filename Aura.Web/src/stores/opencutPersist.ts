import { createJSONStorage } from 'zustand/middleware';

/**
 * Wraps createJSONStorage with defensive guards so corrupted or unreadable
 * localStorage entries don't crash OpenCut routes. Falls back to clearing the
 * bad entry and rehydrating with defaults.
 */
export function createSafeJSONStorage<T>(storageKey: string) {
  const storage = createJSONStorage<T>(() => localStorage);

  return {
    getItem: (name: string) => {
      try {
        return storage.getItem(name);
      } catch (error) {
        console.warn(`[OpenCut] Resetting corrupted persisted state for ${storageKey}`, error);
        try {
          localStorage.removeItem(name);
        } catch {
          // ignore
        }
        return null;
      }
    },
    setItem: (name: string, value: unknown) => {
      try {
        return storage.setItem(name, value as never);
      } catch (error) {
        console.warn(`[OpenCut] Failed to persist state for ${storageKey}`, error);
      }
    },
    removeItem: (name: string) => {
      try {
        return storage.removeItem(name);
      } catch (error) {
        console.warn(`[OpenCut] Failed to remove persisted state for ${storageKey}`, error);
      }
    },
  };
}
