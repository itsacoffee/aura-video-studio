/**
 * Hook for managing provider fallback
 */

import { useCallback, useEffect, useState } from 'react';
import { loggingService } from '../services/loggingService';
import { providerFallbackService, ProviderConfig } from '../services/providerFallbackService';

export interface UseProviderFallbackReturn {
  currentProvider: ProviderConfig | null;
  availableProviders: ProviderConfig[];
  isProviderHealthy: boolean;
  fallbackToNext: () => Promise<ProviderConfig | null>;
  resetChain: () => void;
  executeWithFallback: <T>(operation: (provider: ProviderConfig) => Promise<T>) => Promise<T>;
}

/**
 * Hook for managing provider fallback
 */
export function useProviderFallback(
  type: 'llm' | 'tts' | 'image' | 'video'
): UseProviderFallbackReturn {
  const [currentProvider, setCurrentProvider] = useState<ProviderConfig | null>(null);
  const [availableProviders, setAvailableProviders] = useState<ProviderConfig[]>([]);
  const [isHealthy, setIsHealthy] = useState(false);

  useEffect(() => {
    const provider = providerFallbackService.getCurrentProvider(type);
    setCurrentProvider(provider);

    const providers = providerFallbackService.getProviders(type);
    setAvailableProviders(providers);

    if (provider) {
      providerFallbackService.checkProviderHealth(provider).then(setIsHealthy);
    }
  }, [type]);

  const fallbackToNext = useCallback(async () => {
    loggingService.info(`Attempting fallback for ${type}`, 'useProviderFallback', 'fallbackToNext');

    const nextProvider = await providerFallbackService.fallbackToNextProvider(type);

    if (nextProvider) {
      setCurrentProvider(nextProvider);
      const healthy = await providerFallbackService.checkProviderHealth(nextProvider);
      setIsHealthy(healthy);
    }

    return nextProvider;
  }, [type]);

  const resetChain = useCallback(() => {
    providerFallbackService.resetFallbackChain(type);
    const provider = providerFallbackService.getCurrentProvider(type);
    setCurrentProvider(provider);

    if (provider) {
      providerFallbackService.checkProviderHealth(provider).then(setIsHealthy);
    }

    loggingService.info(`Provider chain reset for ${type}`, 'useProviderFallback', 'resetChain');
  }, [type]);

  const executeWithFallback = useCallback(
    async <T>(operation: (provider: ProviderConfig) => Promise<T>): Promise<T> => {
      return providerFallbackService.executeWithFallback(type, operation);
    },
    [type]
  );

  return {
    currentProvider,
    availableProviders,
    isProviderHealthy: isHealthy,
    fallbackToNext,
    resetChain,
    executeWithFallback,
  };
}
