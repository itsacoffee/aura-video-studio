import { apiClient } from './apiClient';
import type { CacheStatistics, CacheClearResponse, CacheEvictResponse } from '../../types/cache';

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
