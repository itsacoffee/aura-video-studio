/**
 * Tests for Quality Dashboard state management
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { useQualityDashboardStore } from '../state/qualityDashboard';

// Mock the fetch API
global.fetch = vi.fn();

// Mock the apiUrl function
vi.mock('../config/api', () => ({
  apiUrl: (path: string) => `http://127.0.0.1:5005${path}`,
  API_BASE_URL: 'http://127.0.0.1:5005',
}));

describe('Quality Dashboard Store', () => {
  beforeEach(() => {
    // Reset store state before each test
    useQualityDashboardStore.setState({
      metrics: null,
      breakdown: null,
      historicalTrends: null,
      platformCompliance: [],
      recommendations: [],
      isLoading: false,
      error: null,
    });

    // Clear all mocks
    vi.clearAllMocks();
  });

  it('should initialize with empty state', () => {
    const state = useQualityDashboardStore.getState();

    expect(state.metrics).toBeNull();
    expect(state.breakdown).toBeNull();
    expect(state.historicalTrends).toBeNull();
    expect(state.platformCompliance).toEqual([]);
    expect(state.recommendations).toEqual([]);
    expect(state.isLoading).toBe(false);
    expect(state.error).toBeNull();
  });

  it('should fetch metrics successfully', async () => {
    const mockMetrics = {
      totalVideosProcessed: 1247,
      averageQualityScore: 92.5,
      successRate: 98.3,
      averageProcessingTime: '00:12:30',
      totalErrorsLast24h: 3,
      currentProcessingJobs: 2,
      queuedJobs: 5,
      peakQualityScore: 99.2,
      lowestQualityScore: 78.5,
      complianceRate: 96.8,
      lastUpdated: '2025-01-01T00:00:00Z',
    };

    const mockBreakdown = {
      resolution: {
        totalChecks: 1247,
        passedChecks: 1230,
        failedChecks: 17,
        averageScore: 98.6,
      },
      audio: {
        totalChecks: 1247,
        passedChecks: 1215,
        failedChecks: 32,
        averageScore: 97.4,
      },
      frameRate: {
        totalChecks: 1247,
        passedChecks: 1240,
        failedChecks: 7,
        averageScore: 99.4,
      },
      consistency: {
        totalChecks: 1247,
        passedChecks: 1190,
        failedChecks: 57,
        averageScore: 95.4,
      },
    };

    (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
      ok: true,
      headers: {
        get: () => 'application/json',
      },
      json: async () => ({ metrics: mockMetrics, breakdown: mockBreakdown }),
    } as Response);

    await useQualityDashboardStore.getState().fetchMetrics();

    const state = useQualityDashboardStore.getState();
    expect(state.metrics).toEqual(mockMetrics);
    expect(state.breakdown).toEqual(mockBreakdown);
    expect(state.isLoading).toBe(false);
    expect(state.error).toBeNull();
  });

  it('should handle fetch metrics error with JSON response', async () => {
    const mockError = {
      detail: 'Failed to retrieve dashboard metrics',
      title: 'Dashboard Metrics Failed',
      status: 500,
    };

    (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
      ok: false,
      status: 500,
      statusText: 'Internal Server Error',
      headers: {
        get: (name: string) => (name === 'content-type' ? 'application/json' : null),
      },
      json: async () => mockError,
    } as Response);

    await useQualityDashboardStore.getState().fetchMetrics();

    const state = useQualityDashboardStore.getState();
    expect(state.error).toBe('Failed to retrieve dashboard metrics');
    expect(state.isLoading).toBe(false);
  });

  it('should handle fetch metrics error with HTML response', async () => {
    (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
      ok: false,
      status: 404,
      statusText: 'Not Found',
      headers: {
        get: (name: string) => (name === 'content-type' ? 'text/html' : null),
      },
    } as Response);

    await useQualityDashboardStore.getState().fetchMetrics();

    const state = useQualityDashboardStore.getState();
    expect(state.error).toContain('Failed to fetch metrics: 404 Not Found');
    expect(state.isLoading).toBe(false);
  });

  it('should fetch recommendations successfully', async () => {
    const mockRecommendations = [
      {
        id: 'rec-1',
        title: 'Improve Quality Score',
        description: 'Test recommendation',
        priority: 'high' as const,
        category: 'quality',
        impactScore: 8.5,
        estimatedImprovement: '5-10%',
        actionItems: ['Action 1', 'Action 2'],
      },
    ];

    (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
      ok: true,
      headers: {
        get: () => 'application/json',
      },
      json: async () => ({ recommendations: mockRecommendations }),
    } as Response);

    await useQualityDashboardStore.getState().fetchRecommendations();

    const state = useQualityDashboardStore.getState();
    expect(state.recommendations).toEqual(mockRecommendations);
    expect(state.isLoading).toBe(false);
  });

  it('should fetch platform compliance successfully', async () => {
    const mockCompliance = [
      {
        platform: 'YouTube',
        complianceRate: 98.5,
        totalVideos: 450,
        compliantVideos: 443,
        commonIssues: ['Audio normalization'],
      },
    ];

    (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
      ok: true,
      headers: {
        get: () => 'application/json',
      },
      json: async () => ({ platforms: mockCompliance }),
    } as Response);

    await useQualityDashboardStore.getState().fetchPlatformCompliance();

    const state = useQualityDashboardStore.getState();
    expect(state.platformCompliance).toEqual(mockCompliance);
    expect(state.isLoading).toBe(false);
  });
});
