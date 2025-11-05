/**
 * Types for offline provider availability and status
 */

export interface OfflineProviderStatus {
  name: string;
  isAvailable: boolean;
  message: string;
  version?: string;
  details: Record<string, unknown>;
  recommendations: string[];
  installationGuideUrl?: string;
}

export interface OfflineProvidersStatus {
  piper: OfflineProviderStatus;
  mimic3: OfflineProviderStatus;
  ollama: OfflineProviderStatus;
  stableDiffusion: OfflineProviderStatus;
  windowsTts: OfflineProviderStatus;
  checkedAt: string;
  hasTtsProvider: boolean;
  hasLlmProvider: boolean;
  hasImageProvider: boolean;
  isFullyOperational: boolean;
}

export type OfflineProviderType = 'piper' | 'mimic3' | 'ollama' | 'stableDiffusion' | 'windowsTts';

export interface OfflineProviderInstallGuide {
  providerName: string;
  description: string;
  steps: string[];
  downloadUrl: string;
  documentationUrl?: string;
}
