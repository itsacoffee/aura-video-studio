/**
 * Performance Analytics API service for video performance tracking and insights
 */

const API_BASE = '/api/performance-analytics';

export interface PerformanceMetrics {
  views: number;
  watchTimeMinutes?: number;
  averageViewDuration?: number;
  averageViewPercentage?: number;
  engagement: {
    likes: number;
    dislikes?: number;
    comments: number;
    shares: number;
    engagementRate: number;
  };
  clickThroughRate?: number;
}

export interface VideoPerformance {
  videoId: string;
  projectId?: string;
  platform: string;
  title: string;
  url?: string;
  publishedAt: string;
  metrics: PerformanceMetrics;
}

export interface PerformancePattern {
  patternId: string;
  patternType: string;
  description: string;
  strength: number;
  occurrences: number;
  impact: {
    viewsImpact: number;
    engagementImpact?: number;
    retentionImpact?: number;
    overallImpact: number;
  };
  firstObserved: string;
  lastObserved: string;
}

export interface PerformanceInsights {
  profileId: string;
  generatedAt: string;
  totalVideos: number;
  averageViews: number;
  averageEngagementRate: number;
  topSuccessPatterns: PerformancePattern[];
  topFailurePatterns: PerformancePattern[];
  actionableInsights: string[];
  overallTrend: string;
}

export interface DecisionCorrelation {
  correlationId: string;
  videoId: string;
  decisionType: string;
  decisionValue: string;
  outcome: {
    outcomeType: string;
    performanceScore: number;
    comparedTo?: string;
  };
  correlationStrength: number;
  statisticalSignificance: number;
  analyzedAt: string;
}

export interface VideoProjectLink {
  linkId: string;
  videoId: string;
  projectId: string;
  linkType: string;
  confidenceScore: number;
  linkedAt: string;
  isConfirmed: boolean;
}

export interface ABTest {
  testId: string;
  testName: string;
  description?: string;
  category: string;
  status: string;
  createdAt: string;
  startedAt?: string;
  completedAt?: string;
  variants: {
    variantId: string;
    variantName: string;
    description?: string;
    projectId?: string;
    videoId?: string;
  }[];
  results?: {
    analyzedAt: string;
    winner?: string;
    confidence: number;
    isStatisticallySignificant: boolean;
    insights: string[];
  };
}

export interface ImportAnalyticsRequest {
  profileId: string;
  platform: string;
  fileType: 'csv' | 'json';
  filePath: string;
}

export interface LinkVideoRequest {
  videoId: string;
  projectId: string;
  profileId: string;
  linkedBy?: string;
}

export interface CreateABTestRequest {
  profileId: string;
  testName: string;
  description?: string;
  category?: string;
  variants: {
    variantName: string;
    description?: string;
    projectId?: string;
    configuration?: Record<string, any>;
  }[];
}

class PerformanceAnalyticsService {
  /**
   * Import analytics data from a file
   */
  async importAnalytics(
    request: ImportAnalyticsRequest
  ): Promise<{ importId: string; videosImported: number }> {
    const response = await fetch(`${API_BASE}/import`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error('Failed to import analytics');
    }

    const data = await response.json();
    return data.import;
  }

  /**
   * Get all videos with performance data for a profile
   */
  async getVideos(profileId: string): Promise<VideoPerformance[]> {
    const response = await fetch(`${API_BASE}/videos/${profileId}`);

    if (!response.ok) {
      throw new Error('Failed to fetch videos');
    }

    const data = await response.json();
    return data.videos;
  }

  /**
   * Link a video to a project
   */
  async linkVideoToProject(request: LinkVideoRequest): Promise<VideoProjectLink> {
    const response = await fetch(`${API_BASE}/link-video`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error('Failed to link video');
    }

    const data = await response.json();
    return data.link;
  }

  /**
   * Get correlations between AI decisions and performance for a project
   */
  async getProjectCorrelations(projectId: string): Promise<DecisionCorrelation[]> {
    const response = await fetch(`${API_BASE}/correlations/${projectId}`);

    if (!response.ok) {
      throw new Error('Failed to fetch correlations');
    }

    const data = await response.json();
    return data.correlations;
  }

  /**
   * Get performance insights for a profile
   */
  async getInsights(profileId: string): Promise<PerformanceInsights> {
    const response = await fetch(`${API_BASE}/insights/${profileId}`);

    if (!response.ok) {
      throw new Error('Failed to fetch insights');
    }

    const data = await response.json();
    return data.insights;
  }

  /**
   * Analyze performance for a profile
   */
  async analyzePerformance(profileId: string): Promise<{
    analyzedAt: string;
    totalVideos: number;
    analyzedProjects: number;
    totalCorrelations: number;
    successPatternsFound: number;
    failurePatternsFound: number;
  }> {
    const response = await fetch(`${API_BASE}/analyze`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ profileId }),
    });

    if (!response.ok) {
      throw new Error('Failed to analyze performance');
    }

    const data = await response.json();
    return data.analysis;
  }

  /**
   * Get success patterns for a profile
   */
  async getSuccessPatterns(profileId: string): Promise<PerformancePattern[]> {
    const response = await fetch(`${API_BASE}/success-patterns/${profileId}`);

    if (!response.ok) {
      throw new Error('Failed to fetch success patterns');
    }

    const data = await response.json();
    return data.patterns;
  }

  /**
   * Create a new A/B test
   */
  async createABTest(request: CreateABTestRequest): Promise<ABTest> {
    const response = await fetch(`${API_BASE}/ab-test`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error('Failed to create A/B test');
    }

    const data = await response.json();
    return data.test;
  }

  /**
   * Get A/B test results
   */
  async getABTestResults(testId: string, profileId: string): Promise<ABTest> {
    const response = await fetch(`${API_BASE}/ab-results/${testId}?profileId=${profileId}`);

    if (!response.ok) {
      throw new Error('Failed to fetch A/B test results');
    }

    const data = await response.json();
    return data.test;
  }
}

export const performanceAnalyticsService = new PerformanceAnalyticsService();
