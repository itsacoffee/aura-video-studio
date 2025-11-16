import { useCallback, useEffect, useState } from 'react';
import { apiUrl } from '@/config/api';
import { loggingService } from '@/services/loggingService';

export type BackendHealthStatus = 'online' | 'offline';

export interface BackendHealthSnapshot {
  status: BackendHealthStatus;
  diagnostics: Record<string, unknown> | null;
  error?: string | null;
  lastChecked: Date | null;
}

export function useBackendHealth(pollIntervalMs = 15000) {
  const [snapshot, setSnapshot] = useState<BackendHealthSnapshot>({
    status: 'offline',
    diagnostics: null,
    error: null,
    lastChecked: null
  });

  const checkHealth = useCallback(async () => {
    const controller = new AbortController();

    try {
      const response = await fetch(apiUrl('/api/health'), {
        headers: {
          'Cache-Control': 'no-cache'
        },
        signal: controller.signal
      });

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }

      const diagnostics = await response.json();
      setSnapshot({
        status: 'online',
        diagnostics,
        error: null,
        lastChecked: new Date()
      });
    } catch (error) {
      const err = error instanceof Error ? error : new Error(String(error));
      loggingService.warn('Backend health check failed', 'useBackendHealth', 'poll', {
        message: err.message
      });

      setSnapshot((prev) => ({
        status: 'offline',
        diagnostics: prev.diagnostics,
        error: err.message,
        lastChecked: new Date()
      }));
    }

    return () => controller.abort();
  }, []);

  useEffect(() => {
    let isMounted = true;
    let cleanup: (() => void) | undefined;

    const run = async () => {
      cleanup = await checkHealth();
    };

    run();
    const interval = window.setInterval(run, pollIntervalMs);

    return () => {
      if (!isMounted) {
        return;
      }
      if (cleanup) {
        cleanup();
      }
      window.clearInterval(interval);
      isMounted = false;
    };
  }, [checkHealth, pollIntervalMs]);

  return {
    ...snapshot,
    refresh: checkHealth
  };
}

