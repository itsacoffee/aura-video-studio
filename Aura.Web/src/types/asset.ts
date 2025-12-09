/**
 * Comprehensive asset/media management types
 * Supports media relinking, asset collection/consolidation, proxy workflow, and offline media handling
 */

/**
 * Asset status indicating availability and state
 */
export type AssetStatus =
  | 'online' // File exists and is accessible
  | 'offline' // File not found at expected path
  | 'modified' // File exists but hash doesn't match
  | 'embedded' // Asset is embedded in project
  | 'proxy-only'; // Only proxy available, original offline

/**
 * Proxy information for an asset
 */
export interface ProxyInfo {
  /** Proxy file path */
  path: string;

  /** Proxy resolution */
  resolution: { width: number; height: number };

  /** Proxy format */
  format: string;

  /** When proxy was generated */
  generatedAt: string;

  /** Whether proxy is currently being used for playback */
  isActive: boolean;
}

/**
 * Embedded asset data for packaged projects
 */
export interface EmbeddedAssetData {
  /** Base64 encoded data (for small files) */
  data?: string;

  /** Chunk references (for large files stored separately) */
  chunks?: string[];

  /** Compression method used */
  compression?: 'none' | 'gzip' | 'lz4';
}

/**
 * Media-specific metadata
 */
export interface MediaInfo {
  /** Duration in seconds (for video/audio) */
  duration?: number;

  /** Resolution (for video/image) */
  resolution?: { width: number; height: number };

  /** Frame rate (for video) */
  frameRate?: number;

  /** Codec information */
  codec?: string;

  /** Bit rate */
  bitRate?: number;

  /** Sample rate (for audio) */
  sampleRate?: number;

  /** Number of channels (for audio) */
  channels?: number;

  /** Color space (for video/image) */
  colorSpace?: string;

  /** Has alpha channel */
  hasAlpha?: boolean;
}

/**
 * Asset usage tracking information
 */
export interface AssetUsage {
  /** Number of times used in timeline */
  timelineCount: number;

  /** Clip IDs that reference this asset */
  clipIds: string[];

  /** Whether asset is used in current project */
  isUsed: boolean;

  /** Last time asset was used */
  lastUsedAt?: string;
}

/**
 * Comprehensive asset reference with full tracking information
 */
export interface AssetReference {
  /** Unique identifier for this asset */
  id: string;

  /** Display name */
  name: string;

  /** Asset type */
  type: 'video' | 'audio' | 'image' | 'subtitle' | 'document' | 'other';

  /** Original file path (absolute) */
  originalPath: string;

  /** Relative path from project file (for portability) */
  relativePath?: string;

  /** File hash for identity verification */
  fileHash?: string;

  /** File size in bytes */
  fileSize: number;

  /** MIME type */
  mimeType: string;

  /** Creation date */
  createdAt: string;

  /** Last modified date */
  modifiedAt: string;

  /** When asset was imported into project */
  importedAt: string;

  /** Asset status */
  status: AssetStatus;

  /** Proxy information if available */
  proxy?: ProxyInfo;

  /** Embedded data (for small assets or packaged projects) */
  embedded?: EmbeddedAssetData;

  /** Media-specific metadata */
  mediaInfo?: MediaInfo;

  /** Usage tracking */
  usage: AssetUsage;
}

/**
 * Asset collection/packaging options
 */
export interface CollectFilesOptions {
  /** Include original high-res files */
  includeOriginals: boolean;

  /** Include proxy files */
  includeProxies: boolean;

  /** Include unused assets */
  includeUnused: boolean;

  /** Transcode to specific format */
  transcode?: {
    format: string;
    quality: 'low' | 'medium' | 'high' | 'lossless';
  };

  /** Destination path */
  destination: string;

  /** Create ZIP archive */
  createArchive: boolean;

  /** Archive name */
  archiveName?: string;
}

/**
 * Result of a collect files operation
 */
export interface CollectFilesResult {
  success: boolean;
  collectedFiles: number;
  totalSize: number;
  outputPath: string;
  errors: string[];
  warnings: string[];
}

/**
 * Request to relink a single asset
 */
export interface RelinkRequest {
  assetId: string;
  newPath: string;
  verifyHash?: boolean;
}

/**
 * Result of a relink operation
 */
export interface RelinkResult {
  success: boolean;
  assetId: string;
  oldPath: string;
  newPath: string;
  hashMatch?: boolean;
  error?: string;
}

/**
 * Request to bulk relink missing assets
 */
export interface BulkRelinkRequest {
  /** Search in this directory for missing files */
  searchDirectory: string;

  /** Search subdirectories */
  recursive: boolean;

  /** Match by filename only (ignore path) */
  matchByName: boolean;

  /** Match by file hash */
  matchByHash: boolean;

  /** Only relink specific assets */
  assetIds?: string[];
}

/**
 * Result of a bulk relink operation
 */
export interface BulkRelinkResult {
  found: number;
  notFound: number;
  relinked: RelinkResult[];
  stillMissing: string[];
}

/**
 * File information returned from backend
 */
export interface FileInfo {
  size: number;
  mimeType: string;
  createdAt: string;
  modifiedAt: string;
  hash?: string;
  exists: boolean;
}

/**
 * Search result for finding files
 */
export interface FileSearchResult {
  foundPath: string | null;
  confidence: number;
  matchedBy: 'name' | 'hash' | 'both';
}
