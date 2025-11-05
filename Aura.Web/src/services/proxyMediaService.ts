/**
 * Proxy Media Service
 *
 * Manages proxy media generation and switching between source and proxy media
 * for improved preview performance
 */

import { apiUrl } from '../config/api';

export interface ProxyMediaMetadata {
  id: string;
  sourcePath: string;
  proxyPath: string;
  quality: 'Draft' | 'Preview' | 'High';
  status: 'NotStarted' | 'Queued' | 'Processing' | 'Completed' | 'Failed';
  createdAt: string;
  lastAccessedAt: string;
  fileSizeBytes: number;
  sourceFileSizeBytes: number;
  width: number;
  height: number;
  bitrateKbps: number;
  errorMessage?: string;
  progressPercent: number;
}

export interface GenerateProxyRequest {
  sourcePath: string;
  quality?: 'Draft' | 'Preview' | 'High';
  backgroundGeneration?: boolean;
  priority?: number;
  overwrite?: boolean;
}

export interface ProxyCacheStats {
  totalProxies: number;
  totalCacheSizeBytes: number;
  totalSourceSizeBytes: number;
  compressionRatio: number;
  maxCacheSizeBytes: number;
  cacheUsagePercent: number;
  isOverLimit: boolean;
}

export class ProxyMediaService {
  private static instance: ProxyMediaService;
  private useProxyMode: boolean = true;
  private proxies: Map<string, ProxyMediaMetadata> = new Map();

  private constructor() {
    this.loadSettings();
  }

  public static getInstance(): ProxyMediaService {
    if (!ProxyMediaService.instance) {
      ProxyMediaService.instance = new ProxyMediaService();
    }
    return ProxyMediaService.instance;
  }

