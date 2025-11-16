import { apiClient } from './apiClient';

/**
 * Event data for streaming script generation
 */
export interface StreamingScriptEvent {
  eventType: 'chunk' | 'complete' | 'error';
  content: string;
  cumulativeContent: string;
  tokenCount: number;
  progressPercentage: number;
  tokensPerSecond?: number;
  model: string;
  isComplete: boolean;
  metrics?: GenerationMetrics;
  errorMessage?: string;
}

/**
 * Final generation metrics from Ollama
 */
export interface GenerationMetrics {
  totalDurationMs: number;
  loadDurationMs: number;
  promptEvalCount: number;
  promptEvalDurationMs: number;
  evalCount: number;
  evalDurationMs: number;
  tokensPerSecond: number;
}

/**
 * Request parameters for streaming generation
 */
export interface StreamGenerationRequest {
  topic: string;
  audience?: string;
  goal?: string;
  tone?: string;
  language?: string;
  aspect?: string;
  targetDurationSeconds?: number;
  pacing?: string;
  density?: string;
  style?: string;
  model?: string;
}

/**
 * Progress callback for streaming updates
 */
export type StreamProgressCallback = (event: StreamingScriptEvent) => void;

/**
 * Stream script generation from Ollama with real-time updates
 */
export async function streamGeneration(
  request: StreamGenerationRequest,
  onProgress: StreamProgressCallback,
  signal?: AbortSignal
): Promise<string> {
  const baseUrl = apiClient.defaults.baseURL || '';
  const url = `${baseUrl}/api/scripts/generate/stream`;

  const response = await fetch(url, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Accept': 'text/event-stream',
    },
    body: JSON.stringify(request),
    signal,
  });

  if (!response.ok) {
    throw new Error(`HTTP error! status: ${response.status}`);
  }

  const reader = response.body?.getReader();
  if (!reader) {
    throw new Error('Response body is not readable');
  }

  const decoder = new TextDecoder();
  let buffer = '';
  let finalContent = '';

  try {
    while (true) {
      const { done, value } = await reader.read();

      if (done) {
        break;
      }

      buffer += decoder.decode(value, { stream: true });

      const lines = buffer.split('\n');
      buffer = lines.pop() || '';

      for (const line of lines) {
        if (line.trim() === '') {
          continue;
        }

        if (line.startsWith('event:')) {
          continue;
        }

        if (line.startsWith('data:')) {
          const data = line.slice(5).trim();
          
          try {
            const event = JSON.parse(data) as StreamingScriptEvent;
            onProgress(event);

            if (event.isComplete) {
              finalContent = event.cumulativeContent;
            }
          } catch (error: unknown) {
            console.error('Failed to parse SSE data:', error instanceof Error ? error.message : String(error));
          }
        }
      }
    }
  } finally {
    reader.releaseLock();
  }

  return finalContent;
}

/**
 * Stream script generation using EventSource (alternative implementation)
 * Note: EventSource doesn't support POST requests or custom headers easily,
 * so the fetch-based approach above is preferred
 */
export function streamGenerationWithEventSource(
  request: StreamGenerationRequest,
  onProgress: StreamProgressCallback,
  onError: (error: Error) => void,
  onComplete: () => void
): () => void {
  const baseUrl = apiClient.defaults.baseURL || '';
  const queryString = new URLSearchParams(request as Record<string, string>).toString();
  const url = `${baseUrl}/api/scripts/generate/stream?${queryString}`;

  const eventSource = new EventSource(url);

  eventSource.addEventListener('progress', (event: MessageEvent) => {
    try {
      const data = JSON.parse(event.data) as StreamingScriptEvent;
      onProgress(data);
    } catch (error: unknown) {
      console.error('Failed to parse progress event:', error instanceof Error ? error.message : String(error));
    }
  });

  eventSource.addEventListener('complete', (event: MessageEvent) => {
    try {
      const data = JSON.parse(event.data) as StreamingScriptEvent;
      onProgress(data);
      eventSource.close();
      onComplete();
    } catch (error: unknown) {
      console.error('Failed to parse complete event:', error instanceof Error ? error.message : String(error));
    }
  });

  eventSource.addEventListener('error', (event: Event) => {
    const messageEvent = event as MessageEvent;
    try {
      const data = JSON.parse(messageEvent.data) as StreamingScriptEvent;
      onError(new Error(data.errorMessage || 'Streaming error occurred'));
    } catch (e: unknown) {
      onError(new Error('Unknown streaming error'));
    }
    eventSource.close();
  });

  eventSource.onerror = () => {
    eventSource.close();
    onError(new Error('Connection to server lost'));
  };

  return () => {
    eventSource.close();
  };
}
