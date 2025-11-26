/**
 * Hook to disable buttons/actions when backend is offline
 * Usage: const disabled = useDisableWhenOffline()
 */

import { useBackendConnection } from './useBackendConnection';

/**
 * Hook that returns true when backend is offline, causing buttons to be disabled
 * This prevents users from clicking actions that will fail
 */
export function useDisableWhenOffline(): boolean {
  const { shouldDisableActions } = useBackendConnection();
  return shouldDisableActions;
}

