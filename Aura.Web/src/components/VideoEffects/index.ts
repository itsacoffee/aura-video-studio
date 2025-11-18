/**
 * Video Effects System Components
 *
 * A comprehensive suite of components for applying and managing
 * professional video effects, transitions, and filters.
 */

export { EffectsTimeline } from './EffectsTimeline';
export { EffectPropertiesPanel } from './EffectPropertiesPanel';
export { EffectsLibrary } from './EffectsLibrary';

// Re-export types for convenience
export type {
  VideoEffect,
  EffectPreset,
  EffectParameter,
  EffectCategory,
  EffectType,
  Keyframe,
  EasingFunction,
  EffectStack,
  ApplyEffectsRequest,
  ApplyEffectsResponse,
  CacheStatistics,
} from '../../types/videoEffects';
