/**
 * Pacing and rhythm analysis API service
 */

import { Scene } from '../../types';

const API_BASE = '/api/pacing';

export enum VideoFormat {
  Explainer = 'Explainer',
  Tutorial = 'Tutorial',
  Vlog = 'Vlog',
  Review = 'Review',
  Educational = 'Educational',
  Entertainment = 'Entertainment',
}

export enum TransitionType {
  NaturalPause = 'NaturalPause',
  SceneChange = 'SceneChange',
  MusicBeat = 'MusicBeat',
  EmphasisPoint = 'EmphasisPoint',
  BRollInsert = 'BRollInsert',
}

export enum Priority {
  Low = 'Low',
  Medium = 'Medium',
  High = 'High',
  Critical = 'Critical',
}

export enum RecommendationType {
  Hook = 'Hook',
  Pacing = 'Pacing',
  VisualInterest = 'VisualInterest',
  ContentDensity = 'ContentDensity',
  Transition = 'Transition',
}

export interface ScenePacingRecommendation {
  sceneIndex: number;
  currentDuration: string; // TimeSpan as ISO duration string
  recommendedDuration: string;
  importanceScore: number;
  complexityScore: number;
  reasoning: string;
}

export interface TransitionPoint {
  timestamp: string; // TimeSpan as ISO duration string
  type: TransitionType;
  confidence: number;
  context: string;
}

export interface PacingAnalysisResult {
  optimalDuration: string; // TimeSpan as ISO duration string
  engagementScore: number;
  sceneRecommendations: ScenePacingRecommendation[];
  suggestedTransitions: TransitionPoint[];
  narrativeArcAssessment: string;
  warnings: string[];
}

export interface AttentionPoint {
  timestamp: string; // TimeSpan as ISO duration string
  attentionLevel: number;
}

export interface AttentionCurve {
  points: AttentionPoint[];
  averageEngagement: number;
  criticalDropPoints: string[];
}

export interface RetentionSegment {
  start: string; // TimeSpan as ISO duration string
  end: string;
  predictedRetention: number;
  riskFactors: string;
}

export interface RetentionPrediction {
  overallRetentionScore: number;
  segments: RetentionSegment[];
  highDropRiskPoints: string[];
}

export interface RetentionRecommendation {
  title: string;
  description: string;
  timestamp: string; // TimeSpan as ISO duration string
  priority: Priority;
  type: RecommendationType;
}

export interface VideoRetentionAnalysis {
  pacingAnalysis: PacingAnalysisResult;
  retentionPrediction: RetentionPrediction;
  attentionCurve: AttentionCurve;
  recommendations: RetentionRecommendation[];
}

export interface PacingParameters {
  minSceneDuration: number;
  maxSceneDuration: number;
  averageSceneDuration: number;
  transitionDensity: number;
  hookDuration: number;
  musicSyncEnabled: boolean;
}

export interface ContentTemplate {
  name: string;
  description: string;
  format: VideoFormat;
  parameters: PacingParameters;
}

export interface QuickMetrics {
  estimatedRetention: number;
  estimatedEngagement: number;
  pacingVariance: number;
}

export interface VideoComparisonMetrics {
  original: QuickMetrics;
  optimized: QuickMetrics;
  improvements: Record<string, number>;
}

export const pacingAnalysisService = {
  /**
   * Analyzes pacing for a set of scenes
   */
  async analyzePacing(
    scenes: Scene[],
    audioPath: string | null,
    format: VideoFormat
  ): Promise<PacingAnalysisResult> {
    const response = await fetch(`${API_BASE}/analyze`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        scenes,
        audioPath,
        format,
      }),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ error: 'Failed to analyze pacing' }));
      throw new Error(error.error || 'Failed to analyze pacing');
    }

    return response.json();
  },

  /**
   * Predicts viewer retention for video content
   */
  async predictRetention(
    scenes: Scene[],
    audioPath: string | null,
    format: VideoFormat
  ): Promise<VideoRetentionAnalysis> {
    const response = await fetch(`${API_BASE}/retention`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        scenes,
        audioPath,
        format,
      }),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ error: 'Failed to predict retention' }));
      throw new Error(error.error || 'Failed to predict retention');
    }

    return response.json();
  },

  /**
   * Optimizes scene durations for better viewer retention
   */
  async optimizeScenes(scenes: Scene[], format: VideoFormat): Promise<Scene[]> {
    const response = await fetch(`${API_BASE}/optimize`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        scenes,
        format,
      }),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ error: 'Failed to optimize scenes' }));
      throw new Error(error.error || 'Failed to optimize scenes');
    }

    return response.json();
  },

  /**
   * Generates attention curve for a video
   */
  async getAttentionCurve(scenes: Scene[], videoDuration: string): Promise<AttentionCurve> {
    const response = await fetch(`${API_BASE}/attention-curve`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        scenes,
        videoDuration,
      }),
    });

    if (!response.ok) {
      const error = await response
        .json()
        .catch(() => ({ error: 'Failed to generate attention curve' }));
      throw new Error(error.error || 'Failed to generate attention curve');
    }

    return response.json();
  },

  /**
   * Compares original vs optimized versions
   */
  async compareVersions(
    originalScenes: Scene[],
    optimizedScenes: Scene[],
    format: VideoFormat
  ): Promise<VideoComparisonMetrics> {
    const response = await fetch(`${API_BASE}/compare`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        originalScenes,
        optimizedScenes,
        format,
      }),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ error: 'Failed to compare versions' }));
      throw new Error(error.error || 'Failed to compare versions');
    }

    return response.json();
  },

  /**
   * Gets available content templates
   */
  async getTemplates(): Promise<ContentTemplate[]> {
    const response = await fetch(`${API_BASE}/templates`);

    if (!response.ok) {
      throw new Error('Failed to fetch templates');
    }

    return response.json();
  },

  /**
   * Converts ISO duration string to seconds
   */
  durationToSeconds(duration: string): number {
    // Simple parser for durations like "PT15S" or "PT1M30S"
    // Pattern is safe: anchored with ^ and $, optional groups are non-overlapping
    // eslint-disable-next-line security/detect-unsafe-regex
    const match = duration.match(/^PT(?:(\d+)H)?(?:(\d+)M)?(?:(\d+)(?:\.(\d+))?S)?$/);
    if (!match) return 0;

    const hours = parseInt(match[1] || '0', 10);
    const minutes = parseInt(match[2] || '0', 10);
    const seconds = parseFloat(match[3] || '0');

    return hours * 3600 + minutes * 60 + seconds;
  },

  /**
   * Converts seconds to ISO duration string
   */
  secondsToDuration(seconds: number): string {
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    const secs = seconds % 60;

    let duration = 'PT';
    if (hours > 0) duration += `${hours}H`;
    if (minutes > 0) duration += `${minutes}M`;
    if (secs > 0 || (hours === 0 && minutes === 0)) duration += `${secs.toFixed(1)}S`;

    return duration;
  },
};

export default pacingAnalysisService;
