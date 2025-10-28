/**
 * Helper to generate deduplication keys for API requests
 * Ensures identical requests use the same key
 */

/**
 * Create a stable hash from an object
 * Uses JSON.stringify with sorted keys to ensure consistent output
 * @param obj - Object to hash
 * @returns String hash of the object
 */
function createStableHash(obj: unknown): string {
  if (obj === null || obj === undefined) {
    return '';
  }

  // For simple types, just convert to string
  if (typeof obj !== 'object') {
    return String(obj);
  }

  // For objects and arrays, create stable JSON representation
  // Sort keys to ensure consistent ordering
  const sortedJson = JSON.stringify(obj, (_, value) => {
    if (value && typeof value === 'object' && !Array.isArray(value)) {
      return Object.keys(value)
        .sort()
        .reduce((sorted: Record<string, unknown>, key) => {
          sorted[key] = value[key];
          return sorted;
        }, {});
    }
    return value;
  });

  // Simple hash function for the JSON string
  let hash = 0;
  for (let i = 0; i < sortedJson.length; i++) {
    const char = sortedJson.charCodeAt(i);
    hash = (hash << 5) - hash + char;
    hash = hash & hash; // Convert to 32-bit integer
  }

  return hash.toString(36); // Convert to base-36 string for compactness
}

/**
 * Create a deduplication key for an API request
 * @param method - HTTP method (GET, POST, etc.)
 * @param url - Request URL
 * @param data - Optional request data/body
 * @returns Unique key for this request combination
 */
export function createDedupeKey(method: string, url: string, data?: unknown): string {
  const methodUpper = method.toUpperCase();
  const dataHash = data ? createStableHash(data) : '';
  
  if (dataHash) {
    return `${methodUpper}:${url}:${dataHash}`;
  }
  
  return `${methodUpper}:${url}`;
}
