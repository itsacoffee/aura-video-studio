/**
 * Asset Manager Service
 * Handles all asset-related operations including tracking, relinking, and collection
 */

import type {
  AssetReference,
  AssetStatus,
  MediaInfo,
  CollectFilesOptions,
  CollectFilesResult,
  RelinkRequest,
  RelinkResult,
  BulkRelinkRequest,
  BulkRelinkResult,
  FileInfo,
} from '../types/asset';
import { post } from './api/apiClient';
import { loggingService } from './loggingService';

const API_BASE = '/api/assets';

/**
 * Event types for asset status changes
 */
export interface AssetStatusChangedEvent {
  id: string;
  status: AssetStatus;
}

export interface AssetProxyToggledEvent {
  assetId: string;
  useProxy: boolean;
}

/**
 * Asset Manager Service Class
 * Manages asset tracking, relinking, and collection operations
 */
class AssetManagerService {
  private assets: Map<string, AssetReference> = new Map();
  private statusCheckInterval: ReturnType<typeof setInterval> | null = null;
  private logger = loggingService.createLogger('AssetManager');

  /**
   * Initialize asset manager with project assets
   */
  initialize(assets: AssetReference[]): void {
    this.assets.clear();
    assets.forEach((asset) => {
      this.assets.set(asset.id, asset);
    });

    // Start periodic status checks
    this.startStatusChecks();

    this.logger.info('initialized', undefined, { assetCount: assets.length });
  }

  /**
   * Register a new asset
   */
  async registerAsset(filePath: string, type: AssetReference['type']): Promise<AssetReference> {
    const id = crypto.randomUUID();
    const now = new Date().toISOString();

    // Get file info from backend
    const fileInfo = await this.getFileInfo(filePath);
    const mediaInfo = await this.getMediaInfo(filePath, type);

    const asset: AssetReference = {
      id,
      name: this.extractFileName(filePath),
      type,
      originalPath: filePath,
      relativePath: undefined, // Set when project is saved
      fileHash: fileInfo.hash,
      fileSize: fileInfo.size,
      mimeType: fileInfo.mimeType,
      createdAt: fileInfo.createdAt,
      modifiedAt: fileInfo.modifiedAt,
      importedAt: now,
      status: 'online',
      mediaInfo,
      usage: {
        timelineCount: 0,
        clipIds: [],
        isUsed: false,
      },
    };

    this.assets.set(id, asset);

    this.logger.info('registered', undefined, { id, name: asset.name, type });

    return asset;
  }

  /**
   * Check status of all assets
   * Uses batch API when available for better performance with many assets
   */
  async checkAllAssetStatus(): Promise<Map<string, AssetStatus>> {
    const results = new Map<string, AssetStatus>();

    // Skip if no assets
    if (this.assets.size === 0) {
      return results;
    }

    // Separate embedded assets (no API call needed) from others
    const { embeddedAssets, checkableAssets } = this.categorizeAssets();

    // Handle embedded assets immediately
    for (const [id] of embeddedAssets) {
      results.set(id, 'embedded');
    }

    // For checkable assets, try batch API first, fall back to individual checks
    if (checkableAssets.length > 0) {
      await this.checkAssetsWithFallback(checkableAssets, results);
    }

    return results;
  }

  /**
   * Categorize assets into embedded and checkable groups
   */
  private categorizeAssets(): {
    embeddedAssets: Array<[string, AssetReference]>;
    checkableAssets: Array<[string, AssetReference]>;
  } {
    const embeddedAssets: Array<[string, AssetReference]> = [];
    const checkableAssets: Array<[string, AssetReference]> = [];

    for (const [id, asset] of this.assets) {
      if (asset.embedded) {
        embeddedAssets.push([id, asset]);
      } else {
        checkableAssets.push([id, asset]);
      }
    }

    return { embeddedAssets, checkableAssets };
  }

  /**
   * Check assets using batch API, fall back to individual checks if needed
   */
  private async checkAssetsWithFallback(
    checkableAssets: Array<[string, AssetReference]>,
    results: Map<string, AssetStatus>
  ): Promise<void> {
    try {
      await this.batchCheckAssets(checkableAssets, results);
    } catch {
      // Fall back to individual checks if batch endpoint not available
      await this.individualCheckAssets(checkableAssets, results);
    }
  }

  /**
   * Batch check assets using batch API endpoint
   */
  private async batchCheckAssets(
    checkableAssets: Array<[string, AssetReference]>,
    results: Map<string, AssetStatus>
  ): Promise<void> {
    const batchResult = await post<Record<string, { exists: boolean; hash?: string }>>(
      `${API_BASE}/check-files-batch`,
      {
        paths: checkableAssets.map(([, asset]) => ({
          id: asset.id,
          path: asset.originalPath,
          expectedHash: asset.fileHash,
        })),
      }
    );

    for (const [id, asset] of checkableAssets) {
      const fileStatus = batchResult[id];
      const status = this.determineStatusFromFileInfo(fileStatus, asset.fileHash);

      results.set(id, status);
      if (status !== asset.status) {
        this.updateAssetStatus(id, status);
      }
    }
  }

