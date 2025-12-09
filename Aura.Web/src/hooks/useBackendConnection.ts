/**
 * Hook to check backend connection status and disable actions when offline
 */

import { useEffect } from 'react';
import { useConnectionStore } from '../stores/connectionStore';
import { useBackendHealth } from './useBackendHealth';

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
