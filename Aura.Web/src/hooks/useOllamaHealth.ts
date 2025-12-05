import { useState, useEffect, useCallback } from 'react';

interface OllamaHealthStatus {
  isHealthy: boolean;
  version: string | null;
  availableModels: string[];
  runningModels: string[];
  baseUrl: string;
  responseTimeMs: number;
  errorMessage: string | null;
  lastChecked: string;
}

interface UseOllamaHealthResult {
  status: OllamaHealthStatus | null;
  isLoading: boolean;
  error: Error | null;
  refresh: () => Promise<void>;
}

/**
 * Hook for monitoring Ollama health status with configurable polling
 * @param pollingIntervalMs Interval between health checks in milliseconds (default: 30000ms)
 *                          Set to 0 to disable polling
 */
export function useOllamaHealth(pollingIntervalMs: number = 30000): UseOllamaHealthResult {
  const [status, setStatus] = useState<OllamaHealthStatus | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);

  const fetchHealth = useCallback(async () => {
    try {
      setIsLoading(true);
      const response = await fetch('/api/ollama/health');

      if (!response.ok) {
        throw new Error('Failed to fetch Ollama health: ' + response.status);
      }

      const data = await response.json();
      setStatus(data);
      setError(null);
    } catch (err: unknown) {
      const errorObj = err instanceof Error ? err : new Error(String(err));
      setError(errorObj);
      setStatus({
        isHealthy: false,
        version: null,
        availableModels: [],
        runningModels: [],
        baseUrl: '',
        responseTimeMs: 0,
        errorMessage: errorObj.message,
        lastChecked: new Date().toISOString(),
      });
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchHealth();

    if (pollingIntervalMs > 0) {
      const interval = setInterval(fetchHealth, pollingIntervalMs);
      return () => clearInterval(interval);
    }
  }, [fetchHealth, pollingIntervalMs]);

  return {
    status,
    isLoading,
    error,
    refresh: fetchHealth,
  };
}

export default useOllamaHealth;
