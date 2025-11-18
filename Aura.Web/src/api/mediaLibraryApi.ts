import type {
  MediaSearchRequest,
  MediaSearchResponse,
  MediaItemResponse,
  MediaUploadRequest,
  MediaCollectionResponse,
  MediaCollectionRequest,
  BulkMediaOperationRequest,
  StorageStats,
  UploadSession,
} from '../types/mediaLibrary';

const API_BASE = '/api/media';

class MediaLibraryApi {
  async searchMedia(request: MediaSearchRequest): Promise<MediaSearchResponse> {
    const response = await fetch(`${API_BASE}/search`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error(`Failed to search media: ${response.statusText}`);
    }

    return response.json();
  }

  async getMedia(id: string): Promise<MediaItemResponse> {
    const response = await fetch(`${API_BASE}/${id}`);

    if (!response.ok) {
      throw new Error(`Failed to get media: ${response.statusText}`);
    }

    return response.json();
  }

  async uploadMedia(file: File, request: MediaUploadRequest): Promise<MediaItemResponse> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('type', request.type);

    if (request.description) {
      formData.append('description', request.description);
    }

    if (request.tags && request.tags.length > 0) {
      formData.append('tags', request.tags.join(','));
    }

    if (request.collectionId) {
      formData.append('collectionId', request.collectionId);
    }

    formData.append('generateThumbnail', String(request.generateThumbnail ?? true));
    formData.append('extractMetadata', String(request.extractMetadata ?? true));

    const response = await fetch(`${API_BASE}/upload`, {
      method: 'POST',
      body: formData,
    });

    if (!response.ok) {
      throw new Error(`Failed to upload media: ${response.statusText}`);
    }

    return response.json();
  }

  async updateMedia(id: string, request: MediaUploadRequest): Promise<MediaItemResponse> {
    const response = await fetch(`${API_BASE}/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error(`Failed to update media: ${response.statusText}`);
    }

    return response.json();
  }

  async deleteMedia(id: string): Promise<void> {
    const response = await fetch(`${API_BASE}/${id}`, {
      method: 'DELETE',
    });

    if (!response.ok) {
      throw new Error(`Failed to delete media: ${response.statusText}`);
    }
  }

  async bulkOperation(request: BulkMediaOperationRequest): Promise<void> {
    const response = await fetch(`${API_BASE}/bulk`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error(`Failed to perform bulk operation: ${response.statusText}`);
    }
  }

  async getCollections(): Promise<MediaCollectionResponse[]> {
    const response = await fetch(`${API_BASE}/collections`);

    if (!response.ok) {
      throw new Error(`Failed to get collections: ${response.statusText}`);
    }

    return response.json();
  }

  async createCollection(request: MediaCollectionRequest): Promise<MediaCollectionResponse> {
    const response = await fetch(`${API_BASE}/collections`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error(`Failed to create collection: ${response.statusText}`);
    }

    return response.json();
  }

  async updateCollection(
    id: string,
    request: MediaCollectionRequest
  ): Promise<MediaCollectionResponse> {
    const response = await fetch(`${API_BASE}/collections/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error(`Failed to update collection: ${response.statusText}`);
    }

    return response.json();
  }

  async deleteCollection(id: string): Promise<void> {
    const response = await fetch(`${API_BASE}/collections/${id}`, {
      method: 'DELETE',
    });

    if (!response.ok) {
      throw new Error(`Failed to delete collection: ${response.statusText}`);
    }
  }

  async getTags(): Promise<string[]> {
    const response = await fetch(`${API_BASE}/tags`);

    if (!response.ok) {
      throw new Error(`Failed to get tags: ${response.statusText}`);
    }

    return response.json();
  }

  async getStorageStats(): Promise<StorageStats> {
    const response = await fetch(`${API_BASE}/stats`);

    if (!response.ok) {
      throw new Error(`Failed to get storage stats: ${response.statusText}`);
    }

    return response.json();
  }

  async initiateChunkedUpload(
    fileName: string,
    totalSize: number,
    totalChunks: number
  ): Promise<UploadSession> {
    const response = await fetch(`${API_BASE}/upload/initiate`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ fileName, totalSize, totalChunks }),
    });

    if (!response.ok) {
      throw new Error(`Failed to initiate upload: ${response.statusText}`);
    }

    return response.json();
  }

  async uploadChunk(sessionId: string, chunkIndex: number, chunk: Blob): Promise<void> {
    const formData = new FormData();
    formData.append('chunk', chunk);

    const response = await fetch(`${API_BASE}/upload/${sessionId}/chunk/${chunkIndex}`, {
      method: 'POST',
      body: formData,
    });

    if (!response.ok) {
      throw new Error(`Failed to upload chunk: ${response.statusText}`);
    }
  }

  async completeChunkedUpload(
    sessionId: string,
    request: MediaUploadRequest
  ): Promise<MediaItemResponse> {
    const response = await fetch(`${API_BASE}/upload/${sessionId}/complete`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error(`Failed to complete upload: ${response.statusText}`);
    }

    return response.json();
  }
}

export const mediaLibraryApi = new MediaLibraryApi();