  /**
   * Check assets individually (fallback when batch is not available)
   */
  private async individualCheckAssets(
    checkableAssets: Array<[string, AssetReference]>,
    results: Map<string, AssetStatus>
  ): Promise<void> {
    for (const [id, asset] of checkableAssets) {
      const status = await this.checkAssetStatus(asset);
      results.set(id, status);

      if (status !== asset.status) {
        this.updateAssetStatus(id, status);
      }
    }
  }

  /**
   * Determine asset status from file info response
   */
  private determineStatusFromFileInfo(
    fileStatus: { exists: boolean; hash?: string } | undefined,
    expectedHash?: string
  ): AssetStatus {
    if (!fileStatus || !fileStatus.exists) {
      return 'offline';
    }
    if (expectedHash && fileStatus.hash !== expectedHash) {
      return 'modified';
    }
    return 'online';
  }

  /**
   * Check status of a single asset
   */
  async checkAssetStatus(asset: AssetReference): Promise<AssetStatus> {
    if (asset.embedded) {
      return 'embedded';
    }

    try {
      const result = await post<{ exists: boolean; hash?: string }>(`${API_BASE}/check-file`, {
        path: asset.originalPath,
      });

      if (!result.exists) {
        return 'offline';
      }

      if (asset.fileHash && result.hash !== asset.fileHash) {
        return 'modified';
      }

      return 'online';
    } catch {
      return 'offline';
    }
  }

  /**
   * Relink a single asset to a new path
   */
  async relinkAsset(request: RelinkRequest): Promise<RelinkResult> {
    const asset = this.assets.get(request.assetId);

    if (!asset) {
      return {
        success: false,
        assetId: request.assetId,
        oldPath: '',
        newPath: request.newPath,
        error: 'Asset not found',
      };
    }

    try {
      // Verify new file exists
      const fileInfo = await this.getFileInfo(request.newPath);

      // Optional hash verification
      let hashMatch: boolean | undefined;
      if (request.verifyHash && asset.fileHash) {
        hashMatch = fileInfo.hash === asset.fileHash;
        if (!hashMatch) {
          this.logger.warn('relink hash mismatch', undefined, {
            assetId: request.assetId,
            originalHash: asset.fileHash,
            newHash: fileInfo.hash,
          });
        }
      }

      const oldPath = asset.originalPath;

      // Update asset
      asset.originalPath = request.newPath;
      asset.fileSize = fileInfo.size;
      asset.modifiedAt = fileInfo.modifiedAt;
      asset.status = 'online';
      if (fileInfo.hash) {
        asset.fileHash = fileInfo.hash;
      }

      this.assets.set(request.assetId, asset);

      this.logger.info('relinked', undefined, {
        assetId: request.assetId,
        oldPath,
        newPath: request.newPath,
      });

      return {
        success: true,
        assetId: request.assetId,
        oldPath,
        newPath: request.newPath,
        hashMatch,
      };
    } catch (error: unknown) {
      const errorMessage = error instanceof Error ? error.message : 'Unknown error';
      return {
        success: false,
        assetId: request.assetId,
        oldPath: asset.originalPath,
        newPath: request.newPath,
        error: errorMessage,
      };
    }
  }

  /**
   * Bulk relink missing assets by searching a directory
   */
  async bulkRelink(request: BulkRelinkRequest): Promise<BulkRelinkResult> {
    const offlineAssets = Array.from(this.assets.values())
      .filter((a) => a.status === 'offline')
      .filter((a) => !request.assetIds || request.assetIds.includes(a.id));

    const results: RelinkResult[] = [];
    const stillMissing: string[] = [];

    for (const asset of offlineAssets) {
      // Search for file in directory
      const foundPath = await this.searchForFile(
        asset,
        request.searchDirectory,
        request.recursive,
        request.matchByName,
        request.matchByHash
      );

      if (foundPath) {
        const result = await this.relinkAsset({
          assetId: asset.id,
          newPath: foundPath,
          verifyHash: request.matchByHash,
        });
        results.push(result);
      } else {
        stillMissing.push(asset.id);
      }
    }

    return {
      found: results.filter((r) => r.success).length,
      notFound: stillMissing.length,
      relinked: results,
      stillMissing,
    };
  }

  /**
   * Collect/consolidate project files
   */
  async collectFiles(options: CollectFilesOptions): Promise<CollectFilesResult> {
    return post<CollectFilesResult>(`${API_BASE}/collect`, {
      assets: Array.from(this.assets.values()),
      options,
    });
  }

  /**
   * Generate proxy for an asset
   */
  async generateProxy(
    assetId: string,
    options: { resolution: { width: number; height: number }; format: string }
  ): Promise<void> {
    const asset = this.assets.get(assetId);
    if (!asset) {
      throw new Error('Asset not found');
    }

    const result = await post<{ proxyPath: string }>(`${API_BASE}/generate-proxy`, {
      assetId,
      sourcePath: asset.originalPath,
      ...options,
    });

    asset.proxy = {
      path: result.proxyPath,
      resolution: options.resolution,
      format: options.format,
      generatedAt: new Date().toISOString(),
      isActive: false,
    };

    this.assets.set(assetId, asset);
  }

