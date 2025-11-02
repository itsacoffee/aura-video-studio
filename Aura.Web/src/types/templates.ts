/**
 * Types for project templates and presets
 */

export enum TemplateCategory {
  YouTube = 'YouTube',
  SocialMedia = 'SocialMedia',
  Business = 'Business',
  Creative = 'Creative',
}

export interface TemplateListItem {
  id: string;
  name: string;
  description: string;
  category: TemplateCategory;
  subCategory: string;
  previewImage: string;
  previewVideo: string;
  tags: string[];
  usageCount: number;
  rating: number;
  isSystemTemplate: boolean;
  isCommunityTemplate: boolean;
}

export interface ProjectTemplate {
  id: string;
  name: string;
  description: string;
  category: TemplateCategory;
  subCategory: string;
  previewImage: string;
  previewVideo: string;
  tags: string[];
  templateData: string;
  createdAt: string;
  updatedAt: string;
  author: string;
  isSystemTemplate: boolean;
  isCommunityTemplate: boolean;
  usageCount: number;
  rating: number;
  ratingCount: number;
}

export interface TemplateStructure {
  tracks: TemplateTrack[];
  placeholders: TemplatePlaceholder[];
  textOverlays: TemplateTextOverlay[];
  transitions: TemplateTransition[];
  effects: TemplateEffect[];
  musicTrack?: TemplateMusicTrack;
  duration: number;
  settings: TemplateSettings;
}

export interface TemplateTrack {
  id: string;
  label: string;
  type: 'video' | 'audio';
}

export interface TemplatePlaceholder {
  id: string;
  trackId: string;
  startTime: number;
  duration: number;
  type: 'video' | 'audio' | 'image';
  placeholderText: string;
  previewUrl: string;
}

export interface TemplateTextOverlay {
  id: string;
  trackId: string;
  startTime: number;
  duration: number;
  text: string;
  font: string;
  fontSize: number;
  color: string;
  animation: string;
  position: TemplatePosition;
}

export interface TemplatePosition {
  x: number;
  y: number;
  alignment: 'center' | 'left' | 'right';
}

export interface TemplateTransition {
  id: string;
  type: string;
  duration: number;
  direction: string;
  position: number;
}

export interface TemplateEffect {
  id: string;
  name: string;
  type: string;
  parameters: Record<string, unknown>;
  appliesTo: string;
}

export interface TemplateMusicTrack {
  trackId: string;
  placeholderUrl: string;
  startTime: number;
  duration: number;
  volume: number;
  fadeIn: boolean;
  fadeOut: boolean;
}

export interface TemplateSettings {
  width: number;
  height: number;
  frameRate: number;
  aspectRatio: string;
}

export interface EffectPreset {
  id: string;
  name: string;
  description: string;
  category: string;
  effects: TemplateEffect[];
  previewImage: string;
}

export interface TransitionPreset {
  id: string;
  name: string;
  type: string;
  defaultDuration: number;
  direction: string;
  previewVideo: string;
}

export interface TitleTemplate {
  id: string;
  name: string;
  category: string;
  textLayers: TemplateTextOverlay[];
  duration: number;
  previewVideo: string;
}

export interface CreateFromTemplateRequest {
  templateId: string;
  projectName: string;
}

export interface SaveAsTemplateRequest {
  name: string;
  description: string;
  category: TemplateCategory;
  subCategory: string;
  tags: string[];
  projectData: string;
  previewImage: string;
}

export interface PaginatedTemplatesResponse {
  items: TemplateListItem[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

/**
 * Custom video template types
 */

export interface CustomVideoTemplate {
  id: string;
  name: string;
  description: string;
  category: string;
  tags: string[];
  createdAt: string;
  updatedAt: string;
  author: string;
  isDefault: boolean;
  scriptStructure: ScriptStructureConfig;
  videoStructure: VideoStructureConfig;
  llmPipeline: LLMPipelineConfig;
  visualPrefs: VisualPreferences;
}

export interface ScriptStructureConfig {
  sections: ScriptSection[];
}

export interface ScriptSection {
  id: string;
  name: string;
  description: string;
  order: number;
  isRequired: boolean;
  isOptional: boolean;
  tone: string;
  style: string;
  minDuration: number;
  maxDuration: number;
}

export interface VideoStructureConfig {
  typicalDuration: number;
  pacing: string;
  sceneCount: number;
  transitionStyle: string;
  useBRoll: boolean;
  musicStyle: string;
  musicVolume: number;
}

export interface LLMPipelineConfig {
  sectionPrompts: SectionPromptConfig[];
  defaultTemperature: number;
  defaultMaxTokens: number;
  defaultModel: string;
  keywordsToEmphasize: string[];
  keywordsToAvoid: string[];
}

export interface SectionPromptConfig {
  sectionId: string;
  systemPrompt: string;
  userPromptTemplate: string;
  temperature: number;
  maxTokens: number;
  model: string;
  variables: Record<string, string>;
}

export interface VisualPreferences {
  imageGenerationPromptTemplate: string;
  colorScheme: string;
  aestheticGuidelines: string[];
  textOverlayStyle: string;
  transitionPreference: string;
  customStyles: Record<string, string>;
}

export interface CreateCustomTemplateRequest {
  name: string;
  description: string;
  category: string;
  tags: string[];
  scriptStructure: ScriptStructureConfig;
  videoStructure: VideoStructureConfig;
  llmPipeline: LLMPipelineConfig;
  visualPrefs: VisualPreferences;
}

export interface UpdateCustomTemplateRequest {
  name: string;
  description: string;
  category: string;
  tags: string[];
  scriptStructure: ScriptStructureConfig;
  videoStructure: VideoStructureConfig;
  llmPipeline: LLMPipelineConfig;
  visualPrefs: VisualPreferences;
}

export interface TemplateExportData {
  version: string;
  template: CustomVideoTemplate;
  exportedAt: string;
}
