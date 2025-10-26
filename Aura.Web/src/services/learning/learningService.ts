import type {
  PatternsResponse,
  InsightsResponse,
  AnalysisResponse,
  MaturityResponse,
  ConfidenceResponse,
  RankSuggestionsRequest,
  RankedSuggestion,
  ConfirmPreferenceRequest,
  InferredPreference,
  LearningAnalytics,
  DecisionStatistics,
} from '../../types/learning';
import apiClient from '../api/apiClient';

/**
 * Service for interacting with AI learning endpoints
 */
class LearningService {
  private baseUrl = '/api/learning';

  /**
   * Get identified patterns for a profile
   */
  async getPatterns(profileId: string): Promise<PatternsResponse> {
    const response = await apiClient.get<PatternsResponse>(`${this.baseUrl}/patterns/${profileId}`);
    return response.data;
  }

  /**
   * Get learning insights for a profile
   */
  async getInsights(profileId: string): Promise<InsightsResponse> {
    const response = await apiClient.get<InsightsResponse>(`${this.baseUrl}/insights/${profileId}`);
    return response.data;
  }

  /**
   * Trigger pattern analysis for a profile
   */
  async analyze(profileId: string): Promise<AnalysisResponse> {
    const response = await apiClient.post<AnalysisResponse>(`${this.baseUrl}/analyze`, {
      profileId,
    });
    return response.data;
  }

  /**
   * Get prediction statistics for a profile
   */
  async getPredictionStats(
    profileId: string
  ): Promise<{ success: boolean; profileId: string; statistics: DecisionStatistics[] }> {
    const response = await apiClient.get(`${this.baseUrl}/predictions/${profileId}`);
    return response.data;
  }

  /**
   * Rank suggestions by predicted acceptance
   */
  async rankSuggestions(request: RankSuggestionsRequest): Promise<{
    success: boolean;
    profileId: string;
    suggestionType: string;
    rankedSuggestions: RankedSuggestion[];
  }> {
    const response = await apiClient.post(`${this.baseUrl}/rank-suggestions`, request);
    return response.data;
  }

  /**
   * Get confidence score for a suggestion type
   */
  async getConfidenceScore(profileId: string, suggestionType: string): Promise<ConfidenceResponse> {
    const response = await apiClient.get<ConfidenceResponse>(
      `${this.baseUrl}/confidence/${profileId}/${suggestionType}`
    );
    return response.data;
  }

  /**
   * Reset learning data for a profile
   */
  async resetLearning(
    profileId: string
  ): Promise<{ success: boolean; profileId: string; message: string }> {
    const response = await apiClient.delete(`${this.baseUrl}/reset/${profileId}`);
    return response.data;
  }

  /**
   * Get learning maturity level for a profile
   */
  async getMaturityLevel(profileId: string): Promise<MaturityResponse> {
    const response = await apiClient.get<MaturityResponse>(`${this.baseUrl}/maturity/${profileId}`);
    return response.data;
  }

  /**
   * Confirm an inferred preference
   */
  async confirmPreference(
    request: ConfirmPreferenceRequest
  ): Promise<{ success: boolean; message: string }> {
    const response = await apiClient.post(`${this.baseUrl}/confirm-preference`, request);
    return response.data;
  }

  /**
   * Get inferred preferences for a profile
   */
  async getInferredPreferences(profileId: string): Promise<{
    success: boolean;
    profileId: string;
    preferences: InferredPreference[];
    count: number;
  }> {
    const response = await apiClient.get(`${this.baseUrl}/preferences/${profileId}`);
    return response.data;
  }

  /**
   * Get comprehensive learning analytics
   */
  async getAnalytics(profileId: string): Promise<{
    success: boolean;
    profileId: string;
    analytics: LearningAnalytics;
  }> {
    const response = await apiClient.get(`${this.baseUrl}/analytics/${profileId}`);
    return response.data;
  }
}

export const learningService = new LearningService();
export default learningService;
