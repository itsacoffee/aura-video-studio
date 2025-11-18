/**
 * React Hook for Configuration Status
 *
 * Provides easy access to configuration status throughout the application
 */

import { useEffect, useState, useCallback } from 'react';
import { configurationStatusService } from '../services/configurationStatusService';
import type {
  ConfigurationStatus,
  SystemCheckResult,
} from '../services/configurationStatusService';

export interface UseConfigurationStatusOptions {
  autoRefresh?: boolean;
  refreshInterval?: number;
}

export interface UseConfigurationStatusReturn {
  status: ConfigurationStatus | null;
  loading: boolean;
  error: Error | null;
  isConfigured: boolean;
  needsSetup: boolean;
  refresh: () => Promise<void>;
  runSystemChecks: () => Promise<SystemCheckResult | null>;
  testProviders: () => Promise<Record<
    string,
    { success: boolean; message: string; responseTimeMs: number }
  > | null>;
  markConfigured: () => Promise<void>;
}

export function useConfigurationStatus(
  options: UseConfigurationStatusOptions = {}
): UseConfigurationStatusReturn {
  const { autoRefresh = false, refreshInterval = 60000 } = options;

  const [status, setStatus] = useState<ConfigurationStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);

  const refresh = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const newStatus = await configurationStatusService.getStatus(true);
      setStatus(newStatus);
    } catch (err) {
      const error = err instanceof Error ? err : new Error(String(err));
      setError(error);
      console.error('Failed to refresh configuration status:', error);
    } finally {
      setLoading(false);
    }
  }, []);

  const runSystemChecks = useCallback(async (): Promise<SystemCheckResult | null> => {
    try {
      return await configurationStatusService.runSystemChecks();
    } catch (err) {
      console.error('Failed to run system checks:', err);
      return null;
    }
  }, []);

  const testProviders = useCallback(async () => {
    try {
      return await configurationStatusService.testProviders();
    } catch (err) {
      console.error('Failed to test providers:', err);
      return null;
    }
  }, []);

  const markConfigured = useCallback(async () => {
    try {
      await configurationStatusService.markConfigured();
      await refresh();
    } catch (err) {
      console.error('Failed to mark configuration as complete:', err);
    }
  }, [refresh]);

  // Initial load
  useEffect(() => {
    refresh();
  }, [refresh]);

  // Subscribe to status changes
  useEffect(() => {
    const unsubscribe = configurationStatusService.subscribe(setStatus);
    return () => unsubscribe();
  }, []);

  // Auto-refresh
  useEffect(() => {
    if (!autoRefresh) return;

    const interval = setInterval(() => {
      refresh();
    }, refreshInterval);

    return () => clearInterval(interval);
  }, [autoRefresh, refreshInterval, refresh]);

  return {
    status,
    loading,
    error,
    isConfigured: status?.isConfigured ?? false,
    needsSetup: !loading && !status?.isConfigured,
    refresh,
    runSystemChecks,
    testProviders,
    markConfigured,
  };
}
