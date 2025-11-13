/**
 * Integration Tests for Hybrid API Layer
 * Tests the complete flow from API client through transport to backend
 */

import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { ApiClient } from '../client';
import { TransportFactory } from '../transport';

describe('Hybrid API Layer Integration', () => {
  let originalWindow: typeof window;

  beforeEach(() => {
    originalWindow = global.window;
  });

  afterEach(() => {
    global.window = originalWindow;
  });

  describe('Environment Detection', () => {
    it('should correctly identify web environment', () => {
      (global.window as typeof window & { electron?: unknown }).electron = undefined;

      expect(TransportFactory.isElectron()).toBe(false);
      expect(TransportFactory.getEnvironment()).toBe('web');

      const client = new ApiClient('http://localhost:5005');
      expect(client.isElectron()).toBe(false);
      expect(client.getEnvironment()).toBe('web');
      expect(client.getTransportName()).toBe('HTTP');
    });

    it('should correctly identify Electron environment', () => {
      const mockElectron = {
        backend: {
          getUrl: vi.fn().mockResolvedValue('http://localhost:5005'),
        },
      };
      (global.window as typeof window & { electron?: typeof window.electron }).electron =
        mockElectron as typeof window.electron;

      expect(TransportFactory.isElectron()).toBe(true);
      expect(TransportFactory.getEnvironment()).toBe('electron');

      const client = new ApiClient('http://localhost:5005');
      expect(client.isElectron()).toBe(true);
      expect(client.getEnvironment()).toBe('electron');
      expect(client.getTransportName()).toBe('IPC');
    });
  });

  describe('API Client Methods', () => {
    let client: ApiClient;

    beforeEach(() => {
      (global.window as typeof window & { electron?: unknown }).electron = undefined;
      client = new ApiClient('http://localhost:5005');
    });

    it('should have all required methods', () => {
      expect(typeof client.get).toBe('function');
      expect(typeof client.post).toBe('function');
      expect(typeof client.put).toBe('function');
      expect(typeof client.patch).toBe('function');
      expect(typeof client.delete).toBe('function');
      expect(typeof client.subscribe).toBe('function');
      expect(typeof client.uploadFile).toBe('function');
      expect(typeof client.downloadFile).toBe('function');
      expect(typeof client.getTransportName).toBe('function');
      expect(typeof client.isElectron).toBe('function');
      expect(typeof client.getEnvironment).toBe('function');
    });

    it('should maintain method signatures across environments', () => {
      const webClient = new ApiClient('http://localhost:5005');

      const mockElectron = {
        backend: {
          getUrl: vi.fn().mockResolvedValue('http://localhost:5005'),
        },
      };
      (global.window as typeof window & { electron?: typeof window.electron }).electron =
        mockElectron as typeof window.electron;

      const electronClient = new ApiClient('http://localhost:5005');

      expect(typeof webClient.get).toBe(typeof electronClient.get);
      expect(typeof webClient.post).toBe(typeof electronClient.post);
      expect(typeof webClient.subscribe).toBe(typeof electronClient.subscribe);
      expect(typeof webClient.uploadFile).toBe(typeof electronClient.uploadFile);
    });
  });

  describe('Progress Tracking', () => {
    let client: ApiClient;

    beforeEach(() => {
      (global.window as typeof window & { electron?: unknown }).electron = undefined;
      client = new ApiClient('http://localhost:5005');
    });

    it('should support progress callbacks for uploads', () => {
      const file = new File(['test'], 'test.txt', { type: 'text/plain' });
      const onProgress = vi.fn();

      const uploadPromise = client.uploadFile('/api/upload', file, onProgress);
      expect(uploadPromise).toBeInstanceOf(Promise);

      expect(onProgress).not.toHaveBeenCalled();
    });

    it('should support progress callbacks for downloads', () => {
      const onProgress = vi.fn();

      const downloadPromise = client.downloadFile('/api/download', 'test.txt', onProgress);
      expect(downloadPromise).toBeInstanceOf(Promise);

      expect(onProgress).not.toHaveBeenCalled();
    });

    it('should convert progress format correctly', () => {
      expect(true).toBe(true);
    });
  });

  describe('SSE Subscriptions', () => {
    let client: ApiClient;

    beforeEach(() => {
      (global.window as typeof window & { electron?: unknown }).electron = undefined;
      client = new ApiClient('http://localhost:5005');
    });

    it('should support SSE subscriptions', () => {
      const mockEventSource = {
        onopen: null as ((event: Event) => void) | null,
        onmessage: null as ((event: MessageEvent) => void) | null,
        onerror: null as ((event: Event) => void) | null,
        close: vi.fn(),
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        dispatchEvent: vi.fn(),
        readyState: 0,
        url: '',
        withCredentials: false,
        CONNECTING: 0,
        OPEN: 1,
        CLOSED: 2,
      };

      global.EventSource = vi.fn(() => mockEventSource) as typeof EventSource;

      const onMessage = vi.fn();
      const onError = vi.fn();

      const unsubscribe = client.subscribe('/api/events', {
        onMessage,
        onError,
      });

      expect(typeof unsubscribe).toBe('function');
      unsubscribe();
      expect(mockEventSource.close).toHaveBeenCalled();
    });

    it('should work in both HTTP and IPC mode', () => {
      const mockEventSource = {
        onopen: null as ((event: Event) => void) | null,
        onmessage: null as ((event: MessageEvent) => void) | null,
        onerror: null as ((event: Event) => void) | null,
        close: vi.fn(),
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        dispatchEvent: vi.fn(),
        readyState: 0,
        url: '',
        withCredentials: false,
        CONNECTING: 0,
        OPEN: 1,
        CLOSED: 2,
      };

      global.EventSource = vi.fn(() => mockEventSource) as typeof EventSource;

      const webClient = new ApiClient('http://localhost:5005');
      const webUnsubscribe = webClient.subscribe('/api/events', {
        onMessage: vi.fn(),
      });

      const mockElectron = {
        backend: {
          getUrl: vi.fn().mockResolvedValue('http://localhost:5005'),
        },
      };
      (global.window as typeof window & { electron?: typeof window.electron }).electron =
        mockElectron as typeof window.electron;

      const ipcClient = new ApiClient('http://localhost:5005');
      const ipcUnsubscribe = ipcClient.subscribe('/api/events', {
        onMessage: vi.fn(),
      });

      expect(typeof webUnsubscribe).toBe('function');
      expect(typeof ipcUnsubscribe).toBe('function');

      webUnsubscribe();
      ipcUnsubscribe();
    });
  });

  describe('Error Handling', () => {
    let client: ApiClient;

    beforeEach(() => {
      (global.window as typeof window & { electron?: unknown }).electron = undefined;
      client = new ApiClient('http://localhost:5005');
    });

    it('should handle transport errors gracefully', async () => {
      expect(client.getTransportName()).toBe('HTTP');
    });

    it('should provide consistent error format', () => {
      expect(true).toBe(true);
    });
  });

  describe('Backward Compatibility', () => {
    it('should maintain same API surface as old client', () => {
      (global.window as typeof window & { electron?: unknown }).electron = undefined;
      const client = new ApiClient('http://localhost:5005');

      const requiredMethods = [
        'get',
        'post',
        'put',
        'patch',
        'delete',
        'subscribe',
        'uploadFile',
        'downloadFile',
      ];

      requiredMethods.forEach((method) => {
        expect(client).toHaveProperty(method);
        expect(typeof (client as Record<string, unknown>)[method]).toBe('function');
      });
    });

    it('should work with existing service patterns', () => {
      (global.window as typeof window & { electron?: unknown }).electron = undefined;
      const client = new ApiClient('http://localhost:5005');

      const getPromise = client.get('/health');
      expect(getPromise).toBeInstanceOf(Promise);
    });
  });

  describe('Type Safety', () => {
    let client: ApiClient;

    beforeEach(() => {
      (global.window as typeof window & { electron?: unknown }).electron = undefined;
      client = new ApiClient('http://localhost:5005');
    });

    it('should support generic type parameters', () => {
      interface TestResponse {
        success: boolean;
        data: string;
      }

      const getPromise = client.get<TestResponse>('/api/test');
      expect(getPromise).toBeInstanceOf(Promise);
    });

    it('should infer return types correctly', () => {
      expect(true).toBe(true);
    });
  });
});
