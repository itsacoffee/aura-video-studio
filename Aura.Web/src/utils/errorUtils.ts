/**
 * Error handling utilities
 */

/**
 * Convert unknown error to Error instance
 * Useful for catch blocks where error type is unknown
 */
export function toError(err: unknown): Error {
  return err instanceof Error ? err : new Error(String(err));
}

/**
 * Check if an HTTP status code indicates a retryable error
 */
export function isRetryableStatus(status?: number): boolean {
  // No status usually means network error (retryable)
  if (!status) return true;

  // 5xx server errors are generally retryable
  if (status >= 500 && status < 600) return true;

  // Specific 4xx errors that are retryable
  if (status === 408 || status === 429) return true;

  return false;
}
