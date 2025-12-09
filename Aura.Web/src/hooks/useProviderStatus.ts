import { useEffect, useState, useCallback, useMemo, useRef } from 'react';

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
  overallHealth: 'Green' | 'Yellow' | 'Red';
  ollamaActive: boolean;
  hasAnyLlm: boolean;
  hasAnyTts: boolean;
  hasAnyImageProvider: boolean;
}

/** Maximum number of retry attempts for failed fetch requests */
const MAX_RETRIES = 3;

/** Base delay in milliseconds for exponential backoff between retries */
const RETRY_DELAY_MS = 2000;

/**
 * Backend DTO for detailed provider status
 */
interface BackendProviderStatusDto {
  name: string;
  category: string;
  isAvailable: boolean;
  isOnline: boolean;
  tier: string;
  features: string[];
  message: string;
}

/**
 * Backend DTO for category health
 */
interface BackendCategoryHealthDto {
  category: string;
  required: boolean;
  configuredCount: number;
  healthyCount: number;
  activeProviders: string[];
  message: string;
}

/**
 * Backend DTO for system provider status response
 */
interface BackendSystemProviderStatusDto {
  isOfflineMode: boolean;
  providers: BackendProviderStatusDto[];
  onlineProvidersCount: number;
  offlineProvidersCount: number;
  availableFeatures: string[];
  degradedFeatures: string[];
  lastUpdated: string;
  message: string;
  overallHealth: 'Green' | 'Yellow' | 'Red' | 'Healthy' | 'Degraded' | 'Unhealthy' | 'Unknown';
  categoryHealth: BackendCategoryHealthDto[];
  ollamaActive: boolean;
  hasAnyLlm: boolean;
  hasAnyTts: boolean;
  hasAnyImageProvider: boolean;
}

/**
 * Transforms backend tier string to frontend tier type
 */
function mapTier(tier: string): 'free' | 'local' | 'paid' | 'unknown' {
  const lowerTier = tier.toLowerCase();
  if (lowerTier === 'free') return 'free';
  if (lowerTier === 'local') return 'local';
  if (lowerTier === 'paid' || lowerTier === 'premium' || lowerTier === 'pro') return 'paid';
  return 'unknown';
}

/**
 * Maps backend category strings to normalized category keys
 */
function normalizeCategory(category: string): 'llm' | 'tts' | 'images' | 'other' {
  const lowerCategory = category.toLowerCase();
  if (lowerCategory === 'llm') return 'llm';
  if (lowerCategory === 'tts') return 'tts';
  if (lowerCategory === 'image' || lowerCategory === 'images') return 'images';
  return 'other';
}

/**
 * Transforms backend system provider status to frontend format
 */
function transformBackendResponse(
  backendData: BackendSystemProviderStatusDto
): ProviderStatusResponse {
  const now = new Date();

  const transformProvider = (provider: BackendProviderStatusDto): ProviderStatus => ({
    name: provider.name,
    available: provider.isAvailable,
    tier: mapTier(provider.tier),
    lastChecked: now,
    errorMessage: provider.isAvailable ? undefined : provider.message || undefined,
    details: provider.message || undefined,
  });

  // Group providers by normalized category
  const llmProviders = backendData.providers
    .filter((p) => normalizeCategory(p.category) === 'llm')
    .map(transformProvider);

  const ttsProviders = backendData.providers
    .filter((p) => normalizeCategory(p.category) === 'tts')
    .map(transformProvider);

  const imageProviders = backendData.providers
    .filter((p) => normalizeCategory(p.category) === 'images')
    .map(transformProvider);

  // Map health status to Green/Yellow/Red
  let overallHealth: 'Green' | 'Yellow' | 'Red' = 'Yellow';
  if (backendData.overallHealth) {
    const healthStr = backendData.overallHealth.toString().toLowerCase();
    if (healthStr === 'green' || healthStr === 'healthy') {
      overallHealth = 'Green';
    } else if (healthStr === 'red' || healthStr === 'unhealthy') {
      overallHealth = 'Red';
    } else {
      overallHealth = 'Yellow';
    }
  }

  return {
    llm: llmProviders,
    tts: ttsProviders,
    images: imageProviders,
    timestamp: backendData.lastUpdated || now.toISOString(),
    overallHealth,
    ollamaActive: backendData.ollamaActive ?? false,
    hasAnyLlm: backendData.hasAnyLlm ?? false,
    hasAnyTts: backendData.hasAnyTts ?? false,
    hasAnyImageProvider: backendData.hasAnyImageProvider ?? false,
  };
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
  /** Whether the last fetch attempt failed (different from error being set during initial load) */
  hasFetchError: boolean;
  /** Number of consecutive fetch failures */
  failureCount: number;
  refresh: () => Promise<void>;
  lastUpdated: Date | null;
  healthSummary: ProviderHealthSummary;
}

