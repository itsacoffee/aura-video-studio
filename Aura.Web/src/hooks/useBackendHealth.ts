import { useCallback, useEffect, useState } from 'react';
import { apiUrl } from '@/config/api';
import { loggingService } from '@/services/loggingService';
import type { DesktopBridgeDiagnostics } from '@/types/ambient/window';

export type BackendHealthStatus = 'online' | 'offline';

interface DatabaseMigrationStatus {
  current?: string | null;
  latest?: string | null;
  pending?: number;
  isUpToDate?: boolean;
}

interface DatabaseHealthSnapshot {
  connected?: boolean;
  provider?: string;
  dataSource?: string | null;
  responseTimeMs?: number | null;
  error?: string | null;
  migration?: DatabaseMigrationStatus;
}

interface ApiHealthResponse {
  status: string;
  version?: string;
  environment?: string;
  machineName?: string;
  osPlatform?: string;
  osVersion?: string;
  architecture?: string;
  timestamp: string;
  database?: DatabaseHealthSnapshot;
}

export interface BackendHealthSnapshot {
  status: BackendHealthStatus;
  diagnostics: ApiHealthResponse | null;
  bridge: DesktopBridgeDiagnostics | null;
  error?: string | null;
  lastChecked: Date | null;
}

export function useBackendHealth(pollIntervalMs = 15000) {
  const getCachedDiagnostics = () =>
    typeof window !== 'undefined'
      ? window.aura?.runtime?.getCachedDiagnostics?.() ??
        window.desktopBridge?.getCachedDiagnostics?.() ??
        null
      : null;

  const [snapshot, setSnapshot] = useState<BackendHealthSnapshot>({
    status: 'offline',
    diagnostics: null,
    bridge: getCachedDiagnostics(),
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

      const diagnostics = (await response.json()) as ApiHealthResponse;
      const desktopDiagnostics = getCachedDiagnostics();

      setSnapshot({
        status: 'online',
        diagnostics,
        bridge: desktopDiagnostics,
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
        bridge: prev.bridge,
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

