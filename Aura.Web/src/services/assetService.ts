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

    const response = await fetch(`${API_BASE}?${params}`);
    if (!response.ok) throw new Error('Failed to fetch assets');
    return response.json();
  },

  /**
   * Get asset by ID
   */
  async getAsset(id: string): Promise<Asset> {
    const response = await fetch(`${API_BASE}/${id}`);
    if (!response.ok) throw new Error('Failed to fetch asset');
    return response.json();
  },

  /**
   * Upload asset to library
   */
  async uploadAsset(file: File, type?: string): Promise<Asset> {
    const formData = new FormData();
    formData.append('file', file);
    if (type) formData.append('type', type);

    const response = await fetch(`${API_BASE}/upload`, {
      method: 'POST',
      body: formData,
    });

    if (!response.ok) throw new Error('Failed to upload asset');
    return response.json();
  },

  /**
   * Add tags to an asset
   */
  async addTags(assetId: string, tags: string[]): Promise<Asset> {
    const response = await fetch(`${API_BASE}/${assetId}/tags`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(tags),
    });

    if (!response.ok) throw new Error('Failed to add tags');
    return response.json();
  },

  /**
   * Delete an asset
   */
  async deleteAsset(assetId: string, deleteFromDisk = false): Promise<void> {
    const params = new URLSearchParams();
    params.append('deleteFromDisk', deleteFromDisk.toString());

    const response = await fetch(`${API_BASE}/${assetId}?${params}`, {
      method: 'DELETE',
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.error || 'Failed to delete asset');
    }
  },

  /**
   * Search stock images
   */
  async searchStockImages(query: string, count = 20): Promise<StockImage[]> {
    const params = new URLSearchParams();
    params.append('query', query);
    params.append('count', count.toString());

    const response = await fetch(`${API_BASE}/stock/search?${params}`);
    if (!response.ok) throw new Error('Failed to search stock images');
    return response.json();
  },

  /**
   * Download and add stock image to library
   */
  async downloadStockImage(request: StockImageDownloadRequest): Promise<Asset> {
    const response = await fetch(`${API_BASE}/stock/download`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });

    if (!response.ok) throw new Error('Failed to download stock image');
    return response.json();
  },

  /**
   * Generate AI image
   */
  async generateAIImage(request: AIImageGenerationRequest): Promise<Asset> {
    const response = await fetch(`${API_BASE}/ai/generate`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.error || 'Failed to generate AI image');
    }
    return response.json();
  },

  /**
   * Get all collections
   */
  async getCollections(): Promise<AssetCollection[]> {
    const response = await fetch(`${API_BASE}/collections`);
    if (!response.ok) throw new Error('Failed to fetch collections');
    return response.json();
  },

  /**
   * Create a collection
   */
  async createCollection(request: CreateCollectionRequest): Promise<AssetCollection> {
    const response = await fetch(`${API_BASE}/collections`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });

    if (!response.ok) throw new Error('Failed to create collection');
    return response.json();
  },

  /**
   * Add asset to collection
   */
  async addToCollection(collectionId: string, assetId: string): Promise<void> {
    const response = await fetch(
      `${API_BASE}/collections/${collectionId}/assets/${assetId}`,
      { method: 'POST' }
    );

    if (!response.ok) throw new Error('Failed to add asset to collection');
  },
};
