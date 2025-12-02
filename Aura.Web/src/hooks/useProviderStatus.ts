import { useEffect, useState, useCallback, useMemo } from 'react';

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

/**
 * Health level indicating overall provider availability
 * - healthy: All critical and important providers are available
 * - degraded: Some providers unavailable but core functionality works
 * - critical: Critical providers (LLM) unavailable
 */
export type ProviderHealthLevel = 'healthy' | 'degraded' | 'critical';

/**
 * Summary of provider health status for quick display
 */
export interface ProviderHealthSummary {
  level: ProviderHealthLevel;
  availableLlm: number;
  totalLlm: number;
  availableTts: number;
  totalTts: number;
  availableImages: number;
  totalImages: number;
  message: string;
}

export interface UseProviderStatusResult {
  llmProviders: ProviderStatus[];
  ttsProviders: ProviderStatus[];
  imageProviders: ProviderStatus[];
  isLoading: boolean;
  error: Error | null;
  refresh: () => Promise<void>;
  lastUpdated: Date | null;
  healthSummary: ProviderHealthSummary;
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
      // Note: Using current time as fallback for missing timestamps since the data was just fetched
      // This is preferable to using Date(0) which would show "January 1, 1970" in the UI
      const now = new Date();
      const processedData: ProviderStatusResponse = {
        ...data,
        llm: llmArray.map((p) => ({
          ...p,
          lastChecked: p?.lastChecked ? new Date(p.lastChecked) : now,
        })),
        tts: ttsArray.map((p) => ({
          ...p,
          lastChecked: p?.lastChecked ? new Date(p.lastChecked) : now,
        })),
        images: imagesArray.map((p) => ({
          ...p,
          lastChecked: p?.lastChecked ? new Date(p.lastChecked) : now,
        })),
        timestamp: data?.timestamp || now.toISOString(),
      };

      setStatus(processedData);
      setLastUpdated(now);
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

  // Calculate health summary based on provider availability
  const healthSummary = useMemo((): ProviderHealthSummary => {
    const llmProviders = status?.llm ?? [];
    const ttsProviders = status?.tts ?? [];
    const imageProviders = status?.images ?? [];

    const availableLlm = llmProviders.filter((p) => p.available).length;
    const totalLlm = llmProviders.length;
    const availableTts = ttsProviders.filter((p) => p.available).length;
    const totalTts = ttsProviders.length;
    const availableImages = imageProviders.filter((p) => p.available).length;
    const totalImages = imageProviders.length;

    // Determine health level based on provider categories:
    // - Critical: LLM providers (required for script generation)
    // - Important: TTS providers (required for narration)
    // - Optional: Image providers (graceful degradation)
    let level: ProviderHealthLevel;
    let message: string;

    if (availableLlm === 0 && totalLlm > 0) {
      // No LLM providers available - critical
      level = 'critical';
      message = 'No script generation providers available';
    } else if (availableTts === 0 && totalTts > 0) {
      // No TTS providers available - critical (required for video)
      level = 'critical';
      message = 'No voice providers available';
    } else if (availableLlm < totalLlm || availableTts < totalTts) {
      // Some critical/important providers unavailable
      level = 'degraded';
      const unavailable: string[] = [];
      if (availableLlm < totalLlm) unavailable.push('LLM');
      if (availableTts < totalTts) unavailable.push('TTS');
      if (availableImages < totalImages) unavailable.push('Image');
      message = `Some ${unavailable.join(', ')} providers unavailable`;
    } else if (availableImages < totalImages && totalImages > 0) {
      // Only image providers have issues - degraded but less severe
      level = 'degraded';
      message = 'Some image providers unavailable';
    } else if (totalLlm === 0 && totalTts === 0 && totalImages === 0) {
      // No providers configured at all
      level = 'degraded';
      message = 'No providers configured';
    } else {
      // All providers healthy
      level = 'healthy';
      message = 'All providers operational';
    }

    return {
      level,
      availableLlm,
      totalLlm,
      availableTts,
      totalTts,
      availableImages,
      totalImages,
      message,
    };
  }, [status]);

  return {
    llmProviders: status?.llm ?? [],
    ttsProviders: status?.tts ?? [],
    imageProviders: status?.images ?? [],
    isLoading,
    error,
    refresh: fetchStatus,
    lastUpdated,
    healthSummary,
  };
}
