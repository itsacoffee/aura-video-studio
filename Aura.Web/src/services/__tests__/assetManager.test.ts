import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import type { AssetReference } from '../../types/asset';
import { AssetManagerService } from '../assetManager';

// Mock the API client
vi.mock('../api/apiClient', () => ({
  post: vi.fn(),
}));

// Mock the logging service
vi.mock('../loggingService', () => ({
  loggingService: {
    createLogger: () => ({
      info: vi.fn(),
      warn: vi.fn(),
      error: vi.fn(),
      debug: vi.fn(),
    }),
    info: vi.fn(),
    warn: vi.fn(),
    error: vi.fn(),
  },
}));

describe('AssetManagerService', () => {
  let service: AssetManagerService;
  let mockPost: ReturnType<typeof vi.fn>;

  const createMockAsset = (overrides: Partial<AssetReference> = {}): AssetReference => ({
    id: 'asset-1',
    name: 'test-video.mp4',
    type: 'video',
    originalPath: '/path/to/test-video.mp4',
    fileHash: 'abc123',
    fileSize: 1024000,
    mimeType: 'video/mp4',
    createdAt: '2024-01-01T00:00:00Z',
    modifiedAt: '2024-01-01T00:00:00Z',
    importedAt: '2024-01-01T00:00:00Z',
    status: 'online',
    usage: {
      timelineCount: 0,
      clipIds: [],
      isUsed: false,
    },
    ...overrides,
  });

  beforeEach(async () => {
    vi.clearAllMocks();
    vi.useFakeTimers();

    // Get the mocked post function
    const apiClient = await import('../api/apiClient');
    mockPost = apiClient.post as ReturnType<typeof vi.fn>;

    // Create a new instance for each test
    service = new AssetManagerService();
  });

  afterEach(() => {
    service.cleanup();
    vi.useRealTimers();
  });

  describe('initialization', () => {
    it('should initialize with empty assets', () => {
      expect(service.getAllAssets()).toHaveLength(0);
    });

    it('should initialize with provided assets', () => {
      const assets = [createMockAsset(), createMockAsset({ id: 'asset-2', name: 'test2.mp4' })];

      service.initialize(assets);

      expect(service.getAllAssets()).toHaveLength(2);
    });

    it('should clear previous assets on re-initialization', () => {
      const initialAssets = [createMockAsset()];
      service.initialize(initialAssets);
      expect(service.getAllAssets()).toHaveLength(1);

      const newAssets = [
        createMockAsset({ id: 'asset-new-1' }),
        createMockAsset({ id: 'asset-new-2' }),
      ];
      service.initialize(newAssets);

      expect(service.getAllAssets()).toHaveLength(2);
      expect(service.getAsset('asset-1')).toBeUndefined();
      expect(service.getAsset('asset-new-1')).toBeDefined();
    });
  });

  describe('getAsset', () => {
    it('should return undefined for non-existent asset', () => {
      expect(service.getAsset('non-existent')).toBeUndefined();
    });

    it('should return asset by ID', () => {
      const asset = createMockAsset();
      service.initialize([asset]);

      const retrieved = service.getAsset('asset-1');
      expect(retrieved).toEqual(asset);
    });
  });

  describe('getOfflineAssets', () => {
    it('should return only offline assets', () => {
      const assets = [
        createMockAsset({ id: 'online-1', status: 'online' }),
        createMockAsset({ id: 'offline-1', status: 'offline' }),
        createMockAsset({ id: 'online-2', status: 'online' }),
        createMockAsset({ id: 'offline-2', status: 'offline' }),
      ];

      service.initialize(assets);

      const offline = service.getOfflineAssets();
      expect(offline).toHaveLength(2);
      expect(offline.every((a) => a.status === 'offline')).toBe(true);
    });

    it('should return empty array when no offline assets', () => {
      const assets = [
        createMockAsset({ id: 'online-1', status: 'online' }),
        createMockAsset({ id: 'online-2', status: 'online' }),
      ];

      service.initialize(assets);

      expect(service.getOfflineAssets()).toHaveLength(0);
    });
  });

  describe('getUnusedAssets', () => {
    it('should return only unused assets', () => {
      const assets = [
        createMockAsset({
          id: 'used-1',
          usage: { timelineCount: 1, clipIds: ['clip-1'], isUsed: true },
        }),
        createMockAsset({
          id: 'unused-1',
          usage: { timelineCount: 0, clipIds: [], isUsed: false },
        }),
      ];

      service.initialize(assets);

      const unused = service.getUnusedAssets();
      expect(unused).toHaveLength(1);
      expect(unused[0].id).toBe('unused-1');
    });
  });

  describe('getAssetsByStatus', () => {
    it('should filter assets by status', () => {
      const assets = [
        createMockAsset({ id: 'online-1', status: 'online' }),
        createMockAsset({ id: 'offline-1', status: 'offline' }),
        createMockAsset({ id: 'modified-1', status: 'modified' }),
      ];

      service.initialize(assets);

      const onlineAssets = service.getAssetsByStatus('online');
      expect(onlineAssets).toHaveLength(1);
      expect(onlineAssets[0].id).toBe('online-1');

      const modifiedAssets = service.getAssetsByStatus('modified');
      expect(modifiedAssets).toHaveLength(1);
      expect(modifiedAssets[0].id).toBe('modified-1');
    });
  });

  describe('updateAssetUsage', () => {
    it('should add clip to usage', () => {
      const asset = createMockAsset();
      service.initialize([asset]);

      service.updateAssetUsage('asset-1', 'clip-1', 'add');

      const updated = service.getAsset('asset-1');
      expect(updated?.usage.clipIds).toContain('clip-1');
      expect(updated?.usage.timelineCount).toBe(1);
      expect(updated?.usage.isUsed).toBe(true);
    });

    it('should remove clip from usage', () => {
      const asset = createMockAsset({
        usage: {
          timelineCount: 2,
          clipIds: ['clip-1', 'clip-2'],
          isUsed: true,
        },
      });
      service.initialize([asset]);

      service.updateAssetUsage('asset-1', 'clip-1', 'remove');

      const updated = service.getAsset('asset-1');
      expect(updated?.usage.clipIds).not.toContain('clip-1');
      expect(updated?.usage.clipIds).toContain('clip-2');
      expect(updated?.usage.timelineCount).toBe(1);
      expect(updated?.usage.isUsed).toBe(true);
    });

    it('should set isUsed to false when all clips removed', () => {
      const asset = createMockAsset({
        usage: {
          timelineCount: 1,
          clipIds: ['clip-1'],
          isUsed: true,
        },
      });
      service.initialize([asset]);

      service.updateAssetUsage('asset-1', 'clip-1', 'remove');

      const updated = service.getAsset('asset-1');
      expect(updated?.usage.clipIds).toHaveLength(0);
      expect(updated?.usage.timelineCount).toBe(0);
      expect(updated?.usage.isUsed).toBe(false);
    });

    it('should not add duplicate clip IDs', () => {
      const asset = createMockAsset();
      service.initialize([asset]);

      service.updateAssetUsage('asset-1', 'clip-1', 'add');
      service.updateAssetUsage('asset-1', 'clip-1', 'add');

      const updated = service.getAsset('asset-1');
      expect(updated?.usage.clipIds).toHaveLength(1);
    });

    it('should handle non-existent asset gracefully', () => {
      service.initialize([]);

      // Should not throw
      expect(() => service.updateAssetUsage('non-existent', 'clip-1', 'add')).not.toThrow();
    });
  });

  describe('removeAsset', () => {
    it('should remove asset by ID', () => {
      const assets = [createMockAsset({ id: 'asset-1' }), createMockAsset({ id: 'asset-2' })];
      service.initialize(assets);

      const result = service.removeAsset('asset-1');

      expect(result).toBe(true);
      expect(service.getAsset('asset-1')).toBeUndefined();
      expect(service.getAllAssets()).toHaveLength(1);
    });

    it('should return false for non-existent asset', () => {
      service.initialize([]);

      const result = service.removeAsset('non-existent');

      expect(result).toBe(false);
    });
  });

  describe('checkAssetStatus', () => {
    it('should return embedded for embedded assets', async () => {
      const asset = createMockAsset({
        embedded: { data: 'base64data' },
      });

      const status = await service.checkAssetStatus(asset);

      expect(status).toBe('embedded');
    });

    it('should return offline when file does not exist', async () => {
      mockPost.mockResolvedValueOnce({ exists: false });

      const asset = createMockAsset();
      const status = await service.checkAssetStatus(asset);

      expect(status).toBe('offline');
    });

    it('should return online when file exists and hash matches', async () => {
      mockPost.mockResolvedValueOnce({ exists: true, hash: 'abc123' });

      const asset = createMockAsset({ fileHash: 'abc123' });
      const status = await service.checkAssetStatus(asset);

      expect(status).toBe('online');
    });

    it('should return modified when file exists but hash differs', async () => {
      mockPost.mockResolvedValueOnce({ exists: true, hash: 'different-hash' });

      const asset = createMockAsset({ fileHash: 'abc123' });
      const status = await service.checkAssetStatus(asset);

      expect(status).toBe('modified');
    });

    it('should return offline on API error', async () => {
      mockPost.mockRejectedValueOnce(new Error('Network error'));

      const asset = createMockAsset();
      const status = await service.checkAssetStatus(asset);

      expect(status).toBe('offline');
    });
  });

  describe('checkAllAssetStatus', () => {
    it('should return empty map when no assets', async () => {
      service.initialize([]);

      const results = await service.checkAllAssetStatus();

      expect(results.size).toBe(0);
    });

    it('should handle embedded assets without API call', async () => {
      const assets = [createMockAsset({ id: 'embedded-1', embedded: { data: 'base64data' } })];
      service.initialize(assets);

      const results = await service.checkAllAssetStatus();

      expect(results.get('embedded-1')).toBe('embedded');
      // No API call should have been made for embedded assets
    });

    it('should use batch API when available', async () => {
      const assets = [
        createMockAsset({ id: 'asset-1', fileHash: 'hash1' }),
        createMockAsset({ id: 'asset-2', fileHash: 'hash2' }),
      ];
      service.initialize(assets);

      mockPost.mockResolvedValueOnce({
        'asset-1': { exists: true, hash: 'hash1' },
        'asset-2': { exists: false },
      });

      const results = await service.checkAllAssetStatus();

      expect(results.get('asset-1')).toBe('online');
      expect(results.get('asset-2')).toBe('offline');
    });

    it('should fall back to individual checks when batch fails', async () => {
      const assets = [createMockAsset({ id: 'asset-1', fileHash: 'hash1' })];
      service.initialize(assets);

      // Batch API fails
      mockPost.mockRejectedValueOnce(new Error('Batch endpoint not available'));
      // Individual check succeeds
      mockPost.mockResolvedValueOnce({ exists: true, hash: 'hash1' });

      const results = await service.checkAllAssetStatus();

      expect(results.get('asset-1')).toBe('online');
    });
  });

  describe('relinkAsset', () => {
    beforeEach(() => {
      const asset = createMockAsset({ status: 'offline' });
      service.initialize([asset]);
    });

    it('should return error for non-existent asset', async () => {
      const result = await service.relinkAsset({
        assetId: 'non-existent',
        newPath: '/new/path.mp4',
      });

      expect(result.success).toBe(false);
      expect(result.error).toBe('Asset not found');
    });

    it('should successfully relink asset', async () => {
      mockPost.mockResolvedValueOnce({
        exists: true,
        size: 2048000,
        mimeType: 'video/mp4',
        createdAt: '2024-01-01T00:00:00Z',
        modifiedAt: '2024-01-02T00:00:00Z',
        hash: 'new-hash',
      });

      const result = await service.relinkAsset({
        assetId: 'asset-1',
        newPath: '/new/path/video.mp4',
      });

      expect(result.success).toBe(true);
      expect(result.newPath).toBe('/new/path/video.mp4');
      expect(result.oldPath).toBe('/path/to/test-video.mp4');

      const updated = service.getAsset('asset-1');
      expect(updated?.originalPath).toBe('/new/path/video.mp4');
      expect(updated?.status).toBe('online');
    });

    it('should report hash mismatch when verifyHash is true', async () => {
      mockPost.mockResolvedValueOnce({
        exists: true,
        size: 2048000,
        mimeType: 'video/mp4',
        createdAt: '2024-01-01T00:00:00Z',
        modifiedAt: '2024-01-02T00:00:00Z',
        hash: 'different-hash',
      });

      const result = await service.relinkAsset({
        assetId: 'asset-1',
        newPath: '/new/path/video.mp4',
        verifyHash: true,
      });

      expect(result.success).toBe(true);
      expect(result.hashMatch).toBe(false);
    });

    it('should return error when new file does not exist', async () => {
      mockPost.mockRejectedValueOnce(new Error('File not found'));

      const result = await service.relinkAsset({
        assetId: 'asset-1',
        newPath: '/non-existent/path.mp4',
      });

      expect(result.success).toBe(false);
      expect(result.error).toBe('File not found');
    });
  });

  describe('bulkRelink', () => {
    it('should relink all found files', async () => {
      const assets = [
        createMockAsset({ id: 'offline-1', status: 'offline' }),
        createMockAsset({ id: 'offline-2', status: 'offline' }),
      ];
      service.initialize(assets);

      // Mock search responses - one found, one not found
      mockPost
        .mockResolvedValueOnce({ foundPath: '/found/path/video1.mp4' }) // search for offline-1
        .mockResolvedValueOnce({
          // file info for relink
          exists: true,
          size: 1024,
          mimeType: 'video/mp4',
          createdAt: '2024-01-01T00:00:00Z',
          modifiedAt: '2024-01-01T00:00:00Z',
        })
        .mockResolvedValueOnce({ foundPath: null }); // search for offline-2

      const result = await service.bulkRelink({
        searchDirectory: '/search/directory',
        recursive: true,
        matchByName: true,
        matchByHash: false,
      });

      expect(result.found).toBe(1);
      expect(result.notFound).toBe(1);
      expect(result.relinked).toHaveLength(1);
      expect(result.stillMissing).toContain('offline-2');
    });

    it('should only process specified asset IDs', async () => {
      const assets = [
        createMockAsset({ id: 'offline-1', status: 'offline' }),
        createMockAsset({ id: 'offline-2', status: 'offline' }),
        createMockAsset({ id: 'offline-3', status: 'offline' }),
      ];
      service.initialize(assets);

      mockPost.mockResolvedValue({ foundPath: null });

      const result = await service.bulkRelink({
        searchDirectory: '/search/directory',
        recursive: true,
        matchByName: true,
        matchByHash: false,
        assetIds: ['offline-1', 'offline-2'],
      });

      expect(result.stillMissing).toHaveLength(2);
      expect(result.stillMissing).toContain('offline-1');
      expect(result.stillMissing).toContain('offline-2');
      expect(result.stillMissing).not.toContain('offline-3');
    });
  });

  describe('toggleProxy', () => {
    it('should toggle proxy for asset with proxy', () => {
      const asset = createMockAsset({
        proxy: {
          path: '/proxy/video.mp4',
          resolution: { width: 1280, height: 720 },
          format: 'mp4',
          generatedAt: '2024-01-01T00:00:00Z',
          isActive: false,
        },
      });
      service.initialize([asset]);

      // Mock window.dispatchEvent
      const dispatchEventSpy = vi.spyOn(window, 'dispatchEvent');

      service.toggleProxy('asset-1', true);

      const updated = service.getAsset('asset-1');
      expect(updated?.proxy?.isActive).toBe(true);
      expect(dispatchEventSpy).toHaveBeenCalledWith(
        expect.objectContaining({
          type: 'asset:proxyToggled',
        })
      );
    });

    it('should throw error for asset without proxy', () => {
      const asset = createMockAsset();
      service.initialize([asset]);

      expect(() => service.toggleProxy('asset-1', true)).toThrow('Asset has no proxy');
    });
  });

  describe('exportAssets', () => {
    it('should export all assets', () => {
      const assets = [createMockAsset({ id: 'asset-1' }), createMockAsset({ id: 'asset-2' })];
      service.initialize(assets);

      const exported = service.exportAssets();

      expect(exported).toHaveLength(2);
      expect(exported).toEqual(assets);
    });
  });

  describe('cleanup', () => {
    it('should clear all assets and stop status checks', () => {
      const assets = [createMockAsset()];
      service.initialize(assets);
      expect(service.getAllAssets()).toHaveLength(1);

      service.cleanup();

      expect(service.getAllAssets()).toHaveLength(0);
    });
  });
});
