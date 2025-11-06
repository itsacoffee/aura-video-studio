import { create } from 'zustand';

/**
 * Token usage statistics
 */
export interface TokenUsageStatistics {
  totalInputTokens: number;
  totalOutputTokens: number;
  totalTokens: number;
  operationCount: number;
  cacheHits: number;
  cacheHitRate: number;
  averageTokensPerOperation: number;
  averageResponseTimeMs: number;
  totalCost: number;
  costSavedByCache: number;
}

/**
 * Stage cost breakdown
 */
export interface StageCostBreakdown {
  stageName: string;
  cost: number;
  percentageOfTotal: number;
  durationSeconds: number;
  operationCount: number;
  providerName?: string;
}

/**
 * Operation cost detail
 */
export interface OperationCostDetail {
  timestamp: string;
  operationType: string;
  providerName: string;
  cost: number;
  durationMs: number;
  tokensUsed?: number;
  charactersProcessed?: number;
  cacheHit: boolean;
}

/**
 * Cost optimization suggestion
 */
export interface CostOptimizationSuggestion {
  category: string;
  suggestion: string;
  estimatedSavings: number;
  qualityImpact?: string;
}

/**
 * Run cost report
 */
export interface RunCostReport {
  jobId: string;
  projectId?: string;
  projectName?: string;
  startedAt: string;
  completedAt?: string;
  durationSeconds: number;
  totalCost: number;
  currency: string;
  costByStage: Record<string, StageCostBreakdown>;
  costByProvider: Record<string, number>;
  tokenStats?: TokenUsageStatistics;
  operations: OperationCostDetail[];
  optimizationSuggestions: CostOptimizationSuggestion[];
  withinBudget: boolean;
  budgetLimit?: number;
}

/**
 * Budget configuration
 */
export interface BudgetConfiguration {
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

/**
 * Current period spending
 */
export interface CurrentPeriodSpending {
  totalCost: number;
  currency: string;
  periodType: string;
  budget?: number;
  percentageUsed: number;
}

/**
 * Real-time cost accumulation during generation
 */
export interface LiveCostAccumulation {
  currentCost: number;
  estimatedTotalCost: number;
  costByStage: Record<string, number>;
  lastUpdated: string;
}

interface CostTrackingState {
  configuration: BudgetConfiguration | null;
  currentPeriodSpending: CurrentPeriodSpending | null;
  liveAccumulation: LiveCostAccumulation | null;
  runReports: Record<string, RunCostReport>;

  isLoading: boolean;
  error: string | null;

  loadConfiguration: () => Promise<void>;
  updateConfiguration: (config: BudgetConfiguration) => Promise<void>;

  loadCurrentPeriodSpending: () => Promise<void>;

  getRunSummary: (jobId: string) => Promise<RunCostReport | null>;
  exportReport: (jobId: string, format: 'json' | 'csv') => Promise<void>;

  startLiveTracking: () => void;
  updateLiveCost: (cost: number, stage: string) => void;
  stopLiveTracking: () => void;

  reset: () => void;
}

const initialState = {
  configuration: null,
  currentPeriodSpending: null,
  liveAccumulation: null,
  runReports: {},
  isLoading: false,
  error: null,
};

export const useCostTrackingStore = create<CostTrackingState>((set) => ({
  ...initialState,

  loadConfiguration: async () => {
    set({ isLoading: true, error: null });

    try {
      const response = await fetch('/api/cost-tracking/configuration');

      if (!response.ok) {
        throw new Error('Failed to load cost tracking configuration');
      }

      const config = await response.json();
      set({ configuration: config, isLoading: false });
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      set({ error: errorObj.message, isLoading: false });
    }
  },

  updateConfiguration: async (config: BudgetConfiguration) => {
    set({ isLoading: true, error: null });

    try {
      const response = await fetch('/api/cost-tracking/configuration', {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(config),
      });

      if (!response.ok) {
        throw new Error('Failed to update cost tracking configuration');
      }

      set({ configuration: config, isLoading: false });
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      set({ error: errorObj.message, isLoading: false });
    }
  },

  loadCurrentPeriodSpending: async () => {
    set({ isLoading: true, error: null });

    try {
      const response = await fetch('/api/cost-tracking/current-period');

      if (!response.ok) {
        throw new Error('Failed to load current period spending');
      }

      const spending = await response.json();
      set({ currentPeriodSpending: spending, isLoading: false });
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      set({ error: errorObj.message, isLoading: false });
    }
  },

  getRunSummary: async (jobId: string) => {
    set({ isLoading: true, error: null });

    try {
      // Use the unified RunTelemetry v1 endpoint
      const response = await fetch(`/api/telemetry/${jobId}`);
      
      if (!response.ok) {
        if (response.status === 404) {
          set({ isLoading: false });
          return null;
        }
        throw new Error('Failed to load run telemetry');
      }
      
      const telemetry = await response.json();
      
      // Adapt telemetry data to RunCostReport format
      const { adaptTelemetryToRunCost } = await import('@/services/telemetryAdapter');
      const report = adaptTelemetryToRunCost(telemetry);
      
      set((state) => ({
        runReports: { ...state.runReports, [jobId]: report },
        isLoading: false,
      }));

      return report;
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      set({ error: errorObj.message, isLoading: false });
      return null;
    }
  },

  exportReport: async (jobId: string, format: 'json' | 'csv') => {
    set({ isLoading: true, error: null });

    try {
      const response = await fetch(`/api/cost-tracking/export/${jobId}?format=${format}`, {
        method: 'POST',
      });

      if (!response.ok) {
        throw new Error('Failed to export report');
      }

      const blob = await response.blob();
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `cost-report-${jobId}.${format}`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);

      set({ isLoading: false });
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      set({ error: errorObj.message, isLoading: false });
    }
  },

  startLiveTracking: () => {
    set({
      liveAccumulation: {
        currentCost: 0,
        estimatedTotalCost: 0,
        costByStage: {},
        lastUpdated: new Date().toISOString(),
      },
    });
  },

  updateLiveCost: (cost: number, stage: string) => {
    set((state) => {
      if (!state.liveAccumulation) return state;

      const newCostByStage = {
        ...state.liveAccumulation.costByStage,
        [stage]: (state.liveAccumulation.costByStage[stage] || 0) + cost,
      };

      const totalCost = Object.values(newCostByStage).reduce((sum, c) => sum + c, 0);

      return {
        liveAccumulation: {
          currentCost: totalCost,
          estimatedTotalCost: totalCost * 1.1,
          costByStage: newCostByStage,
          lastUpdated: new Date().toISOString(),
        },
      };
    });
  },

  stopLiveTracking: () => {
    set({ liveAccumulation: null });
  },

  reset: () => {
    set(initialState);
  },
}));
