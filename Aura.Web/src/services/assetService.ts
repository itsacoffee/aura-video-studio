/**
 * Asset library API service
 */

import {
  Asset,
  AssetSearchResult,
  AssetCollection,
  StockImage,
  AIImageGenerationRequest,
  CreateCollectionRequest,
  StockImageDownloadRequest,
} from '../types/assets';
import { get, post, del, uploadFile } from './api/apiClient';

const API_BASE = '/api/assets';

export const assetService = {
  /**
   * Get all assets with optional search
   */
  async getAssets(
    query?: string,
    type?: string,
    source?: string,
    page = 1,
    pageSize = 50,
    sortBy = 'dateAdded',
    sortDescending = true
  ): Promise<AssetSearchResult> {
    const params = new URLSearchParams();
    if (query) params.append('query', query);
    if (type) params.append('type', type);
    if (source) params.append('source', source);
    params.append('page', page.toString());
    params.append('pageSize', pageSize.toString());
    params.append('sortBy', sortBy);
    params.append('sortDescending', sortDescending.toString());

    return get<AssetSearchResult>(`${API_BASE}?${params}`);
  },

  /**
   * Get asset by ID
   */
  async getAsset(id: string): Promise<Asset> {
    return get<Asset>(`${API_BASE}/${id}`);
  },

  /**
   * Upload asset to library
   */
  async uploadAsset(
    file: File,
    type?: string,
    onProgress?: (progress: number) => void
  ): Promise<Asset> {
    const url = type ? `${API_BASE}/upload?type=${type}` : `${API_BASE}/upload`;
    return uploadFile<Asset>(url, file, onProgress);
  },

  /**
   * Add tags to an asset
   */
  async addTags(assetId: string, tags: string[]): Promise<Asset> {
    return post<Asset>(`${API_BASE}/${assetId}/tags`, tags);
  },

  /**
   * Delete an asset
   */
  async deleteAsset(assetId: string, deleteFromDisk = false): Promise<void> {
    const params = new URLSearchParams();
    params.append('deleteFromDisk', deleteFromDisk.toString());

    return del<void>(`${API_BASE}/${assetId}?${params}`);
  },

  /**
   * Search stock images with enhanced error handling
   */
  async searchStockImages(query: string, count = 20): Promise<StockImage[]> {
    const params = new URLSearchParams();
    params.append('query', query);
    params.append('count', count.toString());

    try {
      return await get<StockImage[]>(`${API_BASE}/stock/search?${params}`);
    } catch (error: unknown) {
      // Enhanced error handling with user-friendly messages
      const err = error as { response?: { status?: number }; message?: string };
      if (err.response?.status === 429) {
        throw new Error('Rate limit exceeded. Please try again in a few minutes.');
      } else if (err.response?.status === 401 || err.response?.status === 403) {
        throw new Error('API key invalid or not configured. Please check your settings.');
      } else if (err.message?.includes('rate limit')) {
        throw new Error('Stock image provider quota exceeded. Please try again later.');
      } else if (err.message?.includes('API key')) {
        throw new Error('Stock image API key not configured. Please add your API key in settings.');
      }
      throw error;
    }
  },

  /**
   * Download and add stock image to library with retry logic
   */
  async downloadStockImage(
    request: StockImageDownloadRequest,
    onProgress?: (progress: number) => void
  ): Promise<Asset> {
    const maxRetries = 3;
    let lastError: Error | null = null;

    for (let attempt = 1; attempt <= maxRetries; attempt++) {
      try {
        if (onProgress) {
          onProgress(attempt === 1 ? 0 : ((attempt - 1) / maxRetries) * 50);
        }

        const asset = await post<Asset>(`${API_BASE}/stock/download`, request);

        if (onProgress) {
          onProgress(100);
        }

        return asset;
      } catch (error: unknown) {
        lastError = error instanceof Error ? error : new Error(String(error));

        // Don't retry on client errors (4xx)
        const err = error as { response?: { status?: number } };
        if (err.response?.status && err.response.status >= 400 && err.response.status < 500) {
          throw error;
        }

        // Wait before retrying (exponential backoff)
        if (attempt < maxRetries) {
          await new Promise((resolve) => setTimeout(resolve, 1000 * attempt));
        }
      }
    }

    throw new Error(
      `Failed to download stock image after ${maxRetries} attempts: ${lastError?.message || 'Unknown error'}`
    );
  },

  /**
   * Get list of available stock providers with their status
   */
  async getStockProviders(): Promise<{
    providers: Array<{
      name: string;
      available: boolean;
      hasApiKey: boolean;
      quotaRemaining: number | null;
      quotaLimit: number | null;
      error: string | null;
    }>;
  }> {
    return get(`${API_BASE}/stock/providers`);
  },

  /**
   * Get quota status for a specific provider
   */
  async getStockQuota(provider: string): Promise<{
    provider: string;
    remaining: number;
    limit: number;
    resetTime: string | null;
  }> {
    return get(`${API_BASE}/stock/quota/${provider}`);
  },

  /**
   * Generate AI image
   */
  async generateAIImage(request: AIImageGenerationRequest): Promise<Asset> {
    return post<Asset>(`${API_BASE}/ai/generate`, request);
  },

  /**
   * Get all collections
   */
  async getCollections(): Promise<AssetCollection[]> {
    return get<AssetCollection[]>(`${API_BASE}/collections`);
  },

  /**
   * Create a collection
   */
  async createCollection(request: CreateCollectionRequest): Promise<AssetCollection> {
    return post<AssetCollection>(`${API_BASE}/collections`, request);
  },

  /**
   * Add asset to collection
   */
  async addToCollection(collectionId: string, assetId: string): Promise<void> {
    return post<void>(`${API_BASE}/collections/${collectionId}/assets/${assetId}`);
  },
};
