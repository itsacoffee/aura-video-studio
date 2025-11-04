import type {
  ExplainArtifactRequest,
  ExplainArtifactResponse,
  ImproveArtifactRequest,
  ImproveArtifactResponse,
  ConstrainedRegenerateRequest,
  ConstrainedRegenerateResponse,
  GuidedModeConfigDto,
  GuidedModeTelemetryDto,
  PromptDiffDto,
} from '../types/api-v1';
import apiClient from './api/apiClient';

/**
 * Service for guided mode operations
 * Handles API calls for explanations, improvements, and constrained regeneration
 */
class GuidedModeService {
  private readonly baseUrl = '/api';

  /**
   * Explain an artifact to the user
   */
  async explainArtifact(request: ExplainArtifactRequest): Promise<ExplainArtifactResponse> {
    const startTime = Date.now();
    try {
      const response = await apiClient.post<ExplainArtifactResponse>(
        `${this.baseUrl}/explain/artifact`,
        request
      );

      await this.trackTelemetry({
        featureUsed: 'explain',
        artifactType: request.artifactType,
        durationMs: Date.now() - startTime,
        success: response.data.success,
      });

      return response.data;
    } catch (error: unknown) {
      await this.trackTelemetry({
        featureUsed: 'explain',
        artifactType: request.artifactType,
        durationMs: Date.now() - startTime,
        success: false,
      });
      throw error;
    }
  }

  /**
   * Improve an artifact with specific action
   */
  async improveArtifact(request: ImproveArtifactRequest): Promise<ImproveArtifactResponse> {
    const startTime = Date.now();
    try {
      const response = await apiClient.post<ImproveArtifactResponse>(
        `${this.baseUrl}/explain/improve`,
        request
      );

      await this.trackTelemetry({
        featureUsed: 'improve',
        artifactType: request.artifactType,
        durationMs: Date.now() - startTime,
        success: response.data.success,
        metadata: {
          action: request.improvementAction,
          lockedSections: String(request.lockedSections?.length ?? 0),
        },
      });

      return response.data;
    } catch (error: unknown) {
      await this.trackTelemetry({
        featureUsed: 'improve',
        artifactType: request.artifactType,
        durationMs: Date.now() - startTime,
        success: false,
      });
      throw error;
    }
  }

  /**
   * Regenerate with constraints (locked sections)
   */
  async constrainedRegenerate(
    request: ConstrainedRegenerateRequest
  ): Promise<ConstrainedRegenerateResponse> {
    const startTime = Date.now();
    try {
      const response = await apiClient.post<ConstrainedRegenerateResponse>(
        `${this.baseUrl}/explain/regenerate`,
        request
      );

      await this.trackTelemetry({
        featureUsed: 'constrained_regenerate',
        artifactType: request.artifactType,
        durationMs: Date.now() - startTime,
        success: response.data.success,
        metadata: {
          regenerationType: request.regenerationType,
          lockedSections: String(request.lockedSections?.length ?? 0),
        },
      });

      return response.data;
    } catch (error: unknown) {
      await this.trackTelemetry({
        featureUsed: 'constrained_regenerate',
        artifactType: request.artifactType,
        durationMs: Date.now() - startTime,
        success: false,
      });
      throw error;
    }
  }

  /**
   * Get prompt diff preview before regeneration
   */
  async getPromptDiff(request: ConstrainedRegenerateRequest): Promise<PromptDiffDto> {
    const response = await apiClient.post<PromptDiffDto>(
      `${this.baseUrl}/explain/prompt-diff`,
      request
    );
    return response.data;
  }

  /**
   * Get guided mode configuration
   */
  async getConfig(): Promise<GuidedModeConfigDto> {
    const response = await apiClient.get<GuidedModeConfigDto>(`${this.baseUrl}/guidedmode/config`);
    return response.data;
  }

  /**
   * Update guided mode configuration
   */
  async updateConfig(config: GuidedModeConfigDto): Promise<GuidedModeConfigDto> {
    const response = await apiClient.post<{ success: boolean; config: GuidedModeConfigDto }>(
      `${this.baseUrl}/guidedmode/config`,
      config
    );
    return response.data.config;
  }

  /**
   * Get default configuration for experience level
   */
  async getDefaultConfig(experienceLevel: string): Promise<GuidedModeConfigDto> {
    const response = await apiClient.get<GuidedModeConfigDto>(
      `${this.baseUrl}/guidedmode/defaults/${experienceLevel}`
    );
    return response.data;
  }

  /**
   * Track guided mode feature usage telemetry
   */
  async trackTelemetry(telemetry: GuidedModeTelemetryDto): Promise<void> {
    try {
      await apiClient.post(`${this.baseUrl}/guidedmode/telemetry`, telemetry);
    } catch (error: unknown) {
      console.error('Failed to track telemetry:', error);
    }
  }

  /**
   * Track user feedback rating
   */
  async trackFeedback(
    featureUsed: string,
    artifactType: string,
    rating: 'positive' | 'negative'
  ): Promise<void> {
    await this.trackTelemetry({
      featureUsed,
      artifactType,
      durationMs: 0,
      success: true,
      feedbackRating: rating,
    });
  }
}

export const guidedModeService = new GuidedModeService();
