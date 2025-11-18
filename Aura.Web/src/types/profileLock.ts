/**
 * Types for Provider Profile Lock functionality
 */

export interface ProfileLockMetadata {
  createdByUser?: string;
  reason?: string;
  tags: string[];
  source: string;
  allowManualFallback: boolean;
  maxWaitBeforeFallbackSeconds?: number;
}

export interface ProviderProfileLock {
  jobId: string;
  providerName: string;
  providerType: string;
  isEnabled: boolean;
  createdAt: string;
  offlineModeEnabled: boolean;
  applicableStages: string[];
  metadata: ProfileLockMetadata;
  source: string;
}

export interface SetProfileLockRequest {
  jobId: string;
  providerName: string;
  providerType: string;
  isEnabled: boolean;
  offlineModeEnabled?: boolean;
  applicableStages?: string[];
  isSessionLevel?: boolean;
  metadata?: ProfileLockMetadata;
}

export interface UnlockProfileLockRequest {
  jobId: string;
  isSessionLevel?: boolean;
  reason?: string;
}

export interface ProfileLockStatistics {
  totalSessionLocks: number;
  totalProjectLocks: number;
  enabledSessionLocks: number;
  enabledProjectLocks: number;
  offlineModeLocksCount: number;
}

export interface ProfileLockStatus {
  jobId?: string;
  hasActiveLock: boolean;
  activeLock?: ProviderProfileLock;
  statistics: ProfileLockStatistics;
}

export interface OfflineCompatibilityResponse {
  providerName: string;
  isCompatible: boolean;
  message?: string;
  offlineCompatibleProviders: string[];
}

export interface ValidateProviderRequest {
  jobId: string;
  providerName: string;
  stageName: string;
  providerRequiresNetwork: boolean;
}

export interface ValidateProviderResponse {
  isValid: boolean;
  validationError?: string;
  activeLock?: ProviderProfileLock;
}

/**
 * Provider status for UI display
 */
export type ProviderStatusState =
  | 'active' // Provider is working normally
  | 'waiting' // Extended wait (30-180s)
  | 'extended-wait' // Deep wait (180s+)
  | 'stall-suspected' // No heartbeat detected
  | 'error' // Fatal error occurred
  | 'user-requested-fallback'; // User initiated fallback

export interface ProviderStatusInfo {
  state: ProviderStatusState;
  providerName: string;
  providerType: string;
  elapsedTimeSeconds: number;
  timeSinceLastHeartbeatSeconds?: number;
  heartbeatCount: number;
  progress?: {
    tokensGenerated?: number;
    chunksProcessed?: number;
    percentComplete?: number;
    message?: string;
  };
  canManuallyFallback: boolean;
  estimatedNextCheckSeconds?: number;
}

export const PIPELINE_STAGES = [
  'planning',
  'script_generation',
  'refinement',
  'tts',
  'visual_prompts',
  'rendering',
] as const;

export type PipelineStage = (typeof PIPELINE_STAGES)[number];
