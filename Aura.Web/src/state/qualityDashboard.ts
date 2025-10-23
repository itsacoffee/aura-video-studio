import { create } from 'zustand';
import { apiUrl } from '../config/api';

export interface QualityMetrics {
  totalVideosProcessed: number;
  averageQualityScore: number;
  successRate: number;
  averageProcessingTime: string;
  totalErrorsLast24h: number;
  currentProcessingJobs: number;
  queuedJobs: number;
  peakQualityScore: number;
  lowestQualityScore: number;
  complianceRate: number;
  lastUpdated: string;
}

export interface CategoryMetrics {
  totalChecks: number;
  passedChecks: number;
  failedChecks: number;
  averageScore: number;
}

export interface MetricsBreakdown {
  resolution: CategoryMetrics;
  audio: CategoryMetrics;
  frameRate: CategoryMetrics;
  consistency: CategoryMetrics;
}

export interface TrendDataPoint {
  timestamp: string;
  qualityScore: number;
  processedVideos: number;
  errorCount: number;
  averageProcessingTime: string;
}

export interface HistoricalTrends {
  startDate: string;
  endDate: string;
  granularity: string;
  trendDirection: string;
  averageChange: number;
  dataPoints: TrendDataPoint[];
}

export interface PlatformCompliance {
  platform: string;
  complianceRate: number;
  totalVideos: number;
  compliantVideos: number;
  commonIssues: string[];
}

export interface QualityRecommendation {
  id: string;
  title: string;
  description: string;
  priority: 'low' | 'medium' | 'high';
  category: string;
  impactScore: number;
  estimatedImprovement: string;
  actionItems: string[];
}

interface QualityDashboardState {
  metrics: QualityMetrics | null;
  breakdown: MetricsBreakdown | null;
  historicalTrends: HistoricalTrends | null;
  platformCompliance: PlatformCompliance[];
  recommendations: QualityRecommendation[];
  isLoading: boolean;
  error: string | null;

  // Actions
  fetchMetrics: () => Promise<void>;
  fetchHistoricalData: (startDate?: Date, endDate?: Date, granularity?: string) => Promise<void>;
  fetchPlatformCompliance: () => Promise<void>;
  fetchRecommendations: () => Promise<void>;
  exportReport: (format: 'json' | 'csv' | 'markdown') => Promise<void>;
  refreshAll: () => Promise<void>;
}

export const useQualityDashboardStore = create<QualityDashboardState>((set, get) => ({
  metrics: null,
  breakdown: null,
  historicalTrends: null,
  platformCompliance: [],
  recommendations: [],
  isLoading: false,
  error: null,

  fetchMetrics: async () => {
    set({ isLoading: true, error: null });
    try {
      const response = await fetch(`${apiUrl}/api/dashboard/metrics`);
      if (!response.ok) throw new Error('Failed to fetch metrics');
      const data = await response.json();
      set({ 
        metrics: data.metrics,
        breakdown: data.breakdown,
        isLoading: false 
      });
    } catch (error) {
      set({ 
        error: error instanceof Error ? error.message : 'Unknown error',
        isLoading: false 
      });
    }
  },

  fetchHistoricalData: async (startDate?: Date, endDate?: Date, granularity = 'daily') => {
    set({ isLoading: true, error: null });
    try {
      const params = new URLSearchParams();
      if (startDate) params.append('startDate', startDate.toISOString());
      if (endDate) params.append('endDate', endDate.toISOString());
      params.append('granularity', granularity);

      const response = await fetch(`${apiUrl}/api/dashboard/historical-data?${params}`);
      if (!response.ok) throw new Error('Failed to fetch historical data');
      const data = await response.json();
      set({ 
        historicalTrends: data,
        isLoading: false 
      });
    } catch (error) {
      set({ 
        error: error instanceof Error ? error.message : 'Unknown error',
        isLoading: false 
      });
    }
  },

  fetchPlatformCompliance: async () => {
    set({ isLoading: true, error: null });
    try {
      const response = await fetch(`${apiUrl}/api/dashboard/platform-compliance`);
      if (!response.ok) throw new Error('Failed to fetch platform compliance');
      const data = await response.json();
      set({ 
        platformCompliance: data.platforms,
        isLoading: false 
      });
    } catch (error) {
      set({ 
        error: error instanceof Error ? error.message : 'Unknown error',
        isLoading: false 
      });
    }
  },

  fetchRecommendations: async () => {
    set({ isLoading: true, error: null });
    try {
      const response = await fetch(`${apiUrl}/api/dashboard/recommendations`);
      if (!response.ok) throw new Error('Failed to fetch recommendations');
      const data = await response.json();
      set({ 
        recommendations: data.recommendations,
        isLoading: false 
      });
    } catch (error) {
      set({ 
        error: error instanceof Error ? error.message : 'Unknown error',
        isLoading: false 
      });
    }
  },

  exportReport: async (format: 'json' | 'csv' | 'markdown') => {
    try {
      const response = await fetch(`${apiUrl}/api/dashboard/export`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ format })
      });

      if (!response.ok) throw new Error('Failed to export report');

      // Download the file
      const blob = await response.blob();
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `quality-report-${new Date().toISOString().split('T')[0]}.${format}`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
    } catch (error) {
      set({ 
        error: error instanceof Error ? error.message : 'Unknown error'
      });
    }
  },

  refreshAll: async () => {
    const state = get();
    await Promise.all([
      state.fetchMetrics(),
      state.fetchHistoricalData(),
      state.fetchPlatformCompliance(),
      state.fetchRecommendations()
    ]);
  }
}));
