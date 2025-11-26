import { useEffect, useState, useCallback } from 'react';

export interface ProviderStatus {
  name: string;
  available: boolean;
  tier: 'free' | 'local' | 'paid' | 'unknown';
  lastChecked: Date;
  errorMessage?: string;
  details?: string;
  howToFix?: string[];
}

export interface ProviderStatusResponse {
  llm: ProviderStatus[];
  tts: ProviderStatus[];
  images: ProviderStatus[];
  timestamp: string;
}

export interface UseProviderStatusResult {
  llmProviders: ProviderStatus[];
  ttsProviders: ProviderStatus[];
  imageProviders: ProviderStatus[];
  isLoading: boolean;
  error: Error | null;
  refresh: () => Promise<void>;
  lastUpdated: Date | null;
}

/**
 * Hook for polling and managing provider status
 * Polls every 15 seconds for real-time updates
 */
export function useProviderStatus(pollInterval: number = 15000): UseProviderStatusResult {
  const [status, setStatus] = useState<ProviderStatusResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null);

  const fetchStatus = useCallback(async () => {
    try {
      setIsLoading(true);
      setError(null);

      const response = await fetch('/api/providers/status');
      
      if (!response.ok) {
        throw new Error(`Failed to fetch provider status: ${response.statusText}`);
      }

      const data: ProviderStatusResponse = await response.json();
      
      // CRITICAL: Defensive coding to prevent crashes from malformed responses
      // If any of the arrays are null/undefined, use empty arrays as fallback
      const llmArray = Array.isArray(data?.llm) ? data.llm : [];
      const ttsArray = Array.isArray(data?.tts) ? data.tts : [];
      const imagesArray = Array.isArray(data?.images) ? data.images : [];
      
      // Convert timestamp strings to Date objects safely
      const processedData: ProviderStatusResponse = {
        ...data,
        llm: llmArray.map(p => ({
          ...p,
          lastChecked: p?.lastChecked ? new Date(p.lastChecked) : new Date(),
        })),
        tts: ttsArray.map(p => ({
          ...p,
          lastChecked: p?.lastChecked ? new Date(p.lastChecked) : new Date(),
        })),
        images: imagesArray.map(p => ({
          ...p,
          lastChecked: p?.lastChecked ? new Date(p.lastChecked) : new Date(),
        })),
        timestamp: data?.timestamp || new Date().toISOString(),
      };

      setStatus(processedData);
      setLastUpdated(new Date());
    } catch (err) {
      const error = err instanceof Error ? err : new Error(String(err));
      setError(error);
      console.error('Error fetching provider status:', error);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    // Initial fetch
    void fetchStatus();

    // Set up polling interval
    const interval = setInterval(() => {
      void fetchStatus();
    }, pollInterval);

    return () => {
      clearInterval(interval);
    };
  }, [fetchStatus, pollInterval]);

  return {
    llmProviders: status?.llm ?? [],
    ttsProviders: status?.tts ?? [],
    imageProviders: status?.images ?? [],
    isLoading,
    error,
    refresh: fetchStatus,
    lastUpdated,
  };
}

