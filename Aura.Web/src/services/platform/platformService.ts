import type {
  PlatformProfile,
  PlatformOptimizationRequest,
  PlatformOptimizationResult,
  MetadataGenerationRequest,
  OptimizedMetadata,
  ThumbnailSuggestionRequest,
  ThumbnailConcept,
  KeywordResearchRequest,
  KeywordResearchResult,
  OptimalPostingTimeRequest,
  OptimalPostingTimeResult,
  ContentAdaptationRequest,
  ContentAdaptationResult,
  MultiPlatformExportRequest,
  MultiPlatformExportResult,
  PlatformTrend,
} from '../../types/platform';
import apiClient from '../api/apiClient';

/**
 * Service for platform optimization API calls
 */
class PlatformService {
  private readonly baseUrl = '/api/platform';

  /**
   * Get all available platform profiles
   */
  async getAllPlatforms(): Promise<PlatformProfile[]> {
    const response = await apiClient.get<PlatformProfile[]>(`${this.baseUrl}/profiles`);
    return response.data;
  }

  /**
   * Get platform requirements by ID
   */
  async getPlatformRequirements(platform: string): Promise<PlatformProfile> {
    const response = await apiClient.get<PlatformProfile>(
      `${this.baseUrl}/requirements/${platform}`
    );
    return response.data;
  }

  /**
   * Optimize video for a specific platform
   */
  async optimizeForPlatform(
    request: PlatformOptimizationRequest
  ): Promise<PlatformOptimizationResult> {
    const response = await apiClient.post<PlatformOptimizationResult>(
      `${this.baseUrl}/optimize`,
      request
    );
    return response.data;
  }

  /**
   * Generate platform-optimized metadata
   */
  async generateMetadata(request: MetadataGenerationRequest): Promise<OptimizedMetadata> {
    const response = await apiClient.post<OptimizedMetadata>(
      `${this.baseUrl}/metadata/generate`,
      request
    );
    return response.data;
  }

  /**
   * Suggest thumbnail concepts
   */
  async suggestThumbnails(request: ThumbnailSuggestionRequest): Promise<ThumbnailConcept[]> {
    const response = await apiClient.post<ThumbnailConcept[]>(
      `${this.baseUrl}/thumbnail/suggest`,
      request
    );
    return response.data;
  }

  /**
   * Generate thumbnail image
   */
  async generateThumbnail(request: ThumbnailSuggestionRequest): Promise<{
    message: string;
    thumbnailPath: string;
    concept: string;
  }> {
    const response = await apiClient.post<{
      message: string;
      thumbnailPath: string;
      concept: string;
    }>(`${this.baseUrl}/thumbnail/generate`, request);
    return response.data;
  }

  /**
   * Research keywords for a topic
   */
  async researchKeywords(request: KeywordResearchRequest): Promise<KeywordResearchResult> {
    const response = await apiClient.post<KeywordResearchResult>(
      `${this.baseUrl}/keywords/research`,
      request
    );
    return response.data;
  }

  /**
   * Get optimal posting times
   */
  async getOptimalPostingTimes(
    request: OptimalPostingTimeRequest
  ): Promise<OptimalPostingTimeResult> {
    const response = await apiClient.post<OptimalPostingTimeResult>(
      `${this.baseUrl}/schedule/optimal`,
      request
    );
    return response.data;
  }

  /**
   * Adapt content for different platform
   */
  async adaptContent(request: ContentAdaptationRequest): Promise<ContentAdaptationResult> {
    const response = await apiClient.post<ContentAdaptationResult>(
      `${this.baseUrl}/adapt-content`,
      request
    );
    return response.data;
  }

  /**
   * Get current platform trends
   */
  async getPlatformTrends(platform: string): Promise<PlatformTrend[]> {
    const response = await apiClient.get<PlatformTrend[]>(`${this.baseUrl}/trends/${platform}`);
    return response.data;
  }

  /**
   * Export for multiple platforms at once
   */
  async multiPlatformExport(
    request: MultiPlatformExportRequest
  ): Promise<MultiPlatformExportResult> {
    const response = await apiClient.post<MultiPlatformExportResult>(
      `${this.baseUrl}/multi-export`,
      request
    );
    return response.data;
  }
}

export const platformService = new PlatformService();
export default platformService;
