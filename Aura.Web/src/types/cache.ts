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
