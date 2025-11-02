import { useState, useEffect, useCallback } from 'react';
import { assetService } from '../services/assetService';
import { loggingService } from '../services/loggingService';
import { Asset, AssetType } from '../types/assets';

interface UseAssetsOptions {
  query?: string;
  type?: AssetType;
  page?: number;
  pageSize?: number;
}

interface UseAssetsResult {
  assets: Asset[];
  loading: boolean;
  error: Error | null;
  retry: () => void;
  totalCount: number;
}

/**
 * Hook for loading assets with error handling and retry capability
 */
export function useAssets(options: UseAssetsOptions = {}): UseAssetsResult {
  const { query, type, page = 1, pageSize = 50 } = options;

  const [assets, setAssets] = useState<Asset[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);
  const [retryCount, setRetryCount] = useState(0);

  const loadAssets = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const result = await assetService.getAssets(query, type, undefined, page, pageSize);

      // Handle empty response gracefully
      if (result && typeof result === 'object' && 'assets' in result) {
        setAssets(Array.isArray(result.assets) ? result.assets : []);
        setTotalCount(typeof result.totalCount === 'number' ? result.totalCount : 0);
      } else {
        setAssets([]);
        setTotalCount(0);
      }

      loggingService.info(`Loaded ${assets.length} assets`, 'useAssets', 'loadAssets');
    } catch (err: unknown) {
      const error = err instanceof Error ? err : new Error(String(err));
      setError(error);
      setAssets([]); // Set empty array on error for graceful degradation
      setTotalCount(0);

      loggingService.error('Failed to load assets', error, 'useAssets', 'loadAssets');
    } finally {
      setLoading(false);
    }
  }, [query, type, page, pageSize, assets.length]);

  useEffect(() => {
    loadAssets();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [retryCount, query, type, page, pageSize]);

  const retry = useCallback(() => {
    loggingService.info('Retrying assets load', 'useAssets', 'retry');
    setRetryCount((prev) => prev + 1);
  }, []);

  return {
    assets,
    loading,
    error,
    retry,
    totalCount,
  };
}
