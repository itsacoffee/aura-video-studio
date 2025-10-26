/**
 * Analytics API service for retention and optimization
 */

const API_BASE = '/api/analytics';

export interface RetentionPoint {
  timePoint: string;
  retention: number;
}

export interface EngagementDip {
  timePoint: string;
  retentionDrop: number;
  severity: string;
  reason: string;
}

export interface RetentionPrediction {
  retentionCurve: RetentionPoint[];
  predictedAverageRetention: number;
  engagementDips: EngagementDip[];
  optimalLength: string;
  recommendations: string[];
}

export interface SegmentScore {
  segmentIndex: number;
  startTime: string;
  duration: string;
  engagementScore: number;
  reasoning: string;
}

export interface AttentionAnalysis {
  segmentScores: SegmentScore[];
  criticalDropPoints: SegmentScore[];
  averageEngagement: number;
  suggestions: string[];
}

export interface PlatformOptimization {
  platform: string;
  optimalDuration: string;
  recommendedAspectRatio: string;
  optimalThumbnailSize: string;
  recommendations: string[];
  metadataGuidelines: Record<string, string>;
  hashtagSuggestions: string[];
}

export interface PlatformAspectRatio {
  platform: string;
  aspectRatio: string;
  resolution: string;
  reasoning: string;
}

export interface AspectRatioSuggestions {
  suggestions: PlatformAspectRatio[];
  recommendedPrimaryFormat: string;
  adaptationStrategy: string;
}

export interface ContentStructureAnalysis {
  hookQuality: number;
  hookSuggestions: string[];
  pacingScore: number;
  pacingIssues: string[];
  structuralStrength: number;
  improvementAreas: string[];
  overallScore: number;
}

export interface ContentImprovement {
  area: string;
  priority: string;
  currentState: string;
  suggestion: string;
  expectedImpact: string;
}

export interface ContentRecommendations {
  targetAudience: string;
  recommendations: ContentImprovement[];
  estimatedImprovementScore: number;
}

export interface ActionItem {
  title: string;
  description: string;
  impact: string;
  difficulty: string;
  category: string;
  estimatedTime: string;
}

export interface ImprovementRoadmap {
  currentScore: number;
  potentialScore: number;
  prioritizedActions: ActionItem[];
  quickWins: { title: string; description: string }[];
  estimatedTimeToImprove: string;
}

export interface FeedbackIssue {
  type: string;
  severity: string;
  message: string;
  suggestion: string;
}

export interface RealTimeFeedback {
  issues: FeedbackIssue[];
  strengths: string[];
  currentQualityScore: number;
  suggestions: string[];
}

export const analyticsService = {
  /**
   * Predicts audience retention for content
   */
  async predictRetention(
    content: string,
    contentType: string,
    videoDuration: string,
    targetDemographic?: string
  ): Promise<RetentionPrediction> {
    const response = await fetch(`${API_BASE}/predict-retention`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ content, contentType, videoDuration, targetDemographic }),
    });

    if (!response.ok) throw new Error('Failed to predict retention');
    return await response.json();
  },

  /**
   * Analyzes attention span patterns
   */
  async analyzeAttention(content: string, videoDuration: string): Promise<AttentionAnalysis> {
    const response = await fetch(`${API_BASE}/analyze-attention`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ content, videoDuration }),
    });

    if (!response.ok) throw new Error('Failed to analyze attention');
    return await response.json();
  },

  /**
   * Gets platform-specific optimization recommendations
   */
  async optimizePlatform(
    platform: string,
    content: string,
    videoDuration: string
  ): Promise<PlatformOptimization> {
    const response = await fetch(`${API_BASE}/optimize-platform`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ platform, content, videoDuration }),
    });

    if (!response.ok) throw new Error('Failed to optimize for platform');
    return await response.json();
  },

  /**
   * Suggests aspect ratios for cross-platform publishing
   */
  async suggestAspectRatios(targetPlatforms: string[]): Promise<AspectRatioSuggestions> {
    const response = await fetch(`${API_BASE}/suggest-aspect-ratios`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ targetPlatforms }),
    });

    if (!response.ok) throw new Error('Failed to suggest aspect ratios');
    return await response.json();
  },

  /**
   * Analyzes content structure
   */
  async analyzeStructure(content: string, contentType: string): Promise<ContentStructureAnalysis> {
    const response = await fetch(`${API_BASE}/analyze-structure`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ content, contentType }),
    });

    if (!response.ok) throw new Error('Failed to analyze structure');
    return await response.json();
  },

  /**
   * Gets content improvement recommendations
   */
  async getRecommendations(
    content: string,
    targetAudience: string
  ): Promise<ContentRecommendations> {
    const response = await fetch(`${API_BASE}/get-recommendations`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ content, targetAudience }),
    });

    if (!response.ok) throw new Error('Failed to get recommendations');
    return await response.json();
  },

  /**
   * Generates comprehensive improvement roadmap
   */
  async getImprovementRoadmap(
    content: string,
    contentType: string,
    videoDuration: string,
    targetPlatforms: string[]
  ): Promise<ImprovementRoadmap> {
    const response = await fetch(`${API_BASE}/improvement-roadmap`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ content, contentType, videoDuration, targetPlatforms }),
    });

    if (!response.ok) throw new Error('Failed to get improvement roadmap');
    return await response.json();
  },

  /**
   * Provides real-time feedback for content being created
   */
  async getRealTimeFeedback(
    currentContent: string,
    currentWordCount: number,
    currentDuration: string
  ): Promise<RealTimeFeedback> {
    const response = await fetch(`${API_BASE}/real-time-feedback`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ currentContent, currentWordCount, currentDuration }),
    });

    if (!response.ok) throw new Error('Failed to get real-time feedback');
    return await response.json();
  },
};
