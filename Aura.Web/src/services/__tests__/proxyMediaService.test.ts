import { describe, it, expect, beforeEach, vi } from 'vitest';
import { ProxyMediaService } from '../proxyMediaService';

describe('ProxyMediaService', () => {
  let service: ProxyMediaService;

  beforeEach(() => {
    service = ProxyMediaService.getInstance();
    service.clearInternalCache();
    localStorage.clear();
    global.fetch = vi.fn();
  });

  describe('proxy mode toggle', () => {
    it('should start with proxy mode enabled by default', () => {
      expect(service.isProxyModeEnabled()).toBe(true);
    });

    it('should toggle proxy mode', () => {
      const initialState = service.isProxyModeEnabled();
      const newState = service.toggleProxyMode();

      expect(newState).toBe(!initialState);
      expect(service.isProxyModeEnabled()).toBe(newState);
    });

    it('should set proxy mode', () => {
      service.setUseProxyMode(false);
      expect(service.isProxyModeEnabled()).toBe(false);

      service.setUseProxyMode(true);
      expect(service.isProxyModeEnabled()).toBe(true);
    });

    it('should persist proxy mode setting', () => {
      service.setUseProxyMode(false);

      const storedSettings = localStorage.getItem('proxyMediaSettings');
      expect(storedSettings).toBeDefined();

      if (storedSettings) {
        const parsed = JSON.parse(storedSettings);
        expect(parsed.useProxyMode).toBe(false);
      }
    });
  });

  describe('proxy exists check', () => {
    it('should check if proxy exists', async () => {
      (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ exists: true }),
      } as Response);

      const exists = await service.proxyExists('/path/to/video.mp4');
      expect(exists).toBe(true);
    });

    it('should return false when proxy does not exist', async () => {
      (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ exists: false }),
      } as Response);

      const exists = await service.proxyExists('/path/to/video.mp4');
      expect(exists).toBe(false);
    });

    it('should handle fetch errors gracefully', async () => {
      (global.fetch as ReturnType<typeof vi.fn>).mockRejectedValueOnce(new Error('Network error'));

      const exists = await service.proxyExists('/path/to/video.mp4');
      expect(exists).toBe(false);
    });
  });

  describe('effective media path', () => {
    it('should return source path when proxy mode is disabled', async () => {
      service.setUseProxyMode(false);
      const sourcePath = '/path/to/video.mp4';

      const effectivePath = await service.getEffectiveMediaPath(sourcePath);
      expect(effectivePath).toBe(sourcePath);
    });

    it('should return proxy path when proxy exists and mode is enabled', async () => {
      service.setUseProxyMode(true);
      const sourcePath = '/path/to/video.mp4';
      const proxyPath = '/cache/proxy/video_preview.mp4';

      (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          id: '123',
          sourcePath,
          proxyPath,
          quality: 'Preview',
          status: 'Completed',
          createdAt: new Date().toISOString(),
          lastAccessedAt: new Date().toISOString(),
          fileSizeBytes: 1000000,
          sourceFileSizeBytes: 5000000,
          width: 1280,
          height: 720,
          bitrateKbps: 3000,
          progressPercent: 100,
        }),
      } as Response);

      const effectivePath = await service.getEffectiveMediaPath(sourcePath);
      expect(effectivePath).toBe(proxyPath);
    });

    it('should return source path when proxy does not exist', async () => {
      service.setUseProxyMode(true);
      const sourcePath = '/path/to/video.mp4';

      (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
      } as Response);

      const effectivePath = await service.getEffectiveMediaPath(sourcePath);
      expect(effectivePath).toBe(sourcePath);
    });
  });
});
