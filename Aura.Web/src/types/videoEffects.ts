// Video Effects Type Definitions

export type EffectType =
  | 'Transition'
  | 'Filter'
  | 'TextAnimation'
  | 'Overlay'
  | 'Transform'
  | 'ColorCorrection'
  | 'AudioEffect'
  | 'Composite';

export type EffectCategory =
  | 'Basic'
  | 'Artistic'
  | 'ColorGrading'
  | 'Blur'
  | 'Vintage'
  | 'Modern'
  | 'Cinematic'
  | 'Custom';

export type EasingFunction =
  | 'Linear'
  | 'EaseIn'
  | 'EaseOut'
  | 'EaseInOut'
  | 'EaseInCubic'
  | 'EaseOutCubic'
  | 'EaseInOutCubic'
  | 'EaseInQuad'
  | 'EaseOutQuad'
  | 'EaseInOutQuad'
  | 'Bounce'
  | 'Elastic';

export interface Keyframe {
  time: number;
  parameterName: string;
  value: any;
  easing: EasingFunction;
  interpolationMode?: string;
}

export interface VideoEffect {
  id: string;
  name: string;
  description?: string;
  type: EffectType;
  category: EffectCategory;
  startTime: number;
  duration: number;
  intensity: number;
  enabled: boolean;
  layer: number;
  keyframes: Keyframe[];
  parameters: Record<string, any>;
  tags: string[];
}

export interface EffectParameter {
  name: string;
  label: string;
  description?: string;
  type: 'number' | 'color' | 'boolean' | 'text' | 'select';
  defaultValue: any;
  value?: any;
  min?: number;
  max?: number;
  step?: number;
  options?: string[];
  animatable: boolean;
  unit?: string;
}

export interface EffectPreset {
  id: string;
  name: string;
  description?: string;
  category: EffectCategory;
  thumbnailUrl?: string;
  isBuiltIn: boolean;
  isFavorite: boolean;
  tags: string[];
  effects: VideoEffect[];
  parameters: Record<string, EffectParameter>;
  usageCount: number;
  createdAt: Date;
  modifiedAt: Date;
}

export interface EffectStack {
  id: string;
  name: string;
  effects: VideoEffect[];
  blendMode: string;
  opacity: number;
  enabled: boolean;
}

export interface ApplyEffectsRequest {
  inputPath: string;
  outputPath?: string;
  effects: VideoEffect[];
  useCache?: boolean;
}

export interface ApplyEffectsResponse {
  outputPath: string;
  success: boolean;
  fromCache: boolean;
}

export interface ApplyPresetRequest {
  inputPath: string;
  outputPath?: string;
  presetId: string;
}

export interface EffectPreviewRequest {
  inputPath: string;
  effect: VideoEffect;
  previewDurationSeconds?: number;
}

export interface EffectPreviewResponse {
  previewPath: string;
  success: boolean;
}

export interface ValidationResponse {
  isValid: boolean;
  errorMessage?: string;
}

export interface CacheStatistics {
  totalEntries: number;
  totalSizeBytes: number;
  hitCount: number;
  missCount: number;
  hitRate: number;
  totalRequests: number;
}
