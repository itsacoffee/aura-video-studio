/**
 * Asset library types
 */

export enum AssetType {
  Image = 'Image',
  Video = 'Video',
  Audio = 'Audio',
}

export enum AssetSource {
  Uploaded = 'Uploaded',
  StockPexels = 'StockPexels',
  StockPixabay = 'StockPixabay',
  AIGenerated = 'AIGenerated',
}

export interface AssetMetadata {
  width?: number;
  height?: number;
  duration?: string;
  fileSizeBytes?: number;
  format?: string;
  codec?: string;
  bitrate?: number;
  sampleRate?: number;
  extra?: Record<string, string>;
}

export interface AssetTag {
  name: string;
  confidence: number;
}

export interface Asset {
  id: string;
  type: AssetType;
  filePath: string;
  thumbnailPath?: string;
  title: string;
  description?: string;
  tags: AssetTag[];
  source: AssetSource;
  metadata: AssetMetadata;
  dateAdded: string;
  dateModified: string;
  usageCount: number;
  collections: string[];
  dominantColor?: string;
}

export interface AssetCollection {
  id: string;
  name: string;
  description?: string;
  color: string;
  dateCreated: string;
  dateModified: string;
  assetIds: string[];
}

export interface AssetSearchFilters {
  type?: AssetType;
  tags?: string[];
  startDate?: string;
  endDate?: string;
  minWidth?: number;
  maxWidth?: number;
  minHeight?: number;
  maxHeight?: number;
  minDuration?: string;
  maxDuration?: string;
  source?: AssetSource;
  collections?: string[];
  usedInTimeline?: boolean;
}

export interface AssetSearchResult {
  assets: Asset[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface StockImage {
  thumbnailUrl: string;
  fullSizeUrl: string;
  previewUrl: string;
  photographer?: string;
  photographerUrl?: string;
  source: string;
  width: number;
  height: number;
}

export interface AIImageGenerationRequest {
  prompt: string;
  negativePrompt?: string;
  style?: string;
  size?: string;
  steps?: number;
  cfgScale?: number;
  seed?: number;
}

export interface CreateCollectionRequest {
  name: string;
  description?: string;
  color?: string;
}

export interface StockImageDownloadRequest {
  imageUrl: string;
  source?: string;
  photographer?: string;
}
