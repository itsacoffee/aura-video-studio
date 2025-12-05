/**
 * OpenCut Effects Store
 *
 * Manages video effects including a library of built-in effects,
 * applied effects on clips, and effect parameters. Provides color
 * correction, blur, stylize effects and more.
 */

import { create } from 'zustand';
import type { EffectCategory, EffectParameter } from '../types/opencut';

function generateId(): string {
  return `effect-${Date.now()}-${Math.random().toString(36).substring(2, 9)}`;
}

/**
 * Extended effect definition with keyframe support.
 */
export interface EffectDefinition {
  /** Unique effect identifier */
  id: string;
  /** Display name */
  name: string;
  /** Description */
  description: string;
  /** Category for grouping */
  category: EffectCategory;
  /** Thumbnail preview URL */
  thumbnailUrl?: string;
  /** Configurable parameters */
  parameters: EffectParameter[];
  /** Whether this is a premium effect */
  isPremium?: boolean;
  /** GPU acceleration required */
  requiresGpu?: boolean;
  /** Whether effect parameters support keyframes */
  supportsKeyframes?: boolean;
}

/**
 * Applied effect on a clip with ordering.
 */
export interface AppliedEffect {
  /** Unique instance identifier */
  id: string;
  /** Reference to effect definition */
  effectId: string;
  /** Clip ID this effect is applied to */
  clipId: string;
  /** Order in the effect stack (lower = applied first) */
  order: number;
  /** Whether effect is enabled */
  enabled: boolean;
  /** Parameter values (key is parameter id) */
  parameters: Record<string, string | number | boolean>;
}

