// Platform optimization and distribution types

export interface PlatformProfile {
  platformId: string;
  name: string;
  description: string;
  requirements: PlatformRequirements;
  bestPractices: PlatformBestPractices;
  algorithmFactors: PlatformAlgorithmFactors;
}

export interface PlatformRequirements {
  supportedAspectRatios: AspectRatioSpec[];
  video: VideoSpecs;
  thumbnail: ThumbnailSpecs;
  metadata: MetadataLimits;
  supportedFormats: string[];
}

export interface AspectRatioSpec {
  ratio: string;
  width: number;
  height: number;
  isPreferred: boolean;
  useCase: string;
}

export interface VideoSpecs {
  minDurationSeconds: number;
  maxDurationSeconds: number;
  optimalMinDurationSeconds: number;
  optimalMaxDurationSeconds: number;
  maxFileSizeBytes: number;
  recommendedCodecs: string[];
  maxBitrate: number;
  recommendedBitrate: number;
  requiredFrameRates: string[];
}

export interface ThumbnailSpecs {
  width: number;
  height: number;
  minWidth: number;
  minHeight: number;
  maxFileSizeBytes: number;
  supportedFormats: string[];
  textOverlayRecommended: boolean;
  safeAreaDescription: string;
}

export interface MetadataLimits {
  titleMaxLength: number;
  descriptionMaxLength: number;
  maxTags: number;
  maxHashtags: number;
  hashtagMaxLength: number;
}

export interface PlatformBestPractices {
  hookDurationSeconds: number;
  hookStrategy: string;
  contentPacing: string;
  captionsRequired: boolean;
  musicImportant: boolean;
  textOverlayEffective: boolean;
  toneAndStyle: string;
  contentStrategies: string[];
  optimalPostingTimes: string;
}

export interface PlatformAlgorithmFactors {
  factors: RankingFactor[];
  algorithmType: string;
  favorsNewContent: boolean;
  typicalViralTimeframeHours: number;
}

export interface RankingFactor {
  name: string;
  description: string;
  weight: number;
}

export interface PlatformOptimizationRequest {
  sourceVideoPath: string;
  targetPlatform: string;
  preferredAspectRatio?: string;
  autoCrop?: boolean;
  optimizeMetadata?: boolean;
  generateThumbnail?: boolean;
}

export interface PlatformOptimizationResult {
  optimizedVideoPath: string;
  thumbnailPath: string;
  metadata: OptimizedMetadata;
  appliedOptimizations: string[];
  technicalSpecs: Record<string, string>;
}

export interface OptimizedMetadata {
  title: string;
  description: string;
  tags: string[];
  hashtags: string[];
  category: string;
  callToAction: string;
  customFields: Record<string, any>;
}

export interface MetadataGenerationRequest {
  platform: string;
  videoTitle: string;
  videoDescription: string;
  keywords: string[];
  targetAudience: string;
  contentType: string;
}

export interface ThumbnailSuggestionRequest {
  platform: string;
  videoContent: string;
  targetEmotion: string;
  includeText?: boolean;
  keyElements: string[];
}

export interface ThumbnailConcept {
  conceptId: string;
  description: string;
  composition: string;
  textOverlay: string;
  colorScheme: string;
  predictedCTR: number;
  designElements: string[];
}

export interface KeywordResearchRequest {
  topic: string;
  platform: string;
  language?: string;
  includeLongTail?: boolean;
}

export interface KeywordResearchResult {
  keywords: KeywordData[];
  clusters: KeywordCluster[];
  trendingTerms: string[];
}

export interface KeywordData {
  keyword: string;
  searchVolume: number;
  difficulty: string;
  relevance: number;
  relatedTerms: string[];
  searchIntent: string;
}

export interface KeywordCluster {
  clusterName: string;
  keywords: string[];
  intent: string;
}

export interface OptimalPostingTimeRequest {
  platform: string;
  timezone?: string;
  targetRegions: string[];
  contentType: string;
}

export interface OptimalPostingTimeResult {
  recommendedTimes: PostingTimeSlot[];
  activityPatterns: Record<string, string>;
  reasoning: string;
}

export interface PostingTimeSlot {
  day: number; // 0-6 (Sunday-Saturday)
  hour: number;
  minute: number;
  engagementScore: number;
  timezone: string;
}

export interface MultiPlatformExportRequest {
  sourceVideoPath: string;
  targetPlatforms: string[];
  optimizeForEach?: boolean;
  generateMetadata?: boolean;
  generateThumbnails?: boolean;
}

export interface MultiPlatformExportResult {
  exports: Record<string, PlatformExport>;
  status: string;
  warnings: string[];
}

export interface PlatformExport {
  platform: string;
  videoPath: string;
  thumbnailPath: string;
  metadata: OptimizedMetadata;
  success: boolean;
  error?: string;
}

export interface PlatformTrend {
  trendId: string;
  platform: string;
  topic: string;
  category: string;
  popularityScore: number;
  startDate: string;
  duration: string;
  relatedHashtags: string[];
  popularCreators: string[];
}

export interface ContentAdaptationRequest {
  sourceVideoPath: string;
  sourcePlatform: string;
  targetPlatform: string;
  adaptPacing?: boolean;
  adaptHook?: boolean;
  adaptFormat?: boolean;
}

export interface ContentAdaptationResult {
  adaptedVideoPath: string;
  changesApplied: string[];
  adaptationStrategy: string;
  recommendations: Record<string, string>;
}

// Helper types
export const SUPPORTED_PLATFORMS = [
  'youtube',
  'tiktok',
  'instagram-reels',
  'instagram-feed',
  'youtube-shorts',
  'linkedin',
  'twitter',
  'facebook',
] as const;

export type SupportedPlatform = (typeof SUPPORTED_PLATFORMS)[number];

export const PLATFORM_DISPLAY_NAMES: Record<SupportedPlatform, string> = {
  youtube: 'YouTube',
  tiktok: 'TikTok',
  'instagram-reels': 'Instagram Reels',
  'instagram-feed': 'Instagram Feed',
  'youtube-shorts': 'YouTube Shorts',
  linkedin: 'LinkedIn',
  twitter: 'Twitter/X',
  facebook: 'Facebook',
};

export const PLATFORM_ICONS: Record<SupportedPlatform, string> = {
  youtube: '▶️',
  tiktok: '🎵',
  'instagram-reels': '📸',
  'instagram-feed': '📷',
  'youtube-shorts': '🎬',
  linkedin: '💼',
  twitter: '🐦',
  facebook: '👥',
};

export const TARGET_EMOTIONS = [
  'exciting',
  'calm',
  'professional',
  'urgent',
  'trustworthy',
  'energetic',
  'inspiring',
  'educational',
] as const;

export type TargetEmotion = (typeof TARGET_EMOTIONS)[number];
