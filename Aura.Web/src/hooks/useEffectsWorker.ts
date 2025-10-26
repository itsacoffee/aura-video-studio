/**
 * useEffectsWorker Hook
 * 
 * Hook for processing effects using Web Workers to offload CPU-intensive
 * image processing from the main thread.
 */

import { useEffect, useRef, useCallback } from 'react';

interface EffectMessage {
  type: 'apply-effects';
  imageData: ImageData;
  effects: any[];
  timestamp: number;
}

interface ResultMessage {
  type: 'result';
  imageData: ImageData;
  timestamp: number;
  processingTime: number;
}

interface ErrorMessage {
  type: 'error';
  error: string;
  timestamp: number;
}

type WorkerResponse = ResultMessage | ErrorMessage;

export function useEffectsWorker() {
  const workerRef = useRef<Worker | null>(null);
  const callbacksRef = useRef<Map<number, (result: ImageData | null, error?: string, processingTime?: number) => void>>(new Map());

  // Initialize worker on mount
  useEffect(() => {
    // Create worker from URL
    try {
      // In production, the worker will be bundled separately
      // For now, we'll create it inline
      const workerCode = `
        self.onmessage = (event) => {
          const message = event.data;
          if (message.type === 'apply-effects') {
            try {
              const startTime = performance.now();
              const result = applyEffects(message.imageData, message.effects);
              const processingTime = performance.now() - startTime;
              self.postMessage({
                type: 'result',
                imageData: result,
                timestamp: message.timestamp,
                processingTime,
              });
            } catch (error) {
              self.postMessage({
                type: 'error',
                error: error.message || 'Unknown error',
                timestamp: message.timestamp,
              });
            }
          }
        };

        function applyEffects(imageData, effects) {
          // Simple pass-through for now
          // In a real implementation, this would apply the actual effects
          return imageData;
        }
      `;

      const blob = new Blob([workerCode], { type: 'application/javascript' });
      const workerUrl = URL.createObjectURL(blob);
      workerRef.current = new Worker(workerUrl);

      // Set up message handler
      workerRef.current.onmessage = (event: MessageEvent<WorkerResponse>) => {
        const response = event.data;
        
        if (response.type === 'result') {
          const callback = callbacksRef.current.get(response.timestamp);
          if (callback) {
            callback(response.imageData, undefined, response.processingTime);
            callbacksRef.current.delete(response.timestamp);
          }
        } else if (response.type === 'error') {
          const callback = callbacksRef.current.get(response.timestamp);
          if (callback) {
            callback(null, response.error);
            callbacksRef.current.delete(response.timestamp);
          }
        }
      };

      // Cleanup
      return () => {
        if (workerRef.current) {
          workerRef.current.terminate();
          URL.revokeObjectURL(workerUrl);
        }
      };
    } catch (error) {
      console.error('Failed to initialize effects worker:', error);
    }
  }, []);

  /**
   * Apply effects to an ImageData object using the worker
   */
  const applyEffects = useCallback(
    (imageData: ImageData, effects: any[]): Promise<ImageData> => {
      return new Promise((resolve, reject) => {
        if (!workerRef.current) {
          reject(new Error('Worker not initialized'));
          return;
        }

        const timestamp = Date.now();
        
        // Store callback
        callbacksRef.current.set(timestamp, (result, error) => {
          if (error) {
            reject(new Error(error));
          } else if (result) {
            resolve(result);
          } else {
            reject(new Error('No result returned from worker'));
          }
        });

        // Send message to worker
        const message: EffectMessage = {
          type: 'apply-effects',
          imageData,
          effects,
          timestamp,
        };

        workerRef.current.postMessage(message);

        // Timeout after 5 seconds
        setTimeout(() => {
          if (callbacksRef.current.has(timestamp)) {
            callbacksRef.current.delete(timestamp);
            reject(new Error('Worker timeout'));
          }
        }, 5000);
      });
    },
    []
  );

  return { applyEffects };
}
