import { useState, useCallback } from 'react';
import { apiUrl } from '../config/api';

export interface InstallProgress {
  engineId: string;
  phase: 'downloading' | 'extracting' | 'verifying' | 'complete';
  bytesProcessed: number;
  totalBytes: number;
  percentComplete: number;
  message?: string;
}

export interface InstallState {
  isInstalling: boolean;
  progress: InstallProgress | null;
  error: string | null;
}

/**
 * Handle Server-Sent Event for installation progress
 */
function handleInstallEvent(
  event: string,
  data: string,
  setInstallState: React.Dispatch<React.SetStateAction<InstallState>>,
  resolve: (value: boolean) => void
): void {
  try {
    const parsedData = JSON.parse(data);

    if (event === 'progress') {
      setInstallState({
        isInstalling: true,
        progress: parsedData as InstallProgress,
        error: null,
      });
    } else if (event === 'complete') {
      setInstallState({
        isInstalling: false,
        progress: null,
        error: null,
      });
      resolve(parsedData.success === true);
    } else if (event === 'error') {
      setInstallState({
        isInstalling: false,
        progress: null,
        error: parsedData.error || 'Installation failed',
      });
      resolve(false);
    }
  } catch (err) {
    console.error('Failed to parse SSE data:', err);
  }
}

/**
 * Process Server-Sent Event lines from stream
 */
function processEventLines(
  lines: string[],
  setInstallState: React.Dispatch<React.SetStateAction<InstallState>>,
  resolve: (value: boolean) => void
): void {
  for (const line of lines) {
    if (!line.trim()) continue;

    const eventMatch = line.match(/^event: (\w+)\ndata: (.+)$/);
    if (eventMatch) {
      const [, event, data] = eventMatch;
      handleInstallEvent(event, data, setInstallState, resolve);
    }
  }
}

export function useEngineInstallProgress() {
  const [installState, setInstallState] = useState<InstallState>({
    isInstalling: false,
    progress: null,
    error: null,
  });

  const installWithProgress = useCallback(
    async (
      engineId: string,
      options?: {
        customUrl?: string;
        localFilePath?: string;
        version?: string;
        port?: number;
      }
    ): Promise<boolean> => {
      setInstallState({
        isInstalling: true,
        progress: null,
        error: null,
      });

      return new Promise((resolve) => {
        const eventSource = new EventSource(apiUrl('/api/engines/install-stream'));

        // Send install request via POST by creating a hidden form
        // (EventSource only supports GET, so we use a workaround)
        // Actually, we need to use fetch with a ReadableStream instead
        eventSource.close();

        // Use fetch with streaming instead
        const installViaFetch = async () => {
          try {
            const response = await fetch(apiUrl('/api/engines/install-stream'), {
              method: 'POST',
              headers: {
                'Content-Type': 'application/json',
              },
              body: JSON.stringify({
                engineId,
                ...options,
              }),
            });

            if (!response.ok || !response.body) {
              throw new Error('Failed to start installation stream');
            }

            const reader = response.body.getReader();
            const decoder = new TextDecoder();
            let buffer = '';

            // Stream reading requires infinite loop with break condition inside
            // eslint-disable-next-line no-constant-condition
            while (true) {
              const { done, value } = await reader.read();

              if (done) {
                break;
              }

              buffer += decoder.decode(value, { stream: true });
              const lines = buffer.split('\n\n');
              buffer = lines.pop() || '';

              processEventLines(lines, setInstallState, resolve);
            }
            }

            // If stream ended without complete/error event
            setInstallState({
              isInstalling: false,
              progress: null,
              error: 'Installation stream ended unexpectedly',
            });
            resolve(false);
          } catch (err) {
            setInstallState({
              isInstalling: false,
              progress: null,
              error: err instanceof Error ? err.message : 'Installation failed',
            });
            resolve(false);
          }
        };

        installViaFetch();
      });
    },
    []
  );

  return {
    ...installState,
    installWithProgress,
  };
}
