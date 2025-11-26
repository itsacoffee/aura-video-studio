/**
 * Global connection state store
 * Tracks backend connection status and provides reactive state management
 */

import { create } from 'zustand';

export type ConnectionStatus = 'online' | 'offline' | 'checking';

interface ConnectionStore {
  status: ConnectionStatus;
  lastError: string | null;
  setStatus: (status: ConnectionStatus) => void;
  setError: (error: string | null) => void;
  isOnline: () => boolean;
}

/**
 * Global connection state store
 * Use this to check if backend is available before making API calls
 */
export const useConnectionStore = create<ConnectionStore>((set, get) => ({
  status: 'checking',
  lastError: null,
  setStatus: (status) => set({ status }),
  setError: (error) => set({ lastError: error }),
  isOnline: () => get().status === 'online',
}));
