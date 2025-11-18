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
   * Get enhanced cost tracking configuration
   */
  async getCostTrackingConfiguration(): Promise<CostTrackingConfiguration | null> {
    try {
      const response = await apiClient.get<CostTrackingConfiguration>(
        '/api/cost-tracking/configuration'
      );
      return response.data;
    } catch (error: unknown) {
      console.error('Failed to get cost tracking configuration:', error);
      return null;
    }
  }

  /**
   * Update cost tracking configuration
   */
  async updateCostTrackingConfiguration(config: CostTrackingConfiguration): Promise<boolean> {
    try {
      await apiClient.put('/api/cost-tracking/configuration', config);
      return true;
    } catch (error: unknown) {
      console.error('Failed to update cost tracking configuration:', error);
      return false;
    }
  }

  /**
   * Get spending report for a date range
   */
  async getSpendingReport(
    startDate?: Date,
    endDate?: Date,
    provider?: string
  ): Promise<SpendingReport | null> {
    try {
      const params: Record<string, string> = {};
      if (startDate) params.startDate = startDate.toISOString();
      if (endDate) params.endDate = endDate.toISOString();
      if (provider) params.provider = provider;

      const response = await apiClient.get<SpendingReport>('/api/cost-tracking/spending', {
        params,
      });
      return response.data;
    } catch (error: unknown) {
      console.error('Failed to get spending report:', error);
      return null;
    }
  }

  /**
   * Get current period spending
   */
  async getCurrentPeriodSpending(): Promise<CurrentPeriodSpending | null> {
    try {
      const response = await apiClient.get<CurrentPeriodSpending>(
        '/api/cost-tracking/current-period'
      );
      return response.data;
    } catch (error: unknown) {
      console.error('Failed to get current period spending:', error);
      return null;
    }
  }

  /**
   * Get provider pricing information
   */
  async getProviderPricing(): Promise<ProviderPricing[]> {
    try {
      const response = await apiClient.get<ProviderPricing[]>('/api/cost-tracking/pricing');
      return response.data;
    } catch (error: unknown) {
      console.error('Failed to get provider pricing:', error);
      return [];
    }
  }

  /**
   * Update provider pricing
   */
  async updateProviderPricing(providerName: string, pricing: ProviderPricing): Promise<boolean> {
    try {
      await apiClient.put(`/api/cost-tracking/pricing/${providerName}`, pricing);
      return true;
    } catch (error: unknown) {
      console.error('Failed to update provider pricing:', error);
      return false;
    }
  }

  /**
   * Check if an operation would exceed budget
   */
  async checkBudget(request: CostEstimateRequest): Promise<BudgetCheckResult | null> {
    try {
      const response = await apiClient.post<BudgetCheckResult>(
        '/api/cost-tracking/check-budget',
        request
      );
      return response.data;
    } catch (error: unknown) {
      console.error('Failed to check budget:', error);
      return null;
    }
  }

  /**
   * Reset budget for current period
   */
  async resetBudget(): Promise<boolean> {
    try {
      await apiClient.post('/api/cost-tracking/reset-budget');
      return true;
    } catch (error: unknown) {
      console.error('Failed to reset budget:', error);
      return false;
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

  /**
   * Get current provider preferences from backend
   */
  async getPreferences(): Promise<ProviderPreferences> {
    try {
      const response = await apiClient.get('/api/providers/preferences');
      return response.data;
    } catch (error: unknown) {
      console.error('Failed to get provider preferences:', error);
      // Return defaults if API call fails
      return {
        enableRecommendations: false,
        assistanceLevel: 'Off',
        enableHealthMonitoring: false,
        enableCostTracking: false,
        enableLearning: false,
        enableProfiles: false,
        enableAutoFallback: false,
        alwaysUseDefault: false,
        perOperationOverrides: {},
        activeProfile: 'Balanced',
        excludedProviders: [],
        fallbackChains: {},
        perProviderBudgetLimits: {},
        hardBudgetLimit: false,
      };
    }
  }

  /**
   * Update provider preferences on backend
   */
  async updatePreferences(preferences: Partial<ProviderPreferences>): Promise<void> {
    try {
      await apiClient.post('/api/providers/preferences', preferences);
      // Clear cache when preferences change
      this.clearCache();
    } catch (error: unknown) {
      console.error('Failed to update provider preferences:', error);
      throw error;
    }
  }
}

export interface ProviderPreferences {
  enableRecommendations: boolean;
  assistanceLevel: 'Off' | 'Minimal' | 'Moderate' | 'Full';
  enableHealthMonitoring: boolean;
  enableCostTracking: boolean;
  enableLearning: boolean;
  enableProfiles: boolean;
  enableAutoFallback: boolean;
  globalDefault?: string;
  alwaysUseDefault: boolean;
  perOperationOverrides: Record<string, string>;
  activeProfile: string;
  excludedProviders: string[];
  pinnedProvider?: string;
  fallbackChains: Record<string, string[]>;
  monthlyBudgetLimit?: number;
  perProviderBudgetLimits: Record<string, number>;
  hardBudgetLimit: boolean;
}

export interface CostTrackingConfiguration {
  id?: string;
  userId: string;
  overallMonthlyBudget?: number;
  budgetPeriodStart?: string;
  budgetPeriodEnd?: string;
  periodType: 'Monthly' | 'Weekly' | 'Custom';
  currency: string;
  alertThresholds: number[];
  emailNotificationsEnabled: boolean;
  notificationEmail?: string;
  alertFrequency: 'Once' | 'Daily' | 'EveryTime';
  providerBudgets: Record<string, number>;
  hardBudgetLimit: boolean;
  enableProjectTracking: boolean;
}

export interface SpendingReport {
  startDate: string;
  endDate: string;
  totalCost: number;
  currency: string;
  costByProvider: Record<string, number>;
  costByFeature: Record<string, number>;
  costByProject: Record<string, number>;
  recentTransactions: CostLogEntry[];
  trend?: SpendingTrend;
}

export interface CostLogEntry {
  id: string;
  timestamp: string;
  providerName: string;
  feature: string;
  cost: number;
  projectId?: string;
  projectName?: string;
  tokensUsed?: number;
  charactersUsed?: number;
  computeTimeSeconds?: number;
}

export interface SpendingTrend {
  averageDailyCost: number;
  projectedMonthlyCost: number;
  percentageChange: number;
  trendDirection: string;
}

export interface CurrentPeriodSpending {
  totalCost: number;
  currency: string;
  periodType: string;
  budget?: number;
  percentageUsed: number;
}

export interface ProviderPricing {
  providerName: string;
  providerType: string;
  isFree: boolean;
  costPer1KTokens?: number;
  costPer1KInputTokens?: number;
  costPer1KOutputTokens?: number;
  costPerCharacter?: number;
  costPer1KCharacters?: number;
  costPerImage?: number;
  costPerComputeSecond?: number;
  isManualOverride: boolean;
  lastUpdated: string;
  currency: string;
  notes?: string;
}

export interface BudgetCheckResult {
  isWithinBudget: boolean;
  shouldBlock: boolean;
  warnings: string[];
  currentMonthlyCost: number;
  estimatedNewTotal: number;
}

export interface CostEstimateRequest {
  providerName: string;
  operationType: string;
  estimatedInputTokens?: number;
  estimatedOutputTokens?: number;
  estimatedCharacters?: number;
  estimatedComputeSeconds?: number;
}

export const providerRecommendationService = new ProviderRecommendationService();
