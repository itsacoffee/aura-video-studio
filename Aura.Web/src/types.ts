// Type definitions for Aura Video Studio

export interface HardwareCapabilities {
  tier: string;
  cpu: {
    cores: number;
    threads: number;
  };
  ram: {
    gb: number;
  };
  gpu?: {
    model: string;
    vramGB: number;
    vendor: string;
  };
  enableNVENC: boolean;
  enableSD: boolean;
  offlineOnly: boolean;
}

export interface RenderJob {
  id: string;
  status: string;
  progress: number;
  outputPath: string | null;
  createdAt: string;
}

export interface Profile {
  name: string;
  description: string;
}

export interface DownloadItem {
  name: string;
  version: string;
  url: string;
  sha256: string;
  sizeBytes: number;
  installPath: string;
  required: boolean;
}

export interface Brief {
  topic: string;
  audience: string;
  goal: string;
  tone: string;
  language: string;
  aspect: 'Widescreen16x9' | 'Vertical9x16' | 'Square1x1';
  ragConfiguration?: RagConfiguration;
  scriptGuidance?: string;
}

export interface PlanSpec {
  targetDurationMinutes: number;
  pacing: 'Chill' | 'Conversational' | 'Fast';
  density: 'Sparse' | 'Balanced' | 'Dense';
  style: string;
}

export interface VoiceSpec {
  voiceName: string;
  rate: number;
  pitch: number;
  pauseStyle: 'Auto' | 'None' | 'Breathier';
}

export interface BrandKitConfig {
  watermarkPath?: string;
  watermarkPosition?: string;
  watermarkOpacity: number;
  brandColor?: string;
  accentColor?: string;
}

export interface CaptionsConfig {
  enabled: boolean;
  format: 'srt' | 'vtt';
  burnIn: boolean;
  fontName: string;
  fontSize: number;
  primaryColor: string;
  outlineColor: string;
  outlineWidth: number;
  position: string;
}

export interface StockSourcesConfig {
  enablePexels: boolean;
  enablePixabay: boolean;
  enableUnsplash: boolean;
  enableLocalAssets: boolean;
  enableStableDiffusion: boolean;
  localAssetsDirectory?: string;
  stableDiffusionUrl?: string;
}

export interface PromptModifiers {
  additionalInstructions?: string;
  exampleStyle?: string;
  enableChainOfThought: boolean;
  promptVersion?: string;
}

export interface PromptPreview {
  systemPrompt: string;
  userPrompt: string;
  finalPrompt: string;
  substitutedVariables: Record<string, string>;
  promptVersion: string;
  estimatedTokens: number;
}

export interface FewShotExample {
  videoType: string;
  exampleName: string;
  description: string;
  sampleBrief: string;
  sampleOutput: string;
  keyTechniques: string[];
}

export interface PromptVersion {
  version: string;
  name: string;
  description: string;
  isDefault: boolean;
}

export interface ChainOfThoughtStage {
  stage: 'TopicAnalysis' | 'Outline' | 'FullScript';
  content: string;
  requiresUserReview: boolean;
  suggestedEdits?: string;
}

export interface PromptPreset {
  name: string;
  description: string;
  additionalInstructions?: string;
  exampleStyle?: string;
  enableChainOfThought: boolean;
  promptVersion?: string;
  createdAt: string;
  lastUsedAt?: string;
}

export interface WizardSettings {
  brief: Brief;
  planSpec: PlanSpec;
  brandKit: BrandKitConfig;
  captions: CaptionsConfig;
  stockSources: StockSourcesConfig;
  offlineMode: boolean;
  voiceSpec?: VoiceSpec;
  providerSelection?: PerStageProviderSelection;
}

export interface PerStageProviderSelection {
  script?: string;
  tts?: string;
  visuals?: string;
  upload?: string;
}

export interface PlannerRecommendations {
  outline: string;
  sceneCount: number;
  shotsPerScene: number;
  bRollPercentage: number;
  overlayDensity: number;
  readingLevel: number;
  voice: {
    rate: number;
    pitch: number;
    style: string;
  };
  music: {
    tempo: string;
    intensityCurve: string;
    genre: string;
  };
  captions: {
    position: string;
    fontSize: string;
    highlightKeywords: boolean;
  };
  thumbnailPrompt: string;
  seo: {
    title: string;
    description: string;
    tags: string[];
  };
  qualityScore?: number;
  providerUsed?: string;
  explainabilityNotes?: string;
}

// Re-export Scene from timeline types for backward compatibility
export type { TimelineScene as Scene } from './types/timeline';

// RAG (Retrieval-Augmented Generation) Configuration
export interface RagConfiguration {
  enabled: boolean;
  topK?: number;
  minimumScore?: number;
  maxContextTokens?: number;
  includeCitations?: boolean;
  tightenClaims?: boolean;
}

export interface Citation {
  number: number;
  source: string;
  title?: string;
  section?: string;
  pageNumber?: number;
}

export interface DocumentInfo {
  documentId: string;
  source: string;
  title?: string;
  chunkCount: number;
  createdAt: string;
}

export interface IndexStatistics {
  totalDocuments: number;
  totalChunks: number;
  totalSizeBytes: number;
  lastUpdated: string;
  documentsByFormat: Record<string, number>;
}

export interface IndexingResult {
  success: boolean;
  documentId: string;
  chunksCreated: number;
  processingTimeMs: number;
  warnings: string[];
}
