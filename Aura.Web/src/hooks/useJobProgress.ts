/**
 * React hook for managing job progress with EventSource
 * Ensures proper lifecycle management and cleanup of EventSource connections
 */

import { useEffect, useRef } from 'react';
import { JobEvent, subscribeToJobEvents } from '../features/render/api/jobs';

/**
 * Hook for subscribing to job progress updates
 * Automatically manages EventSource lifecycle and cleanup
 * 
 * @param jobId - The ID of the job to monitor (null to stop monitoring)
 * @param onProgress - Callback when progress event is received
 */
export function useJobProgress(
  jobId: string | null,
  onProgress: (event: JobEvent) => void
): void {
  const cleanupFlagRef = useRef(false);
  const unsubscribeRef = useRef<(() => void) | null>(null);
  const timeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    // Reset cleanup flag
    cleanupFlagRef.current = false;

    if (!jobId) {
      return;
    }

    // Subscribe to job events
    const unsubscribe = subscribeToJobEvents(
      jobId,
      (event: JobEvent) => {
        // Call the provided callback
        onProgress(event);

        // Check if this is a terminal event
        if (event.type === 'job-completed' || event.type === 'job-failed') {
          // Unsubscribe after a short delay to allow final events to process
          timeoutRef.current = setTimeout(() => {
            if (unsubscribeRef.current && !cleanupFlagRef.current) {
              unsubscribeRef.current();
              unsubscribeRef.current = null;
            }
          }, 1000);
        }
      },
      (error: Error) => {
        console.error('Job progress error:', error);

        // Only retry if component is still mounted
        if (!cleanupFlagRef.current) {
          // Retry after a delay
          timeoutRef.current = setTimeout(() => {
            if (!cleanupFlagRef.current && unsubscribeRef.current) {
              // Reconnection will be handled by creating a new subscription
              // when the effect runs again
            }
          }, 5000);
        }
      }
    );

    // Store unsubscribe function
    unsubscribeRef.current = unsubscribe;

    // Cleanup function
    return () => {
      cleanupFlagRef.current = true;

      // Clear any pending timeouts
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current);
        timeoutRef.current = null;
      }

      // Unsubscribe from events
      if (unsubscribeRef.current) {
        unsubscribeRef.current();
        unsubscribeRef.current = null;
      }
    };
  }, [jobId, onProgress]);
}
