/**
 * Hook to check backend connection status and disable actions when offline
 */

import { useBackendHealth } from './useBackendHealth';
import { useConnectionStore } from '../stores/connectionStore';
import { useEffect } from 'react';

/**
 * Hook to check if backend is connected
 * Returns status and helper to determine if actions should be disabled
 */
export function useBackendConnection() {
  const { status: healthStatus } = useBackendHealth();
  const { status: connectionStatus, setStatus } = useConnectionStore();

  // Sync health status with connection store
  useEffect(() => {
    if (healthStatus === 'online' || healthStatus === 'offline') {
      setStatus(healthStatus);
    }
  }, [healthStatus, setStatus]);

  const isOnline = connectionStatus === 'online';
  const isOffline = connectionStatus === 'offline';

  return {
    isOnline,
    isOffline,
    status: connectionStatus,
    shouldDisableActions: isOffline,
  };
}

