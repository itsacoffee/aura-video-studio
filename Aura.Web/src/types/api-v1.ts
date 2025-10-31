/**
 * API V1 Type Definitions
 *
 * These types match Aura.Api.Models.ApiModels.V1 exactly.
 * Keep in sync with backend types when making changes.
 */

// ============================================================================
// ENUMS
// ============================================================================

/**
 * Video pacing style - affects narration speed and scene transitions
 * Canonical values: "Chill", "Conversational", "Fast"
 */
export enum Pacing {
  Chill = 'Chill',
  Conversational = 'Conversational',
  Fast = 'Fast',
}

/**
 * Content density - affects information per scene
 * Canonical values: "Sparse", "Balanced", "Dense"
 * Legacy alias: "Normal" -> "Balanced"
 */
export enum Density {
  Sparse = 'Sparse',
  Balanced = 'Balanced',
  Dense = 'Dense',
}

/**
 * Video aspect ratio
 * Canonical values: "Widescreen16x9", "Vertical9x16", "Square1x1"
 * Legacy aliases: "16:9" -> "Widescreen16x9", "9:16" -> "Vertical9x16", "1:1" -> "Square1x1"
 */
export enum Aspect {
  Widescreen16x9 = 'Widescreen16x9',
  Vertical9x16 = 'Vertical9x16',
  Square1x1 = 'Square1x1',
}

/**
 * TTS pause style between sentences
 * Canonical values: "Natural", "Short", "Long", "Dramatic"
 */
export enum PauseStyle {
  Natural = 'Natural',
  Short = 'Short',
  Long = 'Long',
  Dramatic = 'Dramatic',
}

/**
 * Provider mode selection
 */
export enum ProviderMode {
  Free = 'Free',
  Pro = 'Pro',
}

/**
 * Hardware tier classification
 */
export enum HardwareTier {
  A = 'A', // High (≥12GB VRAM or NVIDIA 40/50-series)
  B = 'B', // Upper-mid (8-12GB VRAM)
  C = 'C', // Mid (6-8GB VRAM)
  D = 'D', // Entry (≤4-6GB VRAM or no GPU)
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
  audienceProfileId?: string | null;
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

// ============================================================================
// OLLAMA TYPES
// ============================================================================

export interface OllamaModel {
  name: string;
  size: number;
  sizeGB: number;
  modifiedAt: string | null;
  digest: string | null;
}

export interface OllamaModelsResponse {
  models: OllamaModel[];
  baseUrl: string;
}

// ============================================================================
// AUDIENCE PROFILE TYPES
// ============================================================================

export interface AudienceProfileDto {
  id: string | null;
  name: string;
  description: string | null;
  ageRange: AgeRangeDto | null;
  educationLevel: string | null;
  profession: string | null;
  industry: string | null;
  expertiseLevel: string | null;
  incomeBracket: string | null;
  geographicRegion: string | null;
  languageFluency: LanguageFluencyDto | null;
  interests: string[] | null;
  painPoints: string[] | null;
  motivations: string[] | null;
  culturalBackground: CulturalBackgroundDto | null;
  preferredLearningStyle: string | null;
  attentionSpan: AttentionSpanDto | null;
  technicalComfort: string | null;
  accessibilityNeeds: AccessibilityNeedsDto | null;
  isTemplate: boolean;
  tags: string[] | null;
  version: number;
  createdAt: string | null;
  updatedAt: string | null;
  isFavorite: boolean;
  folderPath: string | null;
  usageCount: number;
  lastUsedAt: string | null;
}

export interface AgeRangeDto {
  minAge: number;
  maxAge: number;
  displayName: string;
  contentRating: string;
}

export interface LanguageFluencyDto {
  language: string;
  level: string;
}

export interface CulturalBackgroundDto {
  sensitivities: string[] | null;
  tabooTopics: string[] | null;
  preferredCommunicationStyle: string;
}

export interface AttentionSpanDto {
  preferredDurationMinutes: number;
  displayName: string;
}

export interface AccessibilityNeedsDto {
  requiresCaptions: boolean;
  requiresAudioDescriptions: boolean;
  requiresHighContrast: boolean;
  requiresSimplifiedLanguage: boolean;
  requiresLargeText: boolean;
}

export interface CreateAudienceProfileRequest {
  profile: AudienceProfileDto;
}

export interface UpdateAudienceProfileRequest {
  profile: AudienceProfileDto;
}

export interface AudienceProfileResponse {
  profile: AudienceProfileDto;
  validation: ValidationResultDto | null;
}

export interface AudienceProfileListResponse {
  profiles: AudienceProfileDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface ValidationResultDto {
  isValid: boolean;
  errors: ValidationIssueDto[];
  warnings: ValidationIssueDto[];
  infos: ValidationIssueDto[];
}

export interface ValidationIssueDto {
  severity: string;
  field: string;
  message: string;
  suggestedFix: string | null;
}

export interface AnalyzeAudienceRequest {
  scriptText: string;
}

export interface AnalyzeAudienceResponse {
  inferredProfile: AudienceProfileDto;
  confidenceScore: number;
  reasoningFactors: string[];
}

export interface MoveToFolderRequest {
  folderPath: string | null;
}

export interface FolderListResponse {
  folders: string[];
}

export interface ExportProfileResponse {
  json: string;
}

export interface ImportProfileRequest {
  json: string;
}

// Enum values for audience profile fields
export const EducationLevels = [
  'HighSchool',
  'SomeCollege',
  'AssociateDegree',
  'BachelorDegree',
  'MasterDegree',
  'Doctorate',
  'Vocational',
  'SelfTaught',
  'InProgress',
] as const;

export const ExpertiseLevels = [
  'CompleteBeginner',
  'Novice',
  'Intermediate',
  'Advanced',
  'Expert',
  'Professional',
] as const;

export const IncomeBrackets = [
  'NotSpecified',
  'LowIncome',
  'MiddleIncome',
  'UpperMiddleIncome',
  'HighIncome',
] as const;

export const GeographicRegions = [
  'Global',
  'NorthAmerica',
  'Europe',
  'Asia',
  'LatinAmerica',
  'MiddleEast',
  'Africa',
  'Oceania',
] as const;

export const FluencyLevels = ['Native', 'Fluent', 'Intermediate', 'Beginner'] as const;

export const CommunicationStyles = [
  'Direct',
  'Indirect',
  'Formal',
  'Casual',
  'Humorous',
  'Professional',
] as const;

export const LearningStyles = [
  'Visual',
  'Auditory',
  'Kinesthetic',
  'ReadingWriting',
  'Multimodal',
] as const;

export const TechnicalComfortLevels = [
  'NonTechnical',
  'BasicUser',
  'Moderate',
  'TechSavvy',
  'Expert',
] as const;

export const ContentRatings = ['ChildSafe', 'TeenAppropriate', 'Adult'] as const;
