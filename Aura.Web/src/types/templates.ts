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
