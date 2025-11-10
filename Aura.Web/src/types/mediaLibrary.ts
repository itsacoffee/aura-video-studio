export enum MediaType {
  Video = 'Video',
  Image = 'Image',
  Audio = 'Audio',
  Document = 'Document',
  Other = 'Other',
}

export enum MediaSource {
  UserUpload = 'UserUpload',
  Generated = 'Generated',
  StockMedia = 'StockMedia',
  Imported = 'Imported',
}

export enum ProcessingStatus {
  Pending = 'Pending',
  Processing = 'Processing',
  Completed = 'Completed',
  Failed = 'Failed',
}

export interface MediaMetadata {
  width?: number;
  height?: number;
  duration?: number;
  framerate?: number;
  format?: string;
  codec?: string;
  bitrate?: number;
  channels?: number;
  sampleRate?: number;
  colorSpace?: string;
  additionalProperties?: Record<string, string>;
}

export interface MediaItemResponse {
  id: string;
  fileName: string;
  type: MediaType;
  source: MediaSource;
  fileSize: number;
  description?: string;
  thumbnailUrl?: string;
  previewUrl?: string;
  url: string;
  metadata?: MediaMetadata;
  processingStatus: ProcessingStatus;
  tags: string[];
  collectionId?: string;
  collectionName?: string;
  usageCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface MediaSearchRequest {
  searchTerm?: string;
  types?: MediaType[];
  sources?: MediaSource[];
  tags?: string[];
  collectionId?: string;
  createdAfter?: string;
  createdBefore?: string;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDescending?: boolean;
}

export interface MediaSearchResponse {
  items: MediaItemResponse[];
  totalItems: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface MediaCollectionResponse {
  id: string;
  name: string;
  description?: string;
  mediaCount: number;
  thumbnailUrl?: string;
  createdAt: string;
  updatedAt: string;
}

export interface MediaCollectionRequest {
  name: string;
  description?: string;
}

export interface MediaUploadRequest {
  fileName: string;
  type: MediaType;
  source?: MediaSource;
  description?: string;
  tags?: string[];
  collectionId?: string;
  generateThumbnail?: boolean;
  extractMetadata?: boolean;
}

export interface BulkMediaOperationRequest {
  mediaIds: string[];
  operation: 'Delete' | 'Move' | 'AddTags' | 'RemoveTags' | 'ChangeCollection';
  targetCollectionId?: string;
  tags?: string[];
}

export interface StorageStats {
  totalSizeBytes: number;
  quotaBytes: number;
  availableBytes: number;
  usagePercentage: number;
  totalFiles: number;
  filesByType: Record<MediaType, number>;
  sizeByType: Record<MediaType, number>;
}

export interface UploadSession {
  sessionId: string;
  fileName: string;
  totalSize: number;
  totalChunks: number;
  expiresAt: string;
}
