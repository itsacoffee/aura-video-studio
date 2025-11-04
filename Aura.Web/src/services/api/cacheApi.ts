import type { CacheStatistics, CacheClearResponse, CacheEvictResponse } from '../../types/cache';
import apiClient from './apiClient';

/**
 * API client for LLM cache management
 */

/**
 * Gets cache statistics
 */
export async function getCacheStatistics(): Promise<CacheStatistics> {
  const response = await apiClient.get<CacheStatistics>('/api/cache/stats');
  return response.data;
}

/**
 * Clears all entries from the cache
 */
export async function clearCache(): Promise<CacheClearResponse> {
  const response = await apiClient.post<CacheClearResponse>('/api/cache/clear');
  return response.data;
}

/**
 * Evicts expired entries from the cache
 */
export async function evictExpiredEntries(): Promise<CacheEvictResponse> {
  const response = await apiClient.post<CacheEvictResponse>('/api/cache/evict-expired');
  return response.data;
}

/**
 * Removes a specific cache entry by key
 */
export async function removeCacheEntry(
  key: string
): Promise<import('../../types/cache').CacheRemoveResponse> {
  const response = await apiClient.delete<import('../../types/cache').CacheRemoveResponse>(
    `/api/cache/${key}`
  );
  return response.data;
}

/**
 * Forces a cache refresh by clearing all entries
 */
export async function forceRefresh(): Promise<CacheClearResponse> {
  const response = await apiClient.post<CacheClearResponse>('/api/cache/refresh');
  return response.data;
}