// Built-in effect definitions
export const BUILTIN_EFFECTS: EffectDefinition[] = [
  // Color Correction Effects
  {
    id: 'exposure',
    name: 'Exposure',
    description: 'Adjust exposure, offset, and gamma',
    category: 'color',
    supportsKeyframes: true,
    parameters: [
      {
        id: 'exposure',
        name: 'Exposure',
        type: 'number',
        defaultValue: 0,
        min: -3,
        max: 3,
        step: 0.1,
        keyframeable: true,
      },
      {
        id: 'offset',
        name: 'Offset',
        type: 'number',
        defaultValue: 0,
        min: -1,
        max: 1,
        step: 0.01,
        keyframeable: true,
      },
      {
        id: 'gamma',
        name: 'Gamma',
        type: 'number',
        defaultValue: 1,
        min: 0.1,
        max: 3,
        step: 0.1,
        keyframeable: true,
      },
    ],
  },
  {
    id: 'color-balance',
    name: 'Color Balance',
    description: 'Adjust temperature, tint, saturation, and vibrance',
    category: 'color',
    supportsKeyframes: true,
    parameters: [
      {
        id: 'temperature',
        name: 'Temperature',
        type: 'number',
        defaultValue: 0,
        min: -100,
        max: 100,
        step: 1,
        keyframeable: true,
      },
      {
        id: 'tint',
        name: 'Tint',
        type: 'number',
        defaultValue: 0,
        min: -100,
        max: 100,
        step: 1,
        keyframeable: true,
      },
      {
        id: 'saturation',
        name: 'Saturation',
        type: 'number',
        defaultValue: 100,
        min: 0,
        max: 200,
        step: 1,
        unit: '%',
        keyframeable: true,
      },
      {
        id: 'vibrance',
        name: 'Vibrance',
        type: 'number',
        defaultValue: 0,
        min: -100,
        max: 100,
        step: 1,
        keyframeable: true,
      },
    ],
  },
  {
    id: 'contrast',
    name: 'Contrast',
    description: 'Adjust contrast, brightness, highlights, and shadows',
    category: 'color',
    supportsKeyframes: true,
    parameters: [
      {
        id: 'contrast',
        name: 'Contrast',
        type: 'number',
        defaultValue: 0,
        min: -100,
        max: 100,
        step: 1,
        keyframeable: true,
      },
      {
        id: 'brightness',
        name: 'Brightness',
        type: 'number',
        defaultValue: 0,
        min: -100,
        max: 100,
        step: 1,
        keyframeable: true,
      },
      {
        id: 'highlights',
        name: 'Highlights',
        type: 'number',
        defaultValue: 0,
        min: -100,
        max: 100,
        step: 1,
        keyframeable: true,
      },
      {
        id: 'shadows',
        name: 'Shadows',
        type: 'number',
        defaultValue: 0,
        min: -100,
        max: 100,
        step: 1,
        keyframeable: true,
      },
    ],
  },
  {
    id: 'hue-saturation',
    name: 'Hue/Saturation',
    description: 'Adjust hue rotation and saturation',
    category: 'color',
    supportsKeyframes: true,
    parameters: [
      {
        id: 'hue',
        name: 'Hue',
        type: 'number',
        defaultValue: 0,
        min: -180,
        max: 180,
        step: 1,
        unit: '°',
        keyframeable: true,
      },
      {
        id: 'saturation',
        name: 'Saturation',
        type: 'number',
        defaultValue: 0,
        min: -100,
        max: 100,
        step: 1,
        keyframeable: true,
      },
      {
        id: 'lightness',
        name: 'Lightness',
        type: 'number',
        defaultValue: 0,
        min: -100,
        max: 100,
        step: 1,
        keyframeable: true,
      },
    ],
  },
  // Blur Effects
  {
    id: 'gaussian-blur',
    name: 'Gaussian Blur',
    description: 'Apply smooth Gaussian blur',
    category: 'blur',
    supportsKeyframes: true,
    requiresGpu: true,
    parameters: [
      {
        id: 'radius',
        name: 'Radius',
        type: 'number',
        defaultValue: 10,
        min: 0,
        max: 100,
        step: 1,
        unit: 'px',
        keyframeable: true,
      },
    ],
  },
  {
    id: 'directional-blur',
    name: 'Directional Blur',
    description: 'Apply motion-like directional blur',
    category: 'blur',
    supportsKeyframes: true,
    requiresGpu: true,
    parameters: [
      {
        id: 'amount',
        name: 'Amount',
        type: 'number',
        defaultValue: 20,
        min: 0,
        max: 100,
        step: 1,
        unit: 'px',
        keyframeable: true,
      },
      {
        id: 'angle',
        name: 'Angle',
        type: 'number',
        defaultValue: 0,
        min: -180,
        max: 180,
        step: 1,
        unit: '°',
        keyframeable: true,
      },
    ],
  },
  // Stylize Effects
  {
    id: 'vignette',
    name: 'Vignette',
    description: 'Add cinematic vignette effect',
    category: 'stylize',
    supportsKeyframes: true,
    parameters: [
      {
        id: 'amount',
        name: 'Amount',
        type: 'number',
        defaultValue: 50,
        min: 0,
        max: 100,
        step: 1,
        unit: '%',
        keyframeable: true,
      },
      {
        id: 'size',
        name: 'Size',
        type: 'number',
        defaultValue: 50,
        min: 0,
        max: 100,
        step: 1,
        unit: '%',
        keyframeable: true,
      },
      {
        id: 'softness',
        name: 'Softness',
        type: 'number',
        defaultValue: 50,
        min: 0,
        max: 100,
        step: 1,
        unit: '%',
        keyframeable: true,
      },
    ],
  },
  {
    id: 'grain',
    name: 'Film Grain',
    description: 'Add film-like grain texture',
    category: 'stylize',
    supportsKeyframes: true,
    parameters: [
      {
        id: 'amount',
        name: 'Amount',
        type: 'number',
        defaultValue: 25,
        min: 0,
        max: 100,
        step: 1,
        unit: '%',
        keyframeable: true,
      },
      {
        id: 'size',
        name: 'Size',
        type: 'number',
        defaultValue: 1,
        min: 0.5,
        max: 3,
        step: 0.1,
        keyframeable: true,
      },
    ],
  },
  {
    id: 'sharpen',
    name: 'Sharpen',
    description: 'Sharpen image details',
    category: 'stylize',
    supportsKeyframes: true,
    parameters: [
      {
        id: 'amount',
        name: 'Amount',
        type: 'number',
        defaultValue: 50,
        min: 0,
        max: 100,
        step: 1,
        unit: '%',
        keyframeable: true,
      },
      {
        id: 'radius',
        name: 'Radius',
        type: 'number',
        defaultValue: 1,
        min: 0.5,
        max: 5,
        step: 0.1,
        unit: 'px',
        keyframeable: true,
      },
    ],
  },
  {
    id: 'glow',
    name: 'Glow',
    description: 'Add soft glow effect',
    category: 'stylize',
    supportsKeyframes: true,
    requiresGpu: true,
    parameters: [
      {
        id: 'intensity',
        name: 'Intensity',
        type: 'number',
        defaultValue: 50,
        min: 0,
        max: 100,
        step: 1,
        unit: '%',
        keyframeable: true,
      },
      {
        id: 'radius',
        name: 'Radius',
        type: 'number',
        defaultValue: 20,
        min: 0,
        max: 100,
        step: 1,
        unit: 'px',
        keyframeable: true,
      },
      {
        id: 'threshold',
        name: 'Threshold',
        type: 'number',
        defaultValue: 50,
        min: 0,
        max: 100,
        step: 1,
        unit: '%',
        keyframeable: true,
      },
    ],
  },
];

interface OpenCutEffectsState {
  /** All available effect definitions */
  definitions: EffectDefinition[];
  /** Applied effects on clips */
  applied: AppliedEffect[];
  /** Currently selected effect ID */
  selectedEffectId: string | null;
}

interface OpenCutEffectsActions {
  /**
   * Apply an effect to a clip.
   * Returns the applied effect ID.
   */
  applyEffect: (effectId: string, clipId: string) => string;

  /**
   * Remove an applied effect by ID.
   */
  removeEffect: (appliedId: string) => void;

  /**
   * Update an effect parameter value.
   */
  updateEffectParameter: (
    appliedId: string,
    paramName: string,
    value: string | number | boolean
  ) => void;

  /**
   * Reorder effects for a clip.
   * Pass the effect IDs in the desired order.
   */
  reorderEffects: (clipId: string, effectIds: string[]) => void;

