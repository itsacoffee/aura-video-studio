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

  describe('cache size management', () => {
    it('should set max cache size', async () => {
      const maxSizeBytes = 5 * 1024 * 1024 * 1024; // 5GB

      (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ maxSizeBytes }),
      } as Response);

      await service.setMaxCacheSize(maxSizeBytes);

      expect(global.fetch).toHaveBeenCalledWith(
        expect.stringContaining('/api/proxy/cache-limit'),
        expect.objectContaining({
          method: 'POST',
          body: JSON.stringify({ maxSizeBytes }),
        })
      );
    });

    it('should get max cache size', async () => {
      const maxSizeBytes = 10 * 1024 * 1024 * 1024; // 10GB

      (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ maxSizeBytes }),
      } as Response);

      const result = await service.getMaxCacheSize();
      expect(result).toBe(maxSizeBytes);
    });

    it('should trigger eviction', async () => {
      (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ message: 'Eviction completed' }),
      } as Response);

      await service.triggerEviction();

      expect(global.fetch).toHaveBeenCalledWith(
        expect.stringContaining('/api/proxy/evict'),
        expect.objectContaining({
          method: 'POST',
        })
      );
    });

    it('should handle eviction errors', async () => {
      (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: false,
      } as Response);

      await expect(service.triggerEviction()).rejects.toThrow('Failed to trigger eviction');
    });
  });

  describe('cache statistics', () => {
    it('should get cache stats with new fields', async () => {
      const stats = {
        totalProxies: 5,
        totalCacheSizeBytes: 5000000000,
        totalSourceSizeBytes: 10000000000,
        compressionRatio: 0.5,
        maxCacheSizeBytes: 10000000000,
        cacheUsagePercent: 50.0,
        isOverLimit: false,
      };

      (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => stats,
      } as Response);

      const result = await service.getCacheStats();
      expect(result).toEqual(stats);
      expect(result.maxCacheSizeBytes).toBe(10000000000);
      expect(result.cacheUsagePercent).toBe(50.0);
      expect(result.isOverLimit).toBe(false);
    });
  });
});
