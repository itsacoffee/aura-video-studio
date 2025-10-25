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
  async uploadAsset(file: File, type?: string, onProgress?: (progress: number) => void): Promise<Asset> {
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
   * Search stock images
   */
  async searchStockImages(query: string, count = 20): Promise<StockImage[]> {
    const params = new URLSearchParams();
    params.append('query', query);
    params.append('count', count.toString());

    return get<StockImage[]>(`${API_BASE}/stock/search?${params}`);
  },

  /**
   * Download and add stock image to library
   */
  async downloadStockImage(request: StockImageDownloadRequest): Promise<Asset> {
    return post<Asset>(`${API_BASE}/stock/download`, request);
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
