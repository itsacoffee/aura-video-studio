/**
 * Health Monitoring Hook
 * Provides real-time health status with auto-retry on failures
 */

import { useState, useEffect, useCallback, useRef } from 'react';
import { getHealthDetails, getHealthReady, type HealthCheckResponse } from '../services/api/healthApi';

export interface UseHealthMonitoringOptions {
  /**
   * Polling interval in milliseconds
   * @default 30000 (30 seconds)
   */
  pollingInterval?: number;

  /**
   * Enable auto-retry on unhealthy status
   * @default true
   */
  enableAutoRetry?: boolean;

  /**
   * Maximum number of retry attempts
   * @default 3
   */
  maxRetries?: number;

  /**
   * Delay between retries in milliseconds
   * @default 5000 (5 seconds)
   */
  retryDelay?: number;

  /**
   * Use ready endpoint instead of full health endpoint
   * @default false
   */
  useReadyEndpoint?: boolean;

  /**
   * Auto-start monitoring on mount
   * @default true
   */
  autoStart?: boolean;
}

export interface HealthMonitoringState {
  health: HealthCheckResponse | null;
  loading: boolean;
  error: Error | null;
  retryCount: number;
  isMonitoring: boolean;
  lastUpdate: Date | null;
}

export function useHealthMonitoring(options: UseHealthMonitoringOptions = {}) {
  const {
    pollingInterval = 30000,
    enableAutoRetry = true,
    maxRetries = 3,
    retryDelay = 5000,
    useReadyEndpoint = false,
    autoStart = true,
  } = options;

  const [state, setState] = useState<HealthMonitoringState>({
    health: null,
    loading: true,
    error: null,
    retryCount: 0,
    isMonitoring: false,
    lastUpdate: null,
  });

  const intervalRef = useRef<number | null>(null);
  const retryTimeoutRef = useRef<number | null>(null);
  const isMountedRef = useRef(true);

  const fetchHealth = useCallback(async () => {
    if (!isMountedRef.current) return;

    try {
      const data = useReadyEndpoint
        ? await getHealthReady()
        : await getHealthDetails();

      if (!isMountedRef.current) return;

      setState((prev) => ({
        ...prev,
        health: data,
        loading: false,
        error: null,
        retryCount: 0,
        lastUpdate: new Date(),
      }));

      // Check if unhealthy and should retry
      if (enableAutoRetry && data.status === 'unhealthy' && state.retryCount < maxRetries) {
        scheduleRetry();
      }
    } catch (err) {
      if (!isMountedRef.current) return;

      const error = err instanceof Error ? err : new Error('Failed to fetch health data');
      setState((prev) => ({
        ...prev,
        loading: false,
        error,
        retryCount: prev.retryCount + 1,
        lastUpdate: new Date(),
      }));

      // Retry on error if enabled
      if (enableAutoRetry && state.retryCount < maxRetries) {
        scheduleRetry();
      }
    }
  }, [useReadyEndpoint, enableAutoRetry, maxRetries, state.retryCount]);

  const scheduleRetry = useCallback(() => {
    if (retryTimeoutRef.current) {
      clearTimeout(retryTimeoutRef.current);
    }

    retryTimeoutRef.current = window.setTimeout(() => {
      fetchHealth();
    }, retryDelay);
  }, [fetchHealth, retryDelay]);

  const startMonitoring = useCallback(() => {
    if (state.isMonitoring) return;

    setState((prev) => ({ ...prev, isMonitoring: true }));
    fetchHealth();

    intervalRef.current = window.setInterval(() => {
      fetchHealth();
    }, pollingInterval);
  }, [state.isMonitoring, fetchHealth, pollingInterval]);

  const stopMonitoring = useCallback(() => {
    if (intervalRef.current) {
      clearInterval(intervalRef.current);
      intervalRef.current = null;
    }

    if (retryTimeoutRef.current) {
      clearTimeout(retryTimeoutRef.current);
      retryTimeoutRef.current = null;
    }

    setState((prev) => ({ ...prev, isMonitoring: false }));
  }, []);

  const refresh = useCallback(() => {
    setState((prev) => ({ ...prev, loading: true, retryCount: 0 }));
    fetchHealth();
  }, [fetchHealth]);

  const resetRetries = useCallback(() => {
    setState((prev) => ({ ...prev, retryCount: 0 }));
  }, []);

  // Auto-start on mount if enabled
  useEffect(() => {
    if (autoStart) {
      startMonitoring();
    }

    return () => {
      isMountedRef.current = false;
      stopMonitoring();
    };
  }, [autoStart]); // eslint-disable-line react-hooks/exhaustive-deps

  return {
    ...state,
    startMonitoring,
    stopMonitoring,
    refresh,
    resetRetries,
  };
}
