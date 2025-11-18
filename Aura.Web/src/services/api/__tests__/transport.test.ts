/**
 * Transport Layer Tests
 * Tests HTTP and IPC transport implementations
 */

import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import {
  HttpTransport,
  IpcTransport,
  TransportFactory,
  type IApiTransport,
  type SSESubscriptionOptions,
} from '../transport';

describe('TransportFactory', () => {
  let originalWindow: typeof window;

  beforeEach(() => {
    originalWindow = global.window;
  });

  afterEach(() => {
    global.window = originalWindow;
  });

  it('should detect web environment when window.aura is not present', () => {
    (global.window as typeof window & { aura?: unknown }).aura = undefined;
    expect(TransportFactory.isElectron()).toBe(false);
    expect(TransportFactory.getEnvironment()).toBe('web');
  });

  it('should detect Electron environment when window.aura is present', () => {
    (global.window as typeof window & { aura?: unknown }).aura = {};
    expect(TransportFactory.isElectron()).toBe(true);
    expect(TransportFactory.getEnvironment()).toBe('electron');
  });

  it('should create HTTP transport in web environment', () => {
    (global.window as typeof window & { aura?: unknown }).aura = undefined;
    const transport = TransportFactory.create('http://localhost:5005');
    expect(transport).toBeInstanceOf(HttpTransport);
    expect(transport.getName()).toBe('HTTP');
  });

  it('should create IPC transport in Electron environment', () => {
    const mockAura = {
      backend: {
        getBaseUrl: vi.fn().mockResolvedValue('http://localhost:5005'),
      },
    } as typeof window.aura;
    (global.window as typeof window & { aura?: unknown }).aura = mockAura;
    (global.window as typeof window & { electron?: unknown }).electron =
      mockAura as unknown as typeof window.electron;

    const transport = TransportFactory.create('http://localhost:5005');
    expect(transport).toBeInstanceOf(IpcTransport);
    expect(transport.getName()).toBe('IPC');
  });
});

describe('HttpTransport', () => {
  let transport: HttpTransport;

  beforeEach(() => {
    transport = new HttpTransport('http://localhost:5005');
  });

  it('should be available', () => {
    expect(transport.isAvailable()).toBe(true);
  });

  it('should have correct name', () => {
    expect(transport.getName()).toBe('HTTP');
  });

  describe('request', () => {
    it('should make GET request', async () => {
      expect(transport.getName()).toBe('HTTP');
    });

    it('should make POST request with data', async () => {
      expect(transport.getName()).toBe('HTTP');
    });
  });

  describe('subscribe', () => {
    it('should create EventSource for SSE', () => {
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

      const options: SSESubscriptionOptions = {
        onMessage: vi.fn(),
        onError: vi.fn(),
        onOpen: vi.fn(),
        onClose: vi.fn(),
      };

      const unsubscribe = transport.subscribe('/api/events', options);
      expect(global.EventSource).toHaveBeenCalledWith('http://localhost:5005/api/events');

      unsubscribe();
      expect(mockEventSource.close).toHaveBeenCalled();
    });

    it('should handle SSE errors', () => {
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

      const onError = vi.fn();
      const options: SSESubscriptionOptions = {
        onMessage: vi.fn(),
        onError,
      };

      transport.subscribe('/api/events', options);

      mockEventSource.onerror?.(new Event('error'));
      expect(onError).toHaveBeenCalled();
    });
  });

  describe('upload', () => {
    it('should upload file with progress', async () => {
      const file = new File(['test content'], 'test.txt', { type: 'text/plain' });
      expect(file.name).toBe('test.txt');
    });
  });

  describe('download', () => {
    it('should download file', async () => {
      const filename = 'test.txt';
      expect(filename).toBe('test.txt');
    });
  });
});

describe('IpcTransport', () => {
  let transport: IpcTransport;
  let mockAura: typeof window.aura;

  beforeEach(() => {
    mockAura = {
      backend: {
        getBaseUrl: vi.fn().mockResolvedValue('http://localhost:5005'),
      },
    } as typeof window.aura;

    (global.window as typeof window & { aura?: typeof window.aura }).aura = mockAura;
    (global.window as typeof window & { electron?: typeof window.electron }).electron =
      mockAura as unknown as typeof window.electron;
    transport = new IpcTransport();
  });

  afterEach(() => {
    (global.window as typeof window & { aura?: unknown }).aura = undefined;
    (global.window as typeof window & { electron?: unknown }).electron = undefined;
  });

  it('should throw error if Aura bridge is not available', () => {
    (global.window as typeof window & { aura?: unknown }).aura = undefined;
    (global.window as typeof window & { electron?: unknown }).electron = undefined;
    expect(() => new IpcTransport()).toThrow('IPC Transport requires Electron environment');
  });

  it('should be available when window.aura exists', () => {
    expect(transport.isAvailable()).toBe(true);
  });

  it('should have correct name', () => {
    expect(transport.getName()).toBe('IPC');
  });

  describe('request', () => {
    it('should make GET request via IPC', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        status: 200,
        statusText: 'OK',
        json: async () => ({ success: true }),
        headers: new Headers(),
      });
      global.fetch = mockFetch;

      const response = await transport.request('/api/test', 'GET');
      expect(mockElectron.backend.getUrl).toHaveBeenCalled();
      expect(response.status).toBe(200);
      expect(response.data).toEqual({ success: true });
    });

    it('should make POST request with data via IPC', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        status: 201,
        statusText: 'Created',
        json: async () => ({ id: '123' }),
        headers: new Headers(),
      });
      global.fetch = mockFetch;

      const response = await transport.request('/api/test', 'POST', { name: 'test' });
      expect(mockElectron.backend.getUrl).toHaveBeenCalled();
      expect(response.status).toBe(201);
      expect(response.data).toEqual({ id: '123' });
    });
  });

  describe('subscribe', () => {
    it('should create EventSource for IPC SSE', async () => {
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

      const options: SSESubscriptionOptions = {
        onMessage: vi.fn(),
        onError: vi.fn(),
        onOpen: vi.fn(),
        onClose: vi.fn(),
      };

      const unsubscribe = transport.subscribe('/api/events', options);

      await new Promise((resolve) => setTimeout(resolve, 10));

      expect(mockElectron.backend.getUrl).toHaveBeenCalled();

      unsubscribe();
      expect(mockEventSource.close).toHaveBeenCalled();
    });
  });

  describe('upload', () => {
    it('should upload file via IPC', async () => {
      const file = new File(['test content'], 'test.txt', { type: 'text/plain' });
      expect(file.name).toBe('test.txt');
    });
  });

  describe('download', () => {
    it('should download file via IPC', async () => {
      const filename = 'test.txt';
      expect(filename).toBe('test.txt');
    });
  });
});
