/**
 * Request deduplication utility
 * Reuses in-flight promises for identical requests to prevent duplicate operations
 */

/**
 * Request deduplicator class
 * Stores in-flight requests and reuses promises for identical keys
 */
export class RequestDeduplicator {
  private pending: Map<string, Promise<unknown>> = new Map();

  /**
   * Deduplicate a request by key
   * @param key - Unique identifier for the request
   * @param request - Function that returns a promise for the request
   * @returns Promise that resolves with the request result
   */
  public deduplicate<T>(key: string, request: () => Promise<T>): Promise<T> {
    // Check if request is already in flight
    const existing = this.pending.get(key);
    if (existing) {
      return existing as Promise<T>;
    }

    // Execute new request
    const promise = request().finally(() => {
      // Remove from pending map when complete
      this.pending.delete(key);
    });

    // Store in pending map
    this.pending.set(key, promise);

    return promise;
  }

  /**
   * Clear pending requests
   * @param key - Optional specific key to clear. If not provided, clears all.
   */
  public clear(key?: string): void {
    if (key) {
      this.pending.delete(key);
    } else {
      this.pending.clear();
    }
  }

  /**
   * Check if a request is currently pending
   * @param key - Request key to check
   * @returns true if request is in flight
   */
  public isPending(key: string): boolean {
    return this.pending.has(key);
  }

  /**
   * Get count of pending requests
   * @returns Number of in-flight requests
   */
  public get pendingCount(): number {
    return this.pending.size;
  }
}

/**
 * Singleton instance for global deduplication
 */
export const requestDeduplicator = new RequestDeduplicator();