/**
 * Hook for polling and managing provider status
 * Polls every 15 seconds for real-time updates
 * Includes retry logic with exponential backoff for transient failures
 * Preserves last successful data when fetch fails
 */
export function useProviderStatus(pollInterval: number = 15000): UseProviderStatusResult {
  const [status, setStatus] = useState<ProviderStatusResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null);
  const [hasFetchError, setHasFetchError] = useState(false);
  const [failureCount, setFailureCount] = useState(0);

  // Track if component is mounted to prevent state updates after unmount
  // Starts as false and is set to true in useEffect
  const isMountedRef = useRef(false);

  // Use a ref to track if we have any status data without causing re-renders
  const hasStatusRef = useRef(false);

  // Update the ref whenever status changes
  useEffect(() => {
    hasStatusRef.current = status !== null;
  }, [status]);

  /**
   * Fetch with retry logic and exponential backoff
   */
  const fetchWithRetry = useCallback(
    async (retryCount: number = 0): Promise<BackendSystemProviderStatusDto | null> => {
      try {
        const response = await fetch('/api/provider-status');

        if (!response.ok) {
          throw new Error(`Failed to fetch provider status: ${response.statusText}`);
        }

        const data: BackendSystemProviderStatusDto = await response.json();
        return data;
      } catch (err) {
        if (retryCount < MAX_RETRIES) {
          // Wait with exponential backoff before retrying
          const delay = RETRY_DELAY_MS * Math.pow(2, retryCount);
          console.warn(
            `Provider status fetch failed (attempt ${retryCount + 1}/${MAX_RETRIES + 1}), retrying in ${delay}ms...`
          );
          await new Promise((resolve) => setTimeout(resolve, delay));

          // Check if still mounted before retrying
          if (!isMountedRef.current) {
            return null;
          }

          return fetchWithRetry(retryCount + 1);
        }
        throw err;
      }
    },
    []
  );

  const fetchStatus = useCallback(async () => {
    // Only set loading to true if we don't have any data yet (use ref to avoid dependency)
    if (!hasStatusRef.current) {
      setIsLoading(true);
    }

    try {
      const backendData = await fetchWithRetry();

      // Check if still mounted
      if (!isMountedRef.current || !backendData) {
        return;
      }

      // Transform backend response format to frontend format
      const transformedData = transformBackendResponse(backendData);

      setStatus(transformedData);
      setLastUpdated(new Date());
      setError(null);
      setHasFetchError(false);
      setFailureCount(0);
    } catch (err) {
      const fetchError = err instanceof Error ? err : new Error(String(err));

      // Only update error state if mounted
      if (!isMountedRef.current) {
        return;
      }

      console.error('Error fetching provider status after retries:', fetchError);
      setError(fetchError);
      setHasFetchError(true);
      setFailureCount((prev) => prev + 1);

      // Keep the previous successful status data if available (don't reset to null)
      // This ensures users see stale data rather than 0/0 when there's a transient failure
    } finally {
      if (isMountedRef.current) {
        setIsLoading(false);
      }
    }
  }, [fetchWithRetry]);

  useEffect(() => {
    // Mark as mounted
    isMountedRef.current = true;

    // Initial fetch
    void fetchStatus();

    // Set up polling interval
    const interval = setInterval(() => {
      void fetchStatus();
    }, pollInterval);

    return () => {
      isMountedRef.current = false;
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

    // Map backend health (Green/Yellow/Red) to frontend health level
    let level: ProviderHealthLevel = 'degraded';
    if (status?.overallHealth) {
      if (status.overallHealth === 'Green') {
        level = 'healthy';
      } else if (status.overallHealth === 'Red') {
        level = 'critical';
      } else {
        level = 'degraded';
      }
    } else {
      // Fallback to old client-side logic if backend doesn't provide health
      if (availableLlm === 0 && totalLlm > 0) {
        level = 'critical';
      } else if (availableTts === 0 && totalTts > 0) {
        level = 'critical';
      } else if (availableLlm < totalLlm || availableTts < totalTts) {
        level = 'degraded';
      } else if (availableImages < totalImages && totalImages > 0) {
        level = 'degraded';
      } else if (totalLlm === 0 && totalTts === 0 && totalImages === 0) {
        level = 'degraded';
      } else {
        level = 'healthy';
      }
    }

    // Generate appropriate message
    let message: string;
    if (level === 'critical') {
      message = 'Critical: Required providers unavailable';
    } else if (level === 'healthy') {
      message = 'All providers operational';
    } else {
      const unavailable: string[] = [];
      if (availableLlm < totalLlm) unavailable.push('LLM');
      if (availableTts < totalTts) unavailable.push('TTS');
      if (availableImages < totalImages) unavailable.push('Image');
      message =
        unavailable.length > 0
          ? `Some ${unavailable.join(', ')} providers unavailable`
          : 'System degraded';
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
    hasFetchError,
    failureCount,
    refresh: fetchStatus,
    lastUpdated,
    healthSummary,
  };
}