  /**
   * Toggle proxy playback for asset
   */
  toggleProxy(assetId: string, useProxy: boolean): void {
    const asset = this.assets.get(assetId);
    if (!asset?.proxy) {
      throw new Error('Asset has no proxy');
    }

    asset.proxy.isActive = useProxy;
    this.assets.set(assetId, asset);

    // Notify playback system
    window.dispatchEvent(
      new CustomEvent<AssetProxyToggledEvent>('asset:proxyToggled', {
        detail: { assetId, useProxy },
      })
    );
  }

  /**
   * Get all assets
   */
  getAllAssets(): AssetReference[] {
    return Array.from(this.assets.values());
  }

  /**
   * Get asset by ID
   */
  getAsset(id: string): AssetReference | undefined {
    return this.assets.get(id);
  }

  /**
   * Get offline assets
   */
  getOfflineAssets(): AssetReference[] {
    return Array.from(this.assets.values()).filter((a) => a.status === 'offline');
  }

  /**
   * Get unused assets
   */
  getUnusedAssets(): AssetReference[] {
    return Array.from(this.assets.values()).filter((a) => !a.usage.isUsed);
  }

  /**
   * Get assets by status
   */
  getAssetsByStatus(status: AssetStatus): AssetReference[] {
    return Array.from(this.assets.values()).filter((a) => a.status === status);
  }

  /**
   * Update asset usage when used in timeline
   */
  updateAssetUsage(assetId: string, clipId: string, action: 'add' | 'remove'): void {
    const asset = this.assets.get(assetId);
    if (!asset) return;

    if (action === 'add') {
      if (!asset.usage.clipIds.includes(clipId)) {
        asset.usage.clipIds.push(clipId);
      }
    } else {
      asset.usage.clipIds = asset.usage.clipIds.filter((id) => id !== clipId);
    }

    asset.usage.timelineCount = asset.usage.clipIds.length;
    asset.usage.isUsed = asset.usage.clipIds.length > 0;
    asset.usage.lastUsedAt = new Date().toISOString();

    this.assets.set(assetId, asset);
  }

  /**
   * Remove an asset from tracking
   */
  removeAsset(assetId: string): boolean {
    return this.assets.delete(assetId);
  }

  /**
   * Clean up and stop status checks
   */
  cleanup(): void {
    if (this.statusCheckInterval) {
      clearInterval(this.statusCheckInterval);
      this.statusCheckInterval = null;
    }
    this.assets.clear();
  }

  /**
   * Export assets for project serialization
   */
  exportAssets(): AssetReference[] {
    return Array.from(this.assets.values());
  }

  // Private helper methods

  private startStatusChecks(): void {
    // Stop any existing interval
    if (this.statusCheckInterval) {
      clearInterval(this.statusCheckInterval);
    }

    // Check asset status every 30 seconds, but skip if no assets are registered
    this.statusCheckInterval = setInterval(() => {
      // Skip status checks if no assets are registered
      if (this.assets.size === 0) {
        return;
      }

      this.checkAllAssetStatus().catch((error: unknown) => {
        this.logger.error(
          'Status check failed',
          error instanceof Error ? error : new Error(String(error))
        );
      });
    }, 30000);
  }

  private updateAssetStatus(id: string, status: AssetStatus): void {
    const asset = this.assets.get(id);
    if (asset) {
      const previousStatus = asset.status;
      asset.status = status;
      this.assets.set(id, asset);

      // Notify UI
      window.dispatchEvent(
        new CustomEvent<AssetStatusChangedEvent>('asset:statusChanged', {
          detail: { id, status },
        })
      );

      this.logger.info('status changed', undefined, {
        id,
        previousStatus,
        newStatus: status,
      });
    }
  }

  private async getFileInfo(path: string): Promise<FileInfo> {
    return post<FileInfo>(`${API_BASE}/file-info`, { path });
  }

  private async getMediaInfo(
    path: string,
    type: AssetReference['type']
  ): Promise<MediaInfo | undefined> {
    if (type !== 'video' && type !== 'audio' && type !== 'image') {
      return undefined;
    }

    try {
      return await post<MediaInfo>(`${API_BASE}/media-info`, { path });
    } catch {
      return undefined;
    }
  }

  private async searchForFile(
    asset: AssetReference,
    directory: string,
    recursive: boolean,
    matchByName: boolean,
    matchByHash: boolean
  ): Promise<string | null> {
    try {
      const result = await post<{ foundPath: string | null }>(`${API_BASE}/search`, {
        fileName: asset.name,
        fileHash: matchByHash ? asset.fileHash : undefined,
        directory,
        recursive,
        matchByName,
      });

      return result.foundPath;
    } catch {
      return null;
    }
  }

  private extractFileName(path: string): string {
    return path.split(/[/\\]/).pop() || path;
  }
}

// Export singleton instance
export const assetManager = new AssetManagerService();

// Export the class for testing purposes
export { AssetManagerService };
