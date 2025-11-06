/**
 * Service for dependency scanning via new unified API
 */

import type {
  DependencyScanResult,
  ScanProgressEvent,
} from '../types/dependency-scan';

/**
 * Scan dependencies using immediate JSON endpoint
 * @param forceRefresh Force a new scan even if cached result exists
 * @returns Scan result
 */
export async function scanDependencies(
  forceRefresh = false
): Promise<DependencyScanResult> {
  const url = `/api/system/scan${forceRefresh ? '?forceRefresh=true' : ''}`;

  const response = await fetch(url, {
    method: 'POST',
  });

  if (!response.ok) {
    throw new Error(`Dependency scan failed: ${response.statusText}`);
  }

  return await response.json();
}

/**
 * Scan dependencies with real-time progress via SSE
 * @param forceRefresh Force a new scan even if cached result exists
 * @param onProgress Callback for progress updates
 * @param onComplete Callback for completion
 * @param onError Callback for errors
 * @returns EventSource connection (caller should close it when done)
 */
export function scanDependenciesStream(
  forceRefresh: boolean,
  onProgress: (event: ScanProgressEvent) => void,
  onComplete: (event: ScanProgressEvent) => void,
  onError: (error: Error) => void
): EventSource {
  const url = `/api/system/scan/stream${forceRefresh ? '?forceRefresh=true' : ''}`;

  const eventSource = new EventSource(url);

  eventSource.addEventListener('started', (e) => {
    const data: ScanProgressEvent = JSON.parse((e as MessageEvent).data);
    onProgress({ ...data, event: 'started' });
  });

  eventSource.addEventListener('step', (e) => {
    const data: ScanProgressEvent = JSON.parse((e as MessageEvent).data);
    onProgress({ ...data, event: 'step' });
  });

  eventSource.addEventListener('issue', (e) => {
    const data: ScanProgressEvent = JSON.parse((e as MessageEvent).data);
    onProgress({ ...data, event: 'issue' });
  });

  eventSource.addEventListener('completed', (e) => {
    const data: ScanProgressEvent = JSON.parse((e as MessageEvent).data);
    onComplete({ ...data, event: 'completed' });
    eventSource.close();
  });

  eventSource.addEventListener('error', (e) => {
    const data = e as MessageEvent;
    if (data.data) {
      try {
        const errorData: ScanProgressEvent = JSON.parse(data.data);
        onError(new Error(errorData.message || 'Scan failed'));
      } catch {
        onError(new Error('Scan failed'));
      }
    } else {
      onError(new Error('Connection error during scan'));
    }
    eventSource.close();
  });

  eventSource.onerror = () => {
    onError(new Error('EventSource connection failed'));
    eventSource.close();
  };

  return eventSource;
}

/**
 * Get cached scan result if available
 * @returns Cached scan result or null
 */
export async function getCachedScan(): Promise<DependencyScanResult | null> {
  const response = await fetch('/api/system/scan/cached');

  if (response.status === 404) {
    return null;
  }

  if (!response.ok) {
    throw new Error(`Failed to get cached scan: ${response.statusText}`);
  }

  return await response.json();
}

/**
 * Clear cached scan result
 */
export async function clearScanCache(): Promise<void> {
  const response = await fetch('/api/system/scan/cache', {
    method: 'DELETE',
  });

  if (!response.ok) {
    throw new Error(`Failed to clear scan cache: ${response.statusText}`);
  }
}
