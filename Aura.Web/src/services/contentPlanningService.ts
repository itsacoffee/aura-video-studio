/**
 * Content Planning API service for AI-driven content strategy and scheduling
 */

import apiClient from './api/apiClient';

// Types for Trend Analysis
export interface TrendData {
  id: string;
  topic: string;
  category: string;
  platform: string;
  trendScore: number;
  direction: 'Rising' | 'Stable' | 'Declining';
  analyzedAt: string;
  dataPoints: TrendDataPoint[];
  metrics: Record<string, any>;
}

export interface TrendDataPoint {
  timestamp: string;
  value: number;
  additionalData?: Record<string, any>;
}

export interface TrendAnalysisRequest {
  category?: string;
  platform?: string;
  keywords: string[];
  startDate?: string;
  endDate?: string;
}

export interface TrendAnalysisResponse {
  trends: TrendData[];
  analyzedAt: string;
  summary: string;
}

// Types for Topic Suggestions
export interface TopicSuggestion {
  id: string;
  topic: string;
  description: string;
  category: string;
  relevanceScore: number;
  trendScore: number;
  predictedEngagement: number;
  keywords: string[];
  recommendedPlatforms: string[];
  generatedAt: string;
  metadata: Record<string, any>;
}

export interface TopicSuggestionRequest {
  category?: string;
  targetAudience?: string;
  interests: string[];
  preferredPlatforms: string[];
  count: number;
}

export interface TopicSuggestionResponse {
  suggestions: TopicSuggestion[];
  generatedAt: string;
  totalCount: number;
}

// Types for Audience Analysis
export interface Demographics {
  ageDistribution: Record<string, number>;
  genderDistribution: Record<string, number>;
  locationDistribution: Record<string, number>;
}

export interface AudienceInsight {
  id: string;
  platform: string;
  demographics: Demographics;
  topInterests: string[];
  preferredContentTypes: string[];
  engagementRate: number;
  bestPostingTimes: Record<string, number>;
  analyzedAt: string;
}

export interface AudienceAnalysisRequest {
  platform?: string;
  category?: string;
  contentTags: string[];
}

export interface AudienceAnalysisResponse {
  insights: AudienceInsight;
  recommendations: string[];
  analyzedAt: string;
}

// Types for Content Scheduling
export interface ScheduledContent {
  id: string;
  contentPlanId: string;
  title: string;
  platform: string;
  scheduledDateTime: string;
  optimalTimeWindow: string;
  predictedReach: number;
  status: 'Pending' | 'Ready' | 'Published' | 'Failed' | 'Cancelled';
  tags: string[];
  metadata: Record<string, any>;
}

export interface SchedulingRecommendation {
  recommendedDateTime: string;
  confidenceScore: number;
  reasoning: string;
  predictedEngagement: number;
  metrics: Record<string, any>;
}

export interface ContentSchedulingRequest {
  platform: string;
  category: string;
  preferredDate?: string;
  targetAudience: string[];
}

export interface ContentSchedulingResponse {
  recommendations: SchedulingRecommendation[];
  analyzedAt: string;
}

export interface ScheduleContentRequest {
  title: string;
  description?: string;
  category?: string;
  platform: string;
  scheduledDateTime: string;
  tags?: string[];
}

// API Methods
export const contentPlanningService = {
  /**
   * Analyzes trends for content planning
   */
  async analyzeTrends(request: TrendAnalysisRequest): Promise<TrendAnalysisResponse> {
    const response = await apiClient.post<TrendAnalysisResponse>(
      '/api/ContentPlanning/trends/analyze',
      request
    );
    return response.data;
  },

  /**
   * Gets trending topics for a specific platform
   */
  async getPlatformTrends(
    platform: string,
    category?: string
  ): Promise<{ success: boolean; trends: TrendData[]; platform: string; category?: string }> {
    const params = category ? { category } : {};
    const response = await apiClient.get(`/api/ContentPlanning/trends/platform/${platform}`, {
      params,
    });
    return response.data;
  },

  /**
   * Generates AI-powered topic suggestions
   */
  async generateTopics(request: TopicSuggestionRequest): Promise<TopicSuggestionResponse> {
    const response = await apiClient.post<TopicSuggestionResponse>(
      '/api/ContentPlanning/topics/generate',
      request
    );
    return response.data;
  },

  /**
   * Generates topic suggestions based on current trends
   */
  async generateTrendBasedTopics(
    trendRequest: TrendAnalysisRequest,
    count: number = 5
  ): Promise<{ success: boolean; topics: TopicSuggestion[]; count: number }> {
    const response = await apiClient.post(
      `/api/ContentPlanning/topics/trend-based?count=${count}`,
      trendRequest
    );
    return response.data;
  },

  /**
   * Gets scheduling recommendations for content
   */
  async getSchedulingRecommendations(
    request: ContentSchedulingRequest
  ): Promise<ContentSchedulingResponse> {
    const response = await apiClient.post<ContentSchedulingResponse>(
      '/api/ContentPlanning/schedule/recommendations',
      request
    );
    return response.data;
  },

  /**
   * Schedules content for a specific time
   */
  async scheduleContent(
    request: ScheduleContentRequest
  ): Promise<{ success: boolean; scheduled: ScheduledContent }> {
    const response = await apiClient.post('/api/ContentPlanning/schedule/content', request);
    return response.data;
  },

  /**
   * Gets scheduled content within a date range
   */
  async getScheduledContent(
    startDate: string,
    endDate: string,
    platform?: string
  ): Promise<{ success: boolean; content: ScheduledContent[]; count: number }> {
    const params: any = { startDate, endDate };
    if (platform) params.platform = platform;

    const response = await apiClient.get('/api/ContentPlanning/schedule/calendar', { params });
    return response.data;
  },

  /**
   * Analyzes target audience for content planning
   */
  async analyzeAudience(request: AudienceAnalysisRequest): Promise<AudienceAnalysisResponse> {
    const response = await apiClient.post<AudienceAnalysisResponse>(
      '/api/ContentPlanning/audience/analyze',
      request
    );
    return response.data;
  },

  /**
   * Gets demographic information for a platform
   */
  async getDemographics(
    platform: string
  ): Promise<{ success: boolean; demographics: Demographics; platform: string }> {
    const response = await apiClient.get(`/api/ContentPlanning/audience/demographics/${platform}`);
    return response.data;
  },

  /**
   * Gets top interests for a category
   */
  async getTopInterests(
    category: string
  ): Promise<{ success: boolean; interests: string[]; category: string }> {
    const response = await apiClient.get(`/api/ContentPlanning/audience/interests/${category}`);
    return response.data;
  },
};
