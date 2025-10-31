import apiClient from '../api/apiClient';

export interface ProviderRecommendation {
  providerName: string;
  reasoning: string;
  qualityScore: number;
  estimatedCost: number;
  expectedLatencySeconds: number;
  isAvailable: boolean;
  healthStatus: 'Healthy' | 'Degraded' | 'Unhealthy' | 'Unknown';
  confidence: number;
}

export interface ProviderHealth {
  providerName: string;
  successRatePercent: number;
  averageLatencySeconds: number;
  totalRequests: number;
  consecutiveFailures: number;
  status: 'Healthy' | 'Degraded' | 'Unhealthy' | 'Unknown';
}

export interface CostTrackingSummary {
  totalMonthlyCost: number;
  costByProvider: Record<string, number>;
  costByOperation: Record<string, number>;
}

export interface ProviderProfile {
  name: string;
  description: string;
}

export type LlmOperationType =
  | 'ScriptGeneration'
  | 'ScriptRefinement'
  | 'VisualPrompts'
  | 'NarrationOptimization'
  | 'QuickOperations'
  | 'SceneAnalysis'
  | 'ContentComplexity'
  | 'NarrativeValidation';

class ProviderRecommendationService {
  private recommendationCache: Map<
    string,
    { recommendations: ProviderRecommendation[]; timestamp: number }
  > = new Map();
  private healthCache: { data: ProviderHealth[]; timestamp: number } | null = null;
  private readonly CACHE_TTL = 60000;

  /**
   * Get provider recommendations for a specific operation type
   */
  async getRecommendations(
    operationType: LlmOperationType,
    estimatedInputTokens: number = 1000
  ): Promise<ProviderRecommendation[]> {
    const cacheKey = `${operationType}_${estimatedInputTokens}`;
    const cached = this.recommendationCache.get(cacheKey);

    if (cached && Date.now() - cached.timestamp < this.CACHE_TTL) {
      return cached.recommendations;
    }

    try {
      const response = await apiClient.get<ProviderRecommendation[]>(
        `/api/providers/recommendations/${operationType}`,
        {
          params: { estimatedInputTokens },
        }
      );

      this.recommendationCache.set(cacheKey, {
        recommendations: response.data,
        timestamp: Date.now(),
      });

      return response.data;
    } catch (error: unknown) {
      console.error('Failed to get provider recommendations:', error);
      return [];
    }
  }

  /**
   * Get best single recommendation for an operation type
   */
  async getBestRecommendation(
    operationType: LlmOperationType,
    estimatedInputTokens: number = 1000
  ): Promise<ProviderRecommendation | null> {
    const recommendations = await this.getRecommendations(operationType, estimatedInputTokens);
    return recommendations.length > 0 ? recommendations[0] : null;
  }

  /**
   * Get health status of all providers
   */
  async getProviderHealth(): Promise<ProviderHealth[]> {
    if (this.healthCache && Date.now() - this.healthCache.timestamp < this.CACHE_TTL) {
      return this.healthCache.data;
    }

    try {
      const response = await apiClient.get<ProviderHealth[]>('/api/providers/health');

      this.healthCache = {
        data: response.data,
        timestamp: Date.now(),
      };

      return response.data;
    } catch (error: unknown) {
      console.error('Failed to get provider health:', error);
      return [];
    }
  }

  /**
   * Get cost tracking summary for current month
   */
  async getCostTracking(): Promise<CostTrackingSummary | null> {
    try {
      const response = await apiClient.get<CostTrackingSummary>('/api/providers/cost-tracking');
      return response.data;
    } catch (error: unknown) {
      console.error('Failed to get cost tracking:', error);
      return null;
    }
  }

  /**
   * Get available provider profiles
   */
  async getProviderProfiles(): Promise<ProviderProfile[]> {
    try {
      const response = await apiClient.get<ProviderProfile[]>('/api/providers/profiles');
      return response.data;
    } catch (error: unknown) {
      console.error('Failed to get provider profiles:', error);
      return [];
    }
  }

  /**
   * Test connection to a provider
   */
  async testProviderConnection(providerName: string, apiKey?: string): Promise<boolean> {
    try {
      const response = await apiClient.post('/api/providers/test-connection', {
        providerName,
        apiKey,
      });
      return response.status === 200;
    } catch (error: unknown) {
      console.error('Provider connection test failed:', error);
      return false;
    }
  }

  /**
   * Clear recommendation cache
   */
  clearCache(): void {
    this.recommendationCache.clear();
    this.healthCache = null;
  }

  /**
   * Get health status indicator for a provider
   */
  getHealthStatusIndicator(status: string): {
    color: string;
    icon: string;
    label: string;
  } {
    switch (status) {
      case 'Healthy':
        return { color: 'green', icon: '✓', label: 'Healthy' };
      case 'Degraded':
        return { color: 'yellow', icon: '⚠', label: 'Degraded' };
      case 'Unhealthy':
        return { color: 'red', icon: '✗', label: 'Unhealthy' };
      default:
        return { color: 'gray', icon: '?', label: 'Unknown' };
    }
  }

  /**
   * Format cost for display
   */
  formatCost(cost: number): string {
    if (cost === 0) {
      return 'Free';
    } else if (cost < 0.01) {
      return `$${cost.toFixed(4)}`;
    } else {
      return `$${cost.toFixed(3)}`;
    }
  }

  /**
   * Format latency for display
   */
  formatLatency(seconds: number): string {
    if (seconds < 5) {
      return 'Fast';
    } else if (seconds < 15) {
      return 'Moderate';
    } else {
      return 'Slow';
    }
  }
}

export const providerRecommendationService = new ProviderRecommendationService();
