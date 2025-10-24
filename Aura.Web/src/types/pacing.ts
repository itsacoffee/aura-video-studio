/**
 * TypeScript types for pacing analysis and optimization
 */

import { Brief } from '../types';

/**
 * Request for pacing analysis
 */
export interface PacingAnalysisRequest {
  script: string;
  scenes: Scene[];
  targetPlatform: string;
  targetDuration: number;
  audience?: string;
  brief: Brief;
}

/**
 * Scene information for pacing analysis
 */
export interface Scene {
  sceneIndex: number;
  narration: string;
  visualDescription: string;
  duration?: number;
}

/**
 * Information density levels
 */
export enum InformationDensity {
  Low = 'Low',
  Medium = 'Medium',
  High = 'High',
}

/**
 * Transition types for scene changes
 */
export enum TransitionType {
  Cut = 'Cut',
  Fade = 'Fade',
  Dissolve = 'Dissolve',
}

/**
 * Scene timing suggestion from ML analysis
 */
export interface SceneTimingSuggestion {
  sceneIndex: number;
  currentDuration: string; // ISO 8601 duration format (e.g., "PT15S")
  optimalDuration: string;
  minDuration: string;
  maxDuration: string;
  importanceScore: number; // 0-100
  complexityScore: number; // 0-100
  emotionalIntensity: number; // 0-100
  informationDensity: InformationDensity;
  transitionType: TransitionType;
  confidence: number; // 0-100
  reasoning: string;
  usedLlmAnalysis: boolean;
}

/**
 * Single point on the attention curve
 */
export interface AttentionDataPoint {
  timestamp: string; // ISO 8601 duration format
  attentionLevel: number; // 0-100
  retentionRate: number; // 0-100
  engagementScore: number; // 0-100
}

/**
 * Attention curve prediction data
 */
export interface AttentionCurveData {
  dataPoints: AttentionDataPoint[];
  averageEngagement: number; // 0-100
  engagementPeaks: string[]; // ISO 8601 duration timestamps
  engagementValleys: string[]; // ISO 8601 duration timestamps
  overallRetentionScore: number; // 0-100
}

/**
 * Complete pacing analysis response
 */
export interface PacingAnalysisResponse {
  overallScore: number; // 0-100
  suggestions: SceneTimingSuggestion[];
  attentionCurve: AttentionCurveData | null;
  estimatedRetention: number; // 0-100
  averageEngagement: number; // 0-100
  analysisId: string;
  timestamp: string; // ISO 8601 timestamp
  correlationId: string;
  confidenceScore: number; // 0-100
  warnings: string[];
}

/**
 * Platform preset information
 */
export interface PlatformPreset {
  name: string;
  recommendedPacing: string;
  avgSceneDuration: string;
  optimalVideoLength: number; // in seconds
  pacingMultiplier: number;
}

/**
 * Platform presets response
 */
export interface PlatformPresetsResponse {
  platforms: PlatformPreset[];
}

/**
 * Request to reanalyze with different parameters
 */
export interface ReanalyzeRequest {
  optimizationLevel: 'Low' | 'Medium' | 'High' | 'Maximum';
  targetPlatform: string;
}

/**
 * Pacing optimization settings
 */
export interface PacingSettings {
  enabled: boolean;
  optimizationLevel: 'Conservative' | 'Moderate' | 'Aggressive';
  targetPlatform: string;
  minConfidence: number; // 0-100
  autoApply: boolean;
}

/**
 * Analysis state for UI
 */
export interface PacingAnalysisState {
  loading: boolean;
  error: string | null;
  data: PacingAnalysisResponse | null;
}
