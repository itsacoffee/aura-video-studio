/**
 * SSML Service - API integration for SSML planning, validation, and optimization
 */

import { post, get } from './api/apiClient';
import type { LineDto } from '@/types/api-v1';

export interface VoiceSpec {
  voiceName: string;
  voiceId?: string;
  rate: number;
  pitch: number;
  volume: number;
}

export interface SSMLPlanningRequest {
  scriptLines: LineDto[];
  targetProvider: string;
  voiceSpec: VoiceSpec;
  targetDurations: Record<number, number>;
  durationTolerance?: number;
  maxFittingIterations?: number;
  enableAggressiveAdjustments?: boolean;
}

export interface ProsodyAdjustments {
  rate: number;
  pitch: number;
  volume: number;
  pauses: Record<number, number>;
  emphasis: EmphasisSpan[];
  iterations: number;
}

export interface EmphasisSpan {
  startPosition: number;
  length: number;
  level: string;
}

export interface TimingMarker {
  offsetMs: number;
  name: string;
  metadata?: string;
}

export interface SSMLSegmentResult {
  sceneIndex: number;
  originalText: string;
  ssmlMarkup: string;
  estimatedDurationMs: number;
  targetDurationMs: number;
  deviationPercent: number;
  adjustments: ProsodyAdjustments;
  timingMarkers: TimingMarker[];
}

export interface DurationFittingStats {
  segmentsAdjusted: number;
  averageFitIterations: number;
  maxFitIterations: number;
  withinTolerancePercent: number;
  averageDeviation: number;
  maxDeviation: number;
  targetDurationSeconds: number;
  actualDurationSeconds: number;
}

export interface SSMLPlanningResult {
  segments: SSMLSegmentResult[];
  stats: DurationFittingStats;
  warnings: string[];
  planningDurationMs: number;
}

export interface SSMLValidationRequest {
  ssml: string;
  targetProvider: string;
}

export interface SSMLRepairSuggestion {
  issue: string;
  suggestion: string;
  canAutoFix: boolean;
}

export interface SSMLValidationResult {
  isValid: boolean;
  errors: string[];
  warnings: string[];
  repairSuggestions: SSMLRepairSuggestion[];
}

export interface SSMLRepairRequest {
  ssml: string;
  targetProvider: string;
}

export interface SSMLRepairResult {
  repairedSsml: string;
  wasRepaired: boolean;
  repairsApplied: string[];
}

export interface ProviderSSMLConstraints {
  supportedTags: string[];
  supportedProsodyAttributes: string[];
  minRate: number;
  maxRate: number;
  minPitch: number;
  maxPitch: number;
  minVolume: number;
  maxVolume: number;
  maxPauseDurationMs: number;
  supportsTimingMarkers: boolean;
  maxTextLength?: number;
}

/**
 * Plan SSML for script lines with duration targeting
 */
export async function planSSML(request: SSMLPlanningRequest): Promise<SSMLPlanningResult> {
  return await post<SSMLPlanningResult>('/api/ssml/plan', request);
}

/**
 * Validate SSML for provider compatibility
 */
export async function validateSSML(request: SSMLValidationRequest): Promise<SSMLValidationResult> {
  return await post<SSMLValidationResult>('/api/ssml/validate', request);
}

/**
 * Auto-repair invalid SSML
 */
export async function repairSSML(request: SSMLRepairRequest): Promise<SSMLRepairResult> {
  return await post<SSMLRepairResult>('/api/ssml/repair', request);
}

/**
 * Get provider-specific SSML constraints
 */
export async function getSSMLConstraints(provider: string): Promise<ProviderSSMLConstraints> {
  return await get<ProviderSSMLConstraints>(`/api/ssml/constraints/${provider}`);
}
