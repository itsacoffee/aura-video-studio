/**
 * API Client Tests
 * Tests the hybrid API client wrapper
 */

import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { ApiClient } from '../client';

describe('ApiClient', () => {
  let originalWindow: typeof window;

  beforeEach(() => {
    originalWindow = global.window;
  });

  afterEach(() => {
    global.window = originalWindow;
  });

  it('should create HTTP transport in web environment', () => {
    (global.window as typeof window & { electron?: unknown }).electron = undefined;
    const client = new ApiClient('http://localhost:5005');
    expect(client.getTransportName()).toBe('HTTP');
    expect(client.getEnvironment()).toBe('web');
    expect(client.isElectron()).toBe(false);
  });

  it('should create IPC transport in Electron environment', () => {
    const mockElectron = {
      backend: {
        getUrl: vi.fn().mockResolvedValue('http://localhost:5005'),
      },
    };
    (global.window as typeof window & { electron?: typeof window.electron }).electron =
      mockElectron as typeof window.electron;

    const client = new ApiClient('http://localhost:5005');
    expect(client.getTransportName()).toBe('IPC');
    expect(client.getEnvironment()).toBe('electron');
    expect(client.isElectron()).toBe(true);
  });

  describe('HTTP Methods', () => {
    let client: ApiClient;

    beforeEach(() => {
      (global.window as typeof window & { electron?: unknown }).electron = undefined;
      client = new ApiClient('http://localhost:5005');
    });

    it('should expose get method', () => {
      expect(typeof client.get).toBe('function');
    });

    it('should expose post method', () => {
      expect(typeof client.post).toBe('function');
    });

    it('should expose put method', () => {
      expect(typeof client.put).toBe('function');
    });

    it('should expose patch method', () => {
      expect(typeof client.patch).toBe('function');
    });

    it('should expose delete method', () => {
      expect(typeof client.delete).toBe('function');
    });

    it('should expose subscribe method', () => {
      expect(typeof client.subscribe).toBe('function');
    });

    it('should expose uploadFile method', () => {
      expect(typeof client.uploadFile).toBe('function');
    });

    it('should expose downloadFile method', () => {
      expect(typeof client.downloadFile).toBe('function');
    });
  });

  describe('IPC Methods', () => {
    let client: ApiClient;

    beforeEach(() => {
      const mockElectron = {
        backend: {
          getUrl: vi.fn().mockResolvedValue('http://localhost:5005'),
        },
      };
      (global.window as typeof window & { electron?: typeof window.electron }).electron =
        mockElectron as typeof window.electron;

      client = new ApiClient('http://localhost:5005');
    });

    it('should expose all methods in IPC mode', () => {
      expect(typeof client.get).toBe('function');
      expect(typeof client.post).toBe('function');
      expect(typeof client.put).toBe('function');
      expect(typeof client.patch).toBe('function');
      expect(typeof client.delete).toBe('function');
      expect(typeof client.subscribe).toBe('function');
      expect(typeof client.uploadFile).toBe('function');
      expect(typeof client.downloadFile).toBe('function');
    });
  });
});
