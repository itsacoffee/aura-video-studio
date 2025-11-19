import apiClient from './apiClient';

/**
 * Provider characteristics from init event
 */
export interface ProviderCharacteristics {
  providerName: string;
  isLocal: boolean;
  expectedFirstTokenMs: number;
  expectedTokensPerSec: number;
  costPer1KTokens: number | null;
  supportsStreaming: boolean;
}

/**
 * Streaming chunk event
 */
export interface StreamChunkEvent {
  eventType: 'chunk';
  content: string;
  accumulatedContent: string;
  tokenIndex: number;
}

/**
 * Complete event with metadata
 */
export interface StreamCompleteEvent {
  eventType: 'complete';
  content: string;
  accumulatedContent: string;
  tokenCount: number;
  metadata: {
    totalTokens: number | null;
    estimatedCost: number | null;
    tokensPerSecond: number | null;
    isLocalModel: boolean | null;
    modelName: string | null;
    timeToFirstTokenMs: number | null;
    totalDurationMs: number | null;
    finishReason: string | null;
  };
}

/**
 * Error event
 */
export interface StreamErrorEvent {
  eventType: 'error';
  errorMessage: string;
  correlationId?: string;
}

/**
 * Init event with provider info
 */
export interface StreamInitEvent {
  eventType: 'init';
  providerName: string;
  isLocal: boolean;
  expectedFirstTokenMs: number;
  expectedTokensPerSec: number;
  costPer1KTokens: number | null;
  supportsStreaming: boolean;
}

/**
 * Union type for all streaming events
 */
export type StreamingScriptEvent =
  | StreamInitEvent
  | StreamChunkEvent
  | StreamCompleteEvent
  | StreamErrorEvent;

/**
 * Event data for streaming script generation (deprecated, use StreamingScriptEvent)
 */
export interface LegacyStreamingScriptEvent {
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
 * Final generation metrics from Ollama (deprecated)
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
  preferredProvider?: string;
}

/**
 * Progress callback for streaming updates
 */
export type StreamProgressCallback = (event: StreamingScriptEvent) => void;

/**
 * Stream script generation with unified provider support
 * Supports all LLM providers: OpenAI, Anthropic, Gemini, Azure, Ollama
 */
// eslint-disable-next-line sonarjs/cognitive-complexity
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
      Accept: 'text/event-stream',
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
  let currentEventType: string | null = null;

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
          currentEventType = null;
          continue;
        }

        if (line.startsWith('event:')) {
          currentEventType = line.slice(6).trim();
          continue;
        }

        if (line.startsWith('data:')) {
          const data = line.slice(5).trim();

          try {
            const parsedData = JSON.parse(data);

            if (!currentEventType) {
              currentEventType = parsedData.eventType;
            }

            let event: StreamingScriptEvent;

            switch (currentEventType) {
              case 'init':
                event = {
                  eventType: 'init',
                  providerName: parsedData.providerName,
                  isLocal: parsedData.isLocal,
                  expectedFirstTokenMs: parsedData.expectedFirstTokenMs,
                  expectedTokensPerSec: parsedData.expectedTokensPerSec,
                  costPer1KTokens: parsedData.costPer1KTokens,
                  supportsStreaming: parsedData.supportsStreaming,
                } as StreamInitEvent;
                break;

              case 'chunk':
                event = {
                  eventType: 'chunk',
                  content: parsedData.content,
                  accumulatedContent: parsedData.accumulatedContent,
                  tokenIndex: parsedData.tokenIndex,
                } as StreamChunkEvent;
                break;

              case 'complete':
                event = {
                  eventType: 'complete',
                  content: parsedData.content || '',
                  accumulatedContent: parsedData.accumulatedContent,
                  tokenCount: parsedData.tokenCount,
                  metadata: parsedData.metadata || {},
                } as StreamCompleteEvent;
                finalContent = parsedData.accumulatedContent;
                break;

              case 'error':
                event = {
                  eventType: 'error',
                  errorMessage: parsedData.errorMessage,
                  correlationId: parsedData.correlationId,
                } as StreamErrorEvent;
                break;

              default:
                console.warn('Unknown event type:', currentEventType);
                continue;
            }

            onProgress(event);
          } catch (error: unknown) {
            console.error(
              'Failed to parse SSE data:',
              error instanceof Error ? error.message : String(error)
            );
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
 * @deprecated Use streamGeneration instead for better control and POST support
 */
export function streamGenerationWithEventSource(
  request: StreamGenerationRequest,
  onProgress: StreamProgressCallback,
  onError: (error: Error) => void,
  onComplete: () => void
): () => void {
  const baseUrl = apiClient.defaults.baseURL || '';
  // Convert request to query parameters, filtering out undefined values
  const params: Record<string, string> = {};
  Object.entries(request).forEach(([key, value]) => {
    if (value !== undefined) {
      params[key] = String(value);
    }
  });
  const queryString = new URLSearchParams(params).toString();
  const url = `${baseUrl}/api/scripts/generate/stream?${queryString}`;

  const eventSource = new EventSource(url);

  eventSource.addEventListener('init', (event: MessageEvent) => {
    try {
      const data = JSON.parse(event.data);
      const initEvent: StreamInitEvent = {
        eventType: 'init',
        providerName: data.providerName,
        isLocal: data.isLocal,
        expectedFirstTokenMs: data.expectedFirstTokenMs,
        expectedTokensPerSec: data.expectedTokensPerSec,
        costPer1KTokens: data.costPer1KTokens,
        supportsStreaming: data.supportsStreaming,
      };
      onProgress(initEvent);
    } catch (error: unknown) {
      console.error(
        'Failed to parse init event:',
        error instanceof Error ? error.message : String(error)
      );
    }
  });

  eventSource.addEventListener('chunk', (event: MessageEvent) => {
    try {
      const data = JSON.parse(event.data);
      const chunkEvent: StreamChunkEvent = {
        eventType: 'chunk',
        content: data.content,
        accumulatedContent: data.accumulatedContent,
        tokenIndex: data.tokenIndex,
      };
      onProgress(chunkEvent);
    } catch (error: unknown) {
      console.error(
        'Failed to parse chunk event:',
        error instanceof Error ? error.message : String(error)
      );
    }
  });

  eventSource.addEventListener('complete', (event: MessageEvent) => {
    try {
      const data = JSON.parse(event.data);
      const completeEvent: StreamCompleteEvent = {
        eventType: 'complete',
        content: data.content || '',
        accumulatedContent: data.accumulatedContent,
        tokenCount: data.tokenCount,
        metadata: data.metadata || {},
      };
      onProgress(completeEvent);
      eventSource.close();
      onComplete();
    } catch (error: unknown) {
      console.error(
        'Failed to parse complete event:',
        error instanceof Error ? error.message : String(error)
      );
    }
  });

  eventSource.addEventListener('error', (event: MessageEvent) => {
    try {
      const data = JSON.parse(event.data);
      const errorEvent: StreamErrorEvent = {
        eventType: 'error',
        errorMessage: data.errorMessage || 'Streaming error occurred',
        correlationId: data.correlationId,
      };
      onProgress(errorEvent);
      onError(new Error(errorEvent.errorMessage));
    } catch {
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