  /**
   * Toggle effect enabled/disabled state.
   */
  toggleEffectEnabled: (appliedId: string) => void;

  /**
   * Duplicate an applied effect.
   * Returns the new effect ID or null if failed.
   */
  duplicateEffect: (appliedId: string) => string | null;

  /**
   * Select an effect by its applied ID.
   */
  selectEffect: (appliedId: string | null) => void;

  /**
   * Get all effects applied to a specific clip.
   */
  getEffectsForClip: (clipId: string) => AppliedEffect[];

  /**
   * Remove all effects for a specific clip.
   */
  removeEffectsForClip: (clipId: string) => void;

  /**
   * Get an effect definition by ID.
   */
  getEffectDefinition: (effectId: string) => EffectDefinition | undefined;

  /**
   * Get all effect definitions in a specific category.
   */
  getDefinitionsByCategory: (category: EffectCategory) => EffectDefinition[];

  /**
   * Get the currently selected applied effect.
   */
  getSelectedEffect: () => AppliedEffect | undefined;

  /**
   * Reset effect parameters to defaults.
   */
  resetEffectParameters: (appliedId: string) => void;

  /**
   * Clear all applied effects.
   */
  clearAllEffects: () => void;
}

export type OpenCutEffectsStore = OpenCutEffectsState & OpenCutEffectsActions;

export const useOpenCutEffectsStore = create<OpenCutEffectsStore>((set, get) => ({
  definitions: BUILTIN_EFFECTS,
  applied: [],
  selectedEffectId: null,

  applyEffect: (effectId, clipId) => {
    const definition = get().definitions.find((d) => d.id === effectId);
    if (!definition) return '';

    const id = generateId();
    const clipEffects = get().getEffectsForClip(clipId);

    const applied: AppliedEffect = {
      id,
      effectId,
      clipId,
      order: clipEffects.length,
      enabled: true,
      parameters: Object.fromEntries(
        definition.parameters.map((param) => [param.id, param.defaultValue])
      ),
    };

    set((state) => ({ applied: [...state.applied, applied] }));
    return id;
  },

  removeEffect: (appliedId) => {
    set((state) => ({
      applied: state.applied.filter((e) => e.id !== appliedId),
      selectedEffectId: state.selectedEffectId === appliedId ? null : state.selectedEffectId,
    }));
  },

  updateEffectParameter: (appliedId, paramName, value) => {
    set((state) => ({
      applied: state.applied.map((e) =>
        e.id === appliedId ? { ...e, parameters: { ...e.parameters, [paramName]: value } } : e
      ),
    }));
  },

  reorderEffects: (clipId, effectIds) => {
    set((state) => ({
      applied: state.applied.map((e) => {
        if (e.clipId !== clipId) return e;
        const newOrder = effectIds.indexOf(e.id);
        return newOrder >= 0 ? { ...e, order: newOrder } : e;
      }),
    }));
  },

  toggleEffectEnabled: (appliedId) => {
    set((state) => ({
      applied: state.applied.map((e) => (e.id === appliedId ? { ...e, enabled: !e.enabled } : e)),
    }));
  },

  duplicateEffect: (appliedId) => {
    const effect = get().applied.find((e) => e.id === appliedId);
    if (!effect) return null;

    const id = generateId();
    const clipEffects = get().getEffectsForClip(effect.clipId);

    const newEffect: AppliedEffect = {
      ...effect,
      id,
      order: clipEffects.length,
    };

    set((state) => ({ applied: [...state.applied, newEffect] }));
    return id;
  },

  selectEffect: (appliedId) => set({ selectedEffectId: appliedId }),

  getEffectsForClip: (clipId) => {
    return get()
      .applied.filter((e) => e.clipId === clipId)
      .sort((a, b) => a.order - b.order);
  },

  removeEffectsForClip: (clipId) => {
    const effectsToRemove = get().applied.filter((e) => e.clipId === clipId);
    const effectIds = effectsToRemove.map((e) => e.id);
    set((state) => ({
      applied: state.applied.filter((e) => e.clipId !== clipId),
      selectedEffectId: effectIds.includes(state.selectedEffectId || '')
        ? null
        : state.selectedEffectId,
    }));
  },

  getEffectDefinition: (effectId) => {
    return get().definitions.find((d) => d.id === effectId);
  },

  getDefinitionsByCategory: (category) => {
    return get().definitions.filter((d) => d.category === category);
  },

  getSelectedEffect: () => {
    const { applied, selectedEffectId } = get();
    return applied.find((e) => e.id === selectedEffectId);
  },

  resetEffectParameters: (appliedId) => {
    const effect = get().applied.find((e) => e.id === appliedId);
    if (!effect) return;

    const definition = get().definitions.find((d) => d.id === effect.effectId);
    if (!definition) return;

    const defaultParams = Object.fromEntries(
      definition.parameters.map((param) => [param.id, param.defaultValue])
    );

    set((state) => ({
      applied: state.applied.map((e) =>
        e.id === appliedId ? { ...e, parameters: defaultParams } : e
      ),
    }));
  },

  clearAllEffects: () => set({ applied: [], selectedEffectId: null }),
}));
