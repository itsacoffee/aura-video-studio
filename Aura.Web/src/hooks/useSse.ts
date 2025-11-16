/**
 * Enhanced SSE Hook with Auto-Reconnection
 *
 * Features:
 * - Automatic reconnection with exponential backoff
 * - Last-Event-ID support for resuming streams
 * - Zod validation for event data
 * - Connection state management
 * - Proper cleanup on unmount
 */

import { useEffect, useRef, useState, useCallback } from 'react';
import { z } from 'zod';
import { apiUrl } from '@/config/api';
import { loggingService } from '@/services/loggingService';
import { toError } from '@/utils/errorUtils';

/**
 * Connection state enum
 */
export enum SseConnectionState {
  CONNECTING = 'CONNECTING',
  CONNECTED = 'CONNECTED',
  DISCONNECTED = 'DISCONNECTED',
  ERROR = 'ERROR',
}

/**
 * SSE event with type and data
 */
export interface SseEvent<T = unknown> {
  type: string;
  data: T;
  id?: string;
  retry?: number;
}

/**
 * SSE hook options
 */
export interface UseSseOptions<T> {
  /**
   * SSE endpoint URL
   */
  url: string;

  /**
   * Event types to listen for (default: all events)
   */
  eventTypes?: string[];

  /**
   * Zod schema for validating event data
   */
  schema?: z.ZodSchema<T>;

  /**
   * Enable automatic reconnection (default: true)
   */
  autoReconnect?: boolean;

  /**
   * Maximum reconnection attempts (default: 10)
   */
  maxReconnectAttempts?: number;

  /**
   * Initial reconnection delay in ms (default: 1000)
   */
  initialReconnectDelay?: number;

  /**
   * Maximum reconnection delay in ms (default: 30000)
   */
  maxReconnectDelay?: number;

  /**
   * Enable Last-Event-ID support (default: true)
   */
  useLastEventId?: boolean;

  /**
   * Custom headers to include in the request
   */
  headers?: Record<string, string>;

  /**
   * Callback when connection opens
   */
  onOpen?: () => void;

  /**
   * Callback when connection closes
   */
  onClose?: () => void;

  /**
   * Callback when an error occurs
   */
  onError?: (error: Event) => void;

  /**
   * Callback when a message is received
   */
  onMessage?: (event: SseEvent<T>) => void;
}

/**
 * SSE hook return value
 */
export interface UseSseReturn<T> {
  /**
   * Current connection state
   */
  state: SseConnectionState;

  /**
   * Last received event
   */
  lastEvent: SseEvent<T> | null;

  /**
   * All received events
   */
  events: SseEvent<T>[];

  /**
   * Last error
   */
  error: Error | null;

  /**
   * Number of reconnection attempts
   */
  reconnectAttempts: number;

  /**
   * Manually close the connection
   */
  close: () => void;

  /**
   * Manually reconnect
   */
  reconnect: () => void;
}

/**
 * Enhanced SSE Hook
 *
 * @example
 * ```tsx
 * const { state, lastEvent, events } = useSse({
 *   url: '/api/jobs/123/events',
 *   schema: z.object({
 *     status: z.string(),
 *     progress: z.number(),
 *   }),
 *   onMessage: (event) => {
 *     console.info('Progress:', event.data.progress);
 *   },
 * });
 * ```
 */
