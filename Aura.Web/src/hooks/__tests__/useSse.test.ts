/**
 * Unit tests for useSse hook
 */

import { renderHook, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { z } from 'zod';
import { useSse, SseConnectionState } from '../useSse';

// Mock EventSource
class MockEventSource {
  public onopen: ((event: Event) => void) | null = null;
  public onerror: ((event: Event) => void) | null = null;
  public onmessage: ((event: MessageEvent) => void) | null = null;
  public readyState: number = 0;
  private eventListeners: Map<string, ((event: Event) => void)[]> = new Map();

  constructor(public url: string) {
    // Simulate async connection
    setTimeout(() => {
      this.readyState = 1;
      if (this.onopen) {
        this.onopen(new Event('open'));
      }
    }, 0);
  }

  addEventListener(type: string, listener: (event: Event) => void): void {
    if (!this.eventListeners.has(type)) {
      this.eventListeners.set(type, []);
    }
    this.eventListeners.get(type)!.push(listener);
  }

  removeEventListener(type: string, listener: (event: Event) => void): void {
    const listeners = this.eventListeners.get(type);
    if (listeners) {
      const index = listeners.indexOf(listener);
      if (index > -1) {
        listeners.splice(index, 1);
      }
    }
  }

  close(): void {
    this.readyState = 2;
  }

  // Helper to simulate incoming message
  public simulateMessage(data: string, type = 'message', id?: string): void {
    const event = new MessageEvent(type, { data, lastEventId: id });

    if (type === 'message' && this.onmessage) {
      this.onmessage(event);
    }

    const listeners = this.eventListeners.get(type);
    if (listeners) {
      listeners.forEach((listener) => listener(event));
    }
  }

  // Helper to simulate error
  public simulateError(): void {
    if (this.onerror) {
      this.onerror(new Event('error'));
    }
  }
}

describe('useSse', () => {
  let mockEventSource: MockEventSource;

  beforeEach(() => {
    // Replace global EventSource with mock
    vi.stubGlobal(
      'EventSource',
      vi.fn((url: string) => {
        mockEventSource = new MockEventSource(url);
        return mockEventSource;
      })
    );
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('should connect to SSE endpoint', async () => {
    const { result } = renderHook(() =>
      useSse({
        url: '/api/test',
      })
    );

    expect(result.current.state).toBe(SseConnectionState.CONNECTING);

    await waitFor(() => {
      expect(result.current.state).toBe(SseConnectionState.CONNECTED);
    });
  });

  it('should receive and parse messages', async () => {
    const { result } = renderHook(() =>
      useSse<{ status: string }>({
        url: '/api/test',
      })
    );

    await waitFor(() => {
      expect(result.current.state).toBe(SseConnectionState.CONNECTED);
    });

    const testData = { status: 'processing' };
    mockEventSource.simulateMessage(JSON.stringify(testData));

    await waitFor(() => {
      expect(result.current.lastEvent).not.toBeNull();
      expect(result.current.lastEvent?.data).toEqual(testData);
    });
  });

  it('should validate messages with zod schema', async () => {
    const schema = z.object({
      status: z.string(),
      progress: z.number(),
    });

    const onMessage = vi.fn();

    const { result } = renderHook(() =>
      useSse({
        url: '/api/test',
        schema,
        onMessage,
      })
    );

    await waitFor(() => {
      expect(result.current.state).toBe(SseConnectionState.CONNECTED);
    });

    // Send valid message
    const validData = { status: 'processing', progress: 50 };
    mockEventSource.simulateMessage(JSON.stringify(validData));

    await waitFor(() => {
      expect(onMessage).toHaveBeenCalledWith(
        expect.objectContaining({
          data: validData,
        })
      );
    });

    // Send invalid message
    onMessage.mockClear();
    const invalidData = { status: 'processing' }; // missing progress
    mockEventSource.simulateMessage(JSON.stringify(invalidData));

    // Should not call onMessage for invalid data
    await new Promise((resolve) => setTimeout(resolve, 100));
    expect(onMessage).not.toHaveBeenCalled();
  });

  it('should support last-event-id for resuming streams', async () => {
    const { result } = renderHook(() =>
      useSse({
        url: '/api/test',
        useLastEventId: true,
      })
    );

    await waitFor(() => {
      expect(result.current.state).toBe(SseConnectionState.CONNECTED);
    });

    // Send message with event ID
    const testData = { status: 'processing' };
    mockEventSource.simulateMessage(JSON.stringify(testData), 'message', 'event-123');

    await waitFor(() => {
      expect(result.current.lastEvent?.id).toBe('event-123');
    });
  });
});