  /**
   * Generate proxy media for a source file
   */
  public async generateProxy(request: GenerateProxyRequest): Promise<ProxyMediaMetadata> {
    const response = await fetch(apiUrl('/api/proxy/generate'), {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        sourcePath: request.sourcePath,
        quality: request.quality || 'Preview',
        backgroundGeneration: request.backgroundGeneration ?? true,
        priority: request.priority ?? 0,
        overwrite: request.overwrite ?? false,
      }),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ error: 'Failed to generate proxy' }));
      throw new Error(error.error || 'Failed to generate proxy');
    }

    const metadata: ProxyMediaMetadata = await response.json();
    this.proxies.set(this.getProxyKey(request.sourcePath, metadata.quality), metadata);
    return metadata;
  }

  /**
   * Get proxy metadata for a source file
   */
  public async getProxyMetadata(
    sourcePath: string,
    quality: 'Draft' | 'Preview' | 'High' = 'Preview'
  ): Promise<ProxyMediaMetadata | null> {
    const key = this.getProxyKey(sourcePath, quality);
    if (this.proxies.has(key)) {
      return this.proxies.get(key)!;
    }

    try {
      const response = await fetch(
        apiUrl(
          `/api/proxy/metadata?sourcePath=${encodeURIComponent(sourcePath)}&quality=${quality}`
        )
      );

      if (!response.ok) {
        return null;
      }

      const metadata: ProxyMediaMetadata = await response.json();
      this.proxies.set(key, metadata);
      return metadata;
    } catch (error) {
      console.error('Error fetching proxy metadata:', error);
      return null;
    }
  }

  /**
   * Check if proxy exists for source file
   */
  public async proxyExists(
    sourcePath: string,
    quality: 'Draft' | 'Preview' | 'High' = 'Preview'
  ): Promise<boolean> {
    try {
      const response = await fetch(
        apiUrl(`/api/proxy/exists?sourcePath=${encodeURIComponent(sourcePath)}&quality=${quality}`)
      );

      if (!response.ok) {
        return false;
      }

      const result = await response.json();
      return result.exists === true;
    } catch (error) {
      console.error('Error checking proxy existence:', error);
      return false;
    }
  }

  /**
   * Get all proxies
   */
  public async getAllProxies(): Promise<ProxyMediaMetadata[]> {
    try {
      const response = await fetch(apiUrl('/api/proxy/all'));

      if (!response.ok) {
        throw new Error('Failed to fetch proxies');
      }

      const proxies: ProxyMediaMetadata[] = await response.json();
      proxies.forEach((p) => {
        this.proxies.set(this.getProxyKey(p.sourcePath, p.quality), p);
      });
      return proxies;
    } catch (error) {
      console.error('Error fetching all proxies:', error);
      return [];
    }
  }

  /**
   * Delete proxy for source file
   */
  public async deleteProxy(
    sourcePath: string,
    quality: 'Draft' | 'Preview' | 'High' = 'Preview'
  ): Promise<void> {
    await fetch(
      apiUrl(`/api/proxy?sourcePath=${encodeURIComponent(sourcePath)}&quality=${quality}`),
      { method: 'DELETE' }
    );

    const key = this.getProxyKey(sourcePath, quality);
    this.proxies.delete(key);
  }

  /**
   * Clear all proxies from cache
   */
  public async clearAllProxies(): Promise<void> {
    await fetch(apiUrl('/api/proxy/clear'), { method: 'POST' });
    this.proxies.clear();
  }

  /**
   * Get cache statistics
   */
  public async getCacheStats(): Promise<ProxyCacheStats> {
    const response = await fetch(apiUrl('/api/proxy/stats'));

    if (!response.ok) {
      throw new Error('Failed to fetch cache stats');
    }

    return await response.json();
  }

  /**
   * Get effective media path (proxy or source based on settings)
   */
  public async getEffectiveMediaPath(sourcePath: string): Promise<string> {
    if (!this.useProxyMode) {
      return sourcePath;
    }

    const metadata = await this.getProxyMetadata(sourcePath);
    if (metadata && metadata.status === 'Completed') {
      return metadata.proxyPath;
    }

    return sourcePath;
  }

  /**
   * Set whether to use proxy mode
   */
  public setUseProxyMode(useProxy: boolean): void {
    this.useProxyMode = useProxy;
    this.saveSettings();
  }

  /**
   * Check if proxy mode is enabled
   */
  public isProxyModeEnabled(): boolean {
    return this.useProxyMode;
  }

  /**
   * Toggle proxy mode
   */
  public toggleProxyMode(): boolean {
    this.useProxyMode = !this.useProxyMode;
    this.saveSettings();
    return this.useProxyMode;
  }

  /**
   * Clear internal cache (for testing)
   */
  public clearInternalCache(): void {
    this.proxies.clear();
  }

  /**
   * Set maximum cache size in bytes
   */
  public async setMaxCacheSize(maxSizeBytes: number): Promise<void> {
    await fetch(apiUrl('/api/proxy/cache-limit'), {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ maxSizeBytes }),
    });
  }

  /**
   * Get maximum cache size in bytes
   */
  public async getMaxCacheSize(): Promise<number> {
    const response = await fetch(apiUrl('/api/proxy/cache-limit'));
    if (!response.ok) {
      throw new Error('Failed to get cache limit');
    }
    const result = await response.json();
    return result.maxSizeBytes;
  }

  /**
   * Manually trigger LRU eviction
   */
  public async triggerEviction(): Promise<void> {
    const response = await fetch(apiUrl('/api/proxy/evict'), {
      method: 'POST',
    });

    if (!response.ok) {
      throw new Error('Failed to trigger eviction');
    }
  }

  private getProxyKey(sourcePath: string, quality: string): string {
    return `${sourcePath}:${quality}`;
  }

  private loadSettings(): void {
    try {
      const stored = localStorage.getItem('proxyMediaSettings');
      if (stored) {
        const settings = JSON.parse(stored);
        this.useProxyMode = settings.useProxyMode ?? true;
      }
    } catch (error) {
      console.error('Error loading proxy settings:', error);
    }
  }

  private saveSettings(): void {
    try {
      localStorage.setItem(
        'proxyMediaSettings',
        JSON.stringify({
          useProxyMode: this.useProxyMode,
        })
      );
    } catch (error) {
      console.error('Error saving proxy settings:', error);
    }
  }
}

export const proxyMediaService = ProxyMediaService.getInstance();
