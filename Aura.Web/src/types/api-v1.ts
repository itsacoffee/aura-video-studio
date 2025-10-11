/**
 * AUTO-GENERATED (Ready for Generation) - DO NOT EDIT MANUALLY
 * 
 * API V1 Type Definitions
 * 
 * These types should be generated from the OpenAPI spec using:
 *   node scripts/contract/generate-api-v1-types.js
 *   or
 *   .\scripts\contract\generate-api-v1-types.ps1
 * 
 * For now, types are manually maintained but should match
 * Aura.Api.Models.ApiModels.V1 exactly.
 * 
 * TODO: Run generation script to fully automate type sync
 */

// ============================================================================
// ENUMS
// ============================================================================

/**
 * Video pacing style - affects narration speed and scene transitions
 * Canonical values: "Chill", "Conversational", "Fast"
 */
export enum Pacing {
  Chill = "Chill",
  Conversational = "Conversational",
  Fast = "Fast"
}

/**
 * Content density - affects information per scene
 * Canonical values: "Sparse", "Balanced", "Dense"
 * Legacy alias: "Normal" -> "Balanced"
 */
export enum Density {
  Sparse = "Sparse",
  Balanced = "Balanced",
  Dense = "Dense"
}

/**
 * Video aspect ratio
 * Canonical values: "Widescreen16x9", "Vertical9x16", "Square1x1"
 * Legacy aliases: "16:9" -> "Widescreen16x9", "9:16" -> "Vertical9x16", "1:1" -> "Square1x1"
 */
export enum Aspect {
  Widescreen16x9 = "Widescreen16x9",
  Vertical9x16 = "Vertical9x16",
  Square1x1 = "Square1x1"
}

/**
 * TTS pause style between sentences
 * Canonical values: "Natural", "Short", "Long", "Dramatic"
 */
export enum PauseStyle {
  Natural = "Natural",
  Short = "Short",
  Long = "Long",
  Dramatic = "Dramatic"
}

/**
 * Provider mode selection
 */
export enum ProviderMode {
  Free = "Free",
  Pro = "Pro"
}

/**
 * Hardware tier classification
 */
export enum HardwareTier {
  A = "A", // High (≥12GB VRAM or NVIDIA 40/50-series)
  B = "B", // Upper-mid (8-12GB VRAM)
  C = "C", // Mid (6-8GB VRAM)
  D = "D"  // Entry (≤4-6GB VRAM or no GPU)
}

// ============================================================================
// REQUEST DTOS
// ============================================================================

export interface PlanRequest {
  targetDurationMinutes: number;
  pacing: Pacing;
  density: Density;
  style: string;
}

export interface ScriptRequest {
  topic: string;
  audience: string;
  goal: string;
  tone: string;
  language: string;
  aspect: Aspect;
  targetDurationMinutes: number;
  pacing: Pacing;
  density: Density;
  style: string;
  providerTier?: string | null;
}

export interface LineDto {
  sceneIndex: number;
  text: string;
  startSeconds: number;
  durationSeconds: number;
}

export interface TtsRequest {
  lines: LineDto[];
  voiceName: string;
  rate: number;
  pitch: number;
  pauseStyle: PauseStyle;
}

export interface ComposeRequest {
  timelineJson: string;
}

export interface RenderRequest {
  timelineJson: string;
  presetName: string;
}

export interface ApplyProfileRequest {
  profileName: string;
}

export interface ApiKeysRequest {
  openAiKey?: string | null;
  elevenLabsKey?: string | null;
  pexelsKey?: string | null;
  stabilityAiKey?: string | null;
}

export interface ProviderPathsRequest {
  stableDiffusionUrl?: string | null;
  ollamaUrl?: string | null;
  ffmpegPath?: string | null;
  ffprobePath?: string | null;
  outputDirectory?: string | null;
}

export interface ProviderTestRequest {
  url?: string | null;
  path?: string | null;
}

export interface ConstraintsDto {
  maxSceneCount?: number | null;
  minSceneCount?: number | null;
  maxBRollPercentage?: number | null;
  maxReadingLevel?: number | null;
}

export interface RecommendationsRequestDto {
  topic: string;
  audience?: string | null;
  goal?: string | null;
  tone?: string | null;
  language?: string | null;
  aspect?: Aspect | null;
  targetDurationMinutes: number;
  pacing?: Pacing | null;
  density?: Density | null;
  style?: string | null;
  audiencePersona?: string | null;
  constraints?: ConstraintsDto | null;
}

export interface AssetSearchRequest {
  provider: string;
  query: string;
  count: number;
  apiKey?: string | null;
  localDirectory?: string | null;
}

export interface AssetGenerateRequest {
  prompt: string;
  style?: string | null;
  aspect?: Aspect | null;
}

// ============================================================================
// RESPONSE DTOS
// ============================================================================

export interface RenderJobDto {
  id: string;
  status: string;
  progress: number;
  outputPath?: string | null;
  createdAt: string;
}

// ============================================================================
// HELPER TYPES
// ============================================================================

/**
 * Type-safe enum value type
 */
export type PacingValue = `${Pacing}`;
export type DensityValue = `${Density}`;
export type AspectValue = `${Aspect}`;
export type PauseStyleValue = `${PauseStyle}`;

/**
 * Enum value arrays for validation and UI dropdowns
 */
export const PacingValues = Object.values(Pacing);
export const DensityValues = Object.values(Density);
export const AspectValues = Object.values(Aspect);
export const PauseStyleValues = Object.values(PauseStyle);