export function useSse<T = unknown>(options: UseSseOptions<T>): UseSseReturn<T> {
  const {
    url,
    eventTypes,
    schema,
    autoReconnect = true,
    maxReconnectAttempts = 10,
    initialReconnectDelay = 1000,
    maxReconnectDelay = 30000,
    useLastEventId = true,
    onOpen,
    onClose,
    onError,
    onMessage,
  } = options;

  const [state, setState] = useState<SseConnectionState>(SseConnectionState.CONNECTING);
  const [lastEvent, setLastEvent] = useState<SseEvent<T> | null>(null);
  const [events, setEvents] = useState<SseEvent<T>[]>([]);
  const [error, setError] = useState<Error | null>(null);
  const [reconnectAttempts, setReconnectAttempts] = useState(0);

  const eventSourceRef = useRef<EventSource | null>(null);
  const reconnectTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const lastEventIdRef = useRef<string | undefined>();
  const isManualCloseRef = useRef(false);

  /**
   * Parse and validate event data
   */
  const parseEventData = useCallback(
    (data: string): T | null => {
      try {
        const parsed = JSON.parse(data) as T;

        if (schema) {
          const result = schema.safeParse(parsed);
          if (!result.success) {
            loggingService.warn('SSE event validation failed', 'useSse', 'validation', {
              error: result.error.message,
              data: parsed,
            });
            return null;
          }
          return result.data;
        }

        return parsed;
      } catch (err) {
        loggingService.error('Failed to parse SSE event data', toError(err), 'useSse', 'parse');
        return null;
      }
    },
    [schema]
  );

  /**
   * Handle incoming message
   */
  const handleMessage = useCallback(
    (event: MessageEvent, type: string) => {
      const data = parseEventData(event.data);

      if (data === null) {
        return;
      }

      const sseEvent: SseEvent<T> = {
        type,
        data,
        id: event.lastEventId || undefined,
      };

      // Store last event ID for reconnection
      if (event.lastEventId && useLastEventId) {
        lastEventIdRef.current = event.lastEventId;
      }

      setLastEvent(sseEvent);
      setEvents((prev) => [...prev, sseEvent]);

      if (onMessage) {
        onMessage(sseEvent);
      }

      loggingService.debug('SSE event received', 'useSse', 'message', {
        type,
        hasId: !!event.lastEventId,
      });
    },
    [parseEventData, useLastEventId, onMessage]
  );

  /**
   * Connect to SSE endpoint
   */
  const connect = useCallback(() => {
    if (eventSourceRef.current) {
      eventSourceRef.current.close();
    }

    setState(SseConnectionState.CONNECTING);
    loggingService.info('Connecting to SSE endpoint', 'useSse', 'connect', {
      url,
      attempt: reconnectAttempts + 1,
    });

    try {
      // Build URL with Last-Event-ID if available
      let finalUrl = url;
      if (useLastEventId && lastEventIdRef.current) {
        const separator = url.includes('?') ? '&' : '?';
        finalUrl = `${url}${separator}lastEventId=${lastEventIdRef.current}`;
      }

      // Note: EventSource doesn't support custom headers in browser
      // For authenticated requests, use cookies or include token in URL
      const resolvedUrl =
        finalUrl.startsWith('http://') || finalUrl.startsWith('https://')
          ? finalUrl
          : apiUrl(finalUrl);
      const eventSource = new EventSource(resolvedUrl);
      eventSourceRef.current = eventSource;

      eventSource.onopen = () => {
        setState(SseConnectionState.CONNECTED);
        setReconnectAttempts(0);
        setError(null);
        loggingService.info('SSE connection established', 'useSse', 'open');

        if (onOpen) {
          onOpen();
        }
      };

      eventSource.onerror = (event) => {
        const err = new Error('SSE connection error');
        loggingService.error('SSE connection error', err, 'useSse', 'error');

        setError(err);
        setState(SseConnectionState.ERROR);

        if (onError) {
          onError(event);
        }

        // Attempt reconnection if enabled
        if (
          autoReconnect &&
          !isManualCloseRef.current &&
          reconnectAttempts < maxReconnectAttempts
        ) {
          const delay = Math.min(
            initialReconnectDelay * Math.pow(2, reconnectAttempts),
            maxReconnectDelay
          );

          loggingService.info('Scheduling SSE reconnection', 'useSse', 'reconnect', {
            attempt: reconnectAttempts + 1,
            delay,
          });

          reconnectTimerRef.current = setTimeout(() => {
            setReconnectAttempts((prev) => prev + 1);
            connect();
          }, delay);
        } else if (reconnectAttempts >= maxReconnectAttempts) {
          loggingService.warn('Max SSE reconnection attempts reached', 'useSse', 'reconnect', {
            maxAttempts: maxReconnectAttempts,
          });
          setState(SseConnectionState.DISCONNECTED);
        }
      };

      // Listen for specific event types or all messages
      if (eventTypes && eventTypes.length > 0) {
        eventTypes.forEach((type) => {
          eventSource.addEventListener(type, (event) => {
            handleMessage(event as MessageEvent, type);
          });
        });
      } else {
        eventSource.onmessage = (event) => {
          handleMessage(event, 'message');
        };
      }
    } catch (err) {
      const error = toError(err);
      loggingService.error('Failed to create SSE connection', error, 'useSse', 'connect');
      setError(error);
      setState(SseConnectionState.ERROR);
    }
  }, [
    url,
    useLastEventId,
    autoReconnect,
    maxReconnectAttempts,
    initialReconnectDelay,
    maxReconnectDelay,
    reconnectAttempts,
    eventTypes,
    handleMessage,
    onOpen,
    onError,
  ]);

  /**
   * Close connection
   */
  const close = useCallback(() => {
    isManualCloseRef.current = true;

    if (reconnectTimerRef.current) {
      clearTimeout(reconnectTimerRef.current);
      reconnectTimerRef.current = null;
    }

    if (eventSourceRef.current) {
      eventSourceRef.current.close();
      eventSourceRef.current = null;
    }

    setState(SseConnectionState.DISCONNECTED);
    loggingService.info('SSE connection closed', 'useSse', 'close');

    if (onClose) {
      onClose();
    }
  }, [onClose]);

  /**
   * Manually trigger reconnection
   */
  const reconnect = useCallback(() => {
    isManualCloseRef.current = false;
    setReconnectAttempts(0);
    connect();
  }, [connect]);

  // Connect on mount
  useEffect(() => {
    connect();

    // Cleanup on unmount
    return () => {
      close();
    };
  }, [connect, close]);

  return {
    state,
    lastEvent,
    events,
    error,
    reconnectAttempts,
    close,
    reconnect,
  };
}
