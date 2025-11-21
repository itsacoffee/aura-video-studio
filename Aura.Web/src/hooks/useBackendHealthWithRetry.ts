import { useState, useCallback } from 'react';
import { HealthCheckService, type HealthCheckResult } from '../services/HealthCheckService';

export interface UseBackendHealthWithRetryOptions {
  maxRetries?: number;
  retryDelayMs?: number;
  timeoutMs?: number;
  exponentialBackoff?: boolean;
  backendUrl?: string;
  onHealthChange?: (isHealthy: boolean) => void;
}

export interface UseBackendHealthWithRetryReturn {
  isChecking: boolean;
  result: HealthCheckResult | null;
  retryCount: number;
  checkHealth: () => Promise<HealthCheckResult>;
  reset: () => void;
}

export const useBackendHealthWithRetry = (
  options: UseBackendHealthWithRetryOptions = {}
): UseBackendHealthWithRetryReturn => {
  const { onHealthChange, ...serviceOptions } = options;

  const [isChecking, setIsChecking] = useState(false);
  const [result, setResult] = useState<HealthCheckResult | null>(null);
  const [retryCount, setRetryCount] = useState(0);

  const checkHealth = useCallback(async (): Promise<HealthCheckResult> => {
    setIsChecking(true);
    setRetryCount(0);

    const service = new HealthCheckService(serviceOptions);

    const healthResult = await service.checkHealth((attempt, _maxAttempts) => {
      setRetryCount(attempt);
    });

    setResult(healthResult);
    setIsChecking(false);

    if (onHealthChange) {
      onHealthChange(healthResult.isHealthy);
    }

    return healthResult;
  }, [serviceOptions, onHealthChange]);

  const reset = useCallback(() => {
    setIsChecking(false);
    setResult(null);
    setRetryCount(0);
  }, []);

  return {
    isChecking,
    result,
    retryCount,
    checkHealth,
    reset,
  };
};
