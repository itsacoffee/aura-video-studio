/**
 * Type definitions for LLM cache API
 */

/**
 * Statistics about cache usage
 */
export interface CacheStatistics {
  totalEntries: number;
  totalHits: number;
  totalMisses: number;
  hitRate: number;
  totalSizeBytes: number;
  totalEvictions: number;
  totalExpirations: number;
  memoryUsageMB?: number;
  gcMemoryMB?: number;
}

/**
 * Response from cache clear operation
 */
export interface CacheClearResponse {
  success: boolean;
  message: string;
  entriesRemoved: number;
}

/**
 * Response from cache eviction operation
 */
export interface CacheEvictResponse {
  success: boolean;
  message: string;
  entriesRemoved: number;
  entriesRemaining: number;
}

/**
 * Response from cache remove operation
 */
export interface CacheRemoveResponse {
  success: boolean;
  message: string;
  key: string;
}

/**
 * Cache metadata for response
 */
export interface CacheMetadata {
  fromCache: boolean;
  cacheAge?: number;
  accessCount?: number;
  cacheKey?: string;
}
