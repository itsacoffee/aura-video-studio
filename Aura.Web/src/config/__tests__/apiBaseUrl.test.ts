/**
 * Tests for API Base URL Resolution
 */

import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import {
  resolveApiBaseUrl,
  resolveApiBaseUrlAsync,
  isElectronEnvironment,
  getElectronBackendUrl,
  type ApiBaseUrlResolution,
} from '../apiBaseUrl';

describe('apiBaseUrl', () => {
  // Store original values
  const originalWindow = global.window;
  const originalImportMeta = import.meta.env;

  beforeEach(() => {
    // Reset window object
    global.window = {
      location: { origin: 'http://localhost:3000' },
    } as unknown as Window & typeof globalThis;
    delete (global.window as Window).desktopBridge;
    delete (global.window as Window).aura;
    delete (global.window as Window).electron;

    // Clear all environment variable stubs
    vi.unstubAllEnvs();
  });

  afterEach(() => {
    // Restore original values
    global.window = originalWindow;
    vi.unstubAllEnvs();
    vi.restoreAllMocks();
  });

  describe('isElectronEnvironment', () => {
    it('should return false when not in Electron', () => {
      expect(isElectronEnvironment()).toBe(false);
    });

    it('should return true when AURA_IS_ELECTRON is set', () => {
      (global.window as Window).AURA_IS_ELECTRON = true;
      expect(isElectronEnvironment()).toBe(true);
    });

    it('should return true when aura bridge exists', () => {
      (global.window as Window).aura = {} as typeof window.aura;
      expect(isElectronEnvironment()).toBe(true);
    });

    it('should return true when desktop bridge is available', () => {
      (global.window as Window).desktopBridge = {} as unknown as typeof window.desktopBridge;
      expect(isElectronEnvironment()).toBe(true);
    });

    it('should return false when window is undefined', () => {
      const temp = global.window;
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      delete (global as any).window;
      expect(isElectronEnvironment()).toBe(false);
      global.window = temp;
    });
  });

  describe('getElectronBackendUrl', () => {
    it('should return null when not in Electron', async () => {
      const result = await getElectronBackendUrl();
      expect(result).toBeNull();
    });

    it('should return URL from AURA_BACKEND_URL global variable', async () => {
      (global.window as Window).AURA_BACKEND_URL = 'http://localhost:5005';
      const result = await getElectronBackendUrl();
      expect(result).toBe('http://localhost:5005');
    });

    it('should trim whitespace from AURA_BACKEND_URL', async () => {
      (global.window as Window).AURA_BACKEND_URL = '  http://localhost:5005  ';
      const result = await getElectronBackendUrl();
      expect(result).toBe('http://localhost:5005');
    });

    it('should return null for empty AURA_BACKEND_URL', async () => {
      (global.window as Window).AURA_BACKEND_URL = '   ';
      const result = await getElectronBackendUrl();
      expect(result).toBeNull();
    });

    it('should return URL from aura runtime when available', async () => {
      (global.window as Window).aura = {
        runtime: {
          getCachedDiagnostics: vi.fn().mockReturnValue({
            backend: { baseUrl: 'http://aura-runtime:5005' },
          }),
        },
      } as unknown as typeof window.aura;
      const result = await getElectronBackendUrl();
      expect(result).toBe('http://aura-runtime:5005');
    });

    it('should return URL from desktop bridge when available', async () => {
      (global.window as Window).desktopBridge = {
        getBackendBaseUrl: vi.fn().mockReturnValue('http://bridge:5005'),
      } as unknown as typeof window.desktopBridge;
      const result = await getElectronBackendUrl();
      expect(result).toBe('http://bridge:5005');
    });

    it('should return URL from desktopBridge.backend.getUrl() when available', async () => {
      (global.window as Window).desktopBridge = {
        backend: {
          getUrl: vi.fn().mockReturnValue('http://contract-bridge:5272'),
        },
      } as unknown as typeof window.desktopBridge;
      const result = await getElectronBackendUrl();
      expect(result).toBe('http://contract-bridge:5272');
    });

    it('should use diagnostic info when desktop bridge cache is empty', async () => {
      const mockDiagnostics = { backend: { baseUrl: 'http://diagnostics:5005' } };
      (global.window as Window).desktopBridge = {
        getBackendBaseUrl: vi.fn().mockReturnValue(undefined),
        getDiagnosticInfo: vi.fn().mockResolvedValue(mockDiagnostics),
      } as unknown as typeof window.desktopBridge;

      const result = await getElectronBackendUrl();
      expect(result).toBe('http://diagnostics:5005');
    });

    it('should call aura.backend.getBaseUrl() when available', async () => {
      const mockGetBaseUrl = vi.fn().mockResolvedValue('http://electron-backend:5005');
      (global.window as Window).aura = {
        backend: {
          getBaseUrl: mockGetBaseUrl,
        },
      } as unknown as typeof window.aura;

      const result = await getElectronBackendUrl();
      expect(mockGetBaseUrl).toHaveBeenCalled();
      expect(result).toBe('http://electron-backend:5005');
    });

    it('should handle errors from aura.backend.getBaseUrl()', async () => {
      const mockGetBaseUrl = vi.fn().mockRejectedValue(new Error('Backend not ready'));
      (global.window as Window).aura = {
        backend: {
          getBaseUrl: mockGetBaseUrl,
        },
      } as unknown as typeof window.aura;

      const consoleWarnSpy = vi.spyOn(console, 'warn').mockImplementation(() => {});
      const result = await getElectronBackendUrl();

      expect(mockGetBaseUrl).toHaveBeenCalled();
      expect(consoleWarnSpy).toHaveBeenCalledWith(
        'Failed to get backend URL from Aura API:',
        expect.any(Error)
      );
      expect(result).toBeNull();
    });

    it('should return null when window is undefined', async () => {
      const temp = global.window;
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      delete (global as any).window;
      const result = await getElectronBackendUrl();
      expect(result).toBeNull();
      global.window = temp;
    });
  });

  describe('resolveApiBaseUrl (synchronous)', () => {
    it('should use Electron global variable when available', () => {
      (global.window as Window).AURA_IS_ELECTRON = true;
      (global.window as Window).AURA_BACKEND_URL = 'http://electron:5005';

      const result = resolveApiBaseUrl();

      expect(result.value).toBe('http://electron:5005');
      expect(result.source).toBe('electron');
      expect(result.isElectron).toBe(true);
    });

    it('should use environment variable when available', () => {
      vi.stubEnv('VITE_API_BASE_URL', 'http://api.example.com');

      const result = resolveApiBaseUrl();

      expect(result.value).toBe('http://api.example.com');
      expect(result.source).toBe('env');
    });

    it('should use window.location.origin as fallback', () => {
      const result = resolveApiBaseUrl();

      expect(result.value).toBe('http://localhost:3000');
      expect(result.source).toBe('origin');
    });

    it('should use default fallback when nothing else available', () => {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      delete (global.window as any).location;

      const result = resolveApiBaseUrl();

      expect(result.value).toBe('http://127.0.0.1:5005');
      expect(result.source).toBe('fallback');
    });

    it('should prioritize Electron over env variable', () => {
      (global.window as Window).AURA_BACKEND_URL = 'http://electron:5005';
      (global.window as Window).AURA_IS_ELECTRON = true;
      vi.stubEnv('VITE_API_BASE_URL', 'http://env.example.com');

      const result = resolveApiBaseUrl();

      expect(result.value).toBe('http://electron:5005');
      expect(result.source).toBe('electron');
    });

    it('should prioritize desktop bridge over legacy globals', () => {
      (global.window as Window).desktopBridge = {
        getBackendBaseUrl: vi.fn().mockReturnValue('http://bridge:5005'),
      } as unknown as typeof window.desktopBridge;
      (global.window as Window).AURA_BACKEND_URL = 'http://legacy:5005';
      (global.window as Window).AURA_IS_ELECTRON = true;

      const result = resolveApiBaseUrl();
      expect(result.value).toBe('http://bridge:5005');
      expect(result.source).toBe('electron');
    });

    it('should use desktopBridge.backend.getUrl() for contract-based URL', () => {
      (global.window as Window).desktopBridge = {
        backend: {
          getUrl: vi.fn().mockReturnValue('http://contract-url:5272'),
        },
      } as unknown as typeof window.desktopBridge;
      (global.window as Window).AURA_IS_ELECTRON = true;

      const result = resolveApiBaseUrl();
      expect(result.value).toBe('http://contract-url:5272');
      expect(result.source).toBe('electron');
      expect(result.isElectron).toBe(true);
    });

    it('should prioritize env over origin', () => {
      vi.stubEnv('VITE_API_BASE_URL', 'http://env.example.com');

      const result = resolveApiBaseUrl();

      expect(result.value).toBe('http://env.example.com');
      expect(result.source).toBe('env');
    });
  });

  describe('resolveApiBaseUrlAsync', () => {
    it('should resolve from Electron API asynchronously', async () => {
      const mockGetUrl = vi.fn().mockResolvedValue('http://electron-async:5005');
      (global.window as Window).AURA_IS_ELECTRON = true;
      (global.window as Window).electron = {
        selectFolder: vi.fn(),
        openPath: vi.fn(),
        openExternal: vi.fn(),
        backend: {
          getUrl: mockGetUrl,
        },
      };

      const result = await resolveApiBaseUrlAsync();

      expect(mockGetUrl).toHaveBeenCalled();
      expect(result.value).toBe('http://electron-async:5005');
      expect(result.source).toBe('electron');
      expect(result.isElectron).toBe(true);
    });

    it('should fall back to synchronous resolution on Electron error', async () => {
      const mockGetUrl = vi.fn().mockRejectedValue(new Error('Backend not ready'));
      (global.window as Window).AURA_IS_ELECTRON = true;
      (global.window as Window).electron = {
        selectFolder: vi.fn(),
        openPath: vi.fn(),
        openExternal: vi.fn(),
        backend: {
          getUrl: mockGetUrl,
        },
      };
      vi.stubEnv('VITE_API_BASE_URL', 'http://fallback.example.com');

      const consoleWarnSpy = vi.spyOn(console, 'warn').mockImplementation(() => {});
      const result = await resolveApiBaseUrlAsync();

      expect(result.value).toBe('http://fallback.example.com');
      expect(result.source).toBe('env');
    });

    it('should use synchronous resolution when not in Electron', async () => {
      vi.stubEnv('VITE_API_BASE_URL', 'http://web.example.com');

      const result = await resolveApiBaseUrlAsync();

      expect(result.value).toBe('http://web.example.com');
      expect(result.source).toBe('env');
      expect(result.isElectron).toBe(false);
    });
  });

  describe('Edge Cases', () => {
    it('should handle empty string from Electron', async () => {
      (global.window as Window).AURA_BACKEND_URL = '';
      const result = resolveApiBaseUrl();

      expect(result.source).not.toBe('electron');
    });

    it('should handle whitespace-only strings', () => {
      (global.window as Window).AURA_BACKEND_URL = '   ';
      const result = resolveApiBaseUrl();

      expect(result.source).not.toBe('electron');
    });

    it('should mark isElectron correctly in all scenarios', () => {
      (global.window as Window).AURA_IS_ELECTRON = true;
      vi.stubEnv('VITE_API_BASE_URL', 'http://example.com');

      const result = resolveApiBaseUrl();

      expect(result.isElectron).toBe(true);
    });
  });
});
