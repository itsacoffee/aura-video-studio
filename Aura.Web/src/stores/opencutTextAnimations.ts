/**
 * OpenCut Text Animations Store
 *
 * Manages text animation presets for captions and text clips including
 * typewriter, bounce, fade, word-by-word highlight, and other popular
 * social media text effects.
 */

import { create } from 'zustand';

function generateId(): string {
  return `anim-${Date.now()}-${Math.random().toString(36).substring(2, 9)}`;
}

/** Available text animation types */
export type TextAnimationType =
  | 'none'
  | 'fade-in'
  | 'fade-out'
  | 'typewriter'
  | 'typewriter-cursor'
  | 'bounce-in'
  | 'bounce-letters'
  | 'slide-up'
  | 'slide-down'
  | 'slide-left'
  | 'slide-right'
  | 'scale-in'
  | 'scale-out'
  | 'word-by-word'
  | 'word-highlight'
  | 'karaoke'
  | 'glitch'
  | 'blur-in'
  | 'rotate-in'
  | 'wave'
  | 'rainbow';

/** Animation category for grouping presets */
export type TextAnimationCategory = 'entrance' | 'emphasis' | 'exit' | 'continuous';

/** Position where animation is applied */
export type TextAnimationPosition = 'in' | 'out' | 'continuous';

/** Text animation preset definition */
export interface TextAnimationPreset {
  /** Unique preset identifier */
  id: string;
  /** Display name */
  name: string;
  /** Animation type */
  type: TextAnimationType;
  /** Category for grouping */
  category: TextAnimationCategory;
  /** Default duration in seconds */
  duration: number;
  /** Easing function name */
  easing: string;
  /** Type-specific parameters */
  parameters: Record<string, string | number | boolean>;
  /** Optional thumbnail preview URL */
  thumbnail?: string;
}

/** Applied text animation instance */
export interface AppliedTextAnimation {
  /** Unique instance identifier */
  id: string;
  /** Target clip or caption ID */
  targetId: string;
  /** Target type */
  targetType: 'clip' | 'caption';
  /** Reference to preset ID */
  presetId: string;
  /** Position where animation is applied */
  position: TextAnimationPosition;
  /** Duration in seconds */
  duration: number;
  /** Delay before animation starts in seconds */
  delay: number;
  /** Parameter values (key is parameter id) */
  parameters: Record<string, string | number | boolean>;
}

/** Built-in text animation presets */
export const BUILTIN_TEXT_ANIMATION_PRESETS: TextAnimationPreset[] = [
  // Entrance animations
  {
    id: 'fade-in',
    name: 'Fade In',
    type: 'fade-in',
    category: 'entrance',
    duration: 0.5,
    easing: 'ease-out',
    parameters: {},
  },
  {
    id: 'typewriter',
    name: 'Typewriter',
    type: 'typewriter',
    category: 'entrance',
    duration: 2,
    easing: 'linear',
    parameters: { cursor: false },
  },
  {
    id: 'typewriter-cursor',
    name: 'Typewriter + Cursor',
    type: 'typewriter-cursor',
    category: 'entrance',
    duration: 2,
    easing: 'linear',
    parameters: { cursor: true, cursorChar: '|' },
  },
  {
    id: 'bounce-in',
    name: 'Bounce In',
    type: 'bounce-in',
    category: 'entrance',
    duration: 0.6,
    easing: 'ease-out-bounce',
    parameters: {},
  },
  {
    id: 'bounce-letters',
    name: 'Bounce Letters',
    type: 'bounce-letters',
    category: 'entrance',
    duration: 1,
    easing: 'ease-out-bounce',
    parameters: { stagger: 0.05 },
  },
  {
    id: 'slide-up',
    name: 'Slide Up',
    type: 'slide-up',
    category: 'entrance',
    duration: 0.5,
    easing: 'ease-out',
    parameters: { distance: 50 },
  },
  {
    id: 'slide-down',
    name: 'Slide Down',
    type: 'slide-down',
    category: 'entrance',
    duration: 0.5,
    easing: 'ease-out',
    parameters: { distance: 50 },
  },
  {
    id: 'slide-left',
    name: 'Slide Left',
    type: 'slide-left',
    category: 'entrance',
    duration: 0.5,
    easing: 'ease-out',
    parameters: { distance: 50 },
  },
  {
    id: 'slide-right',
    name: 'Slide Right',
    type: 'slide-right',
    category: 'entrance',
    duration: 0.5,
    easing: 'ease-out',
    parameters: { distance: 50 },
  },
  {
    id: 'scale-in',
    name: 'Scale In',
    type: 'scale-in',
    category: 'entrance',
    duration: 0.4,
    easing: 'ease-out-back',
    parameters: { startScale: 0 },
  },
  {
    id: 'blur-in',
    name: 'Blur In',
    type: 'blur-in',
    category: 'entrance',
    duration: 0.5,
    easing: 'ease-out',
    parameters: { startBlur: 10 },
  },
  {
    id: 'rotate-in',
    name: 'Rotate In',
    type: 'rotate-in',
    category: 'entrance',
    duration: 0.5,
    easing: 'ease-out',
    parameters: { startRotation: -90 },
  },
  {
    id: 'word-by-word',
    name: 'Word by Word',
    type: 'word-by-word',
    category: 'entrance',
    duration: 2,
    easing: 'ease-out',
    parameters: { stagger: 0.1 },
  },

  // Emphasis animations
  {
    id: 'glitch',
    name: 'Glitch',
    type: 'glitch',
    category: 'emphasis',
    duration: 0.3,
    easing: 'linear',
    parameters: { intensity: 5 },
  },

  // Exit animations
  {
    id: 'fade-out',
    name: 'Fade Out',
    type: 'fade-out',
    category: 'exit',
    duration: 0.5,
    easing: 'ease-in',
    parameters: {},
  },
  {
    id: 'scale-out',
    name: 'Scale Out',
    type: 'scale-out',
    category: 'exit',
    duration: 0.4,
    easing: 'ease-in-back',
    parameters: { endScale: 0 },
  },

  // Continuous animations
  {
    id: 'word-highlight',
    name: 'Word Highlight',
    type: 'word-highlight',
    category: 'continuous',
    duration: 3,
    easing: 'linear',
    parameters: { highlightColor: '#FFFF00' },
  },
  {
    id: 'karaoke',
    name: 'Karaoke',
    type: 'karaoke',
    category: 'continuous',
    duration: 5,
    easing: 'linear',
    parameters: { fillColor: '#00FF00' },
  },
  {
    id: 'wave',
    name: 'Wave',
    type: 'wave',
    category: 'continuous',
    duration: 2,
    easing: 'ease-in-out',
    parameters: { amplitude: 10, frequency: 2 },
  },
  {
    id: 'rainbow',
    name: 'Rainbow',
    type: 'rainbow',
    category: 'continuous',
    duration: 3,
    easing: 'linear',
    parameters: { speed: 1 },
  },
];

/** Text animations store state */
interface TextAnimationsState {
  /** All available animation presets */
  presets: TextAnimationPreset[];
  /** Applied animations on targets */
  applied: AppliedTextAnimation[];
  /** Currently selected animation ID */
  selectedAnimationId: string | null;
}

/** Text animations store actions */
interface TextAnimationsActions {
  /**
   * Apply an animation preset to a target.
   * Returns the applied animation ID.
   */
  applyAnimation: (
    targetId: string,
    targetType: 'clip' | 'caption',
    presetId: string,
    position: TextAnimationPosition
  ) => string;

  /**
   * Remove an applied animation by ID.
   */
  removeAnimation: (animationId: string) => void;

  /**
   * Update an applied animation's properties.
   */
  updateAnimation: (animationId: string, updates: Partial<AppliedTextAnimation>) => void;

  /**
   * Get all animations applied to a specific target.
   */
  getAnimationsForTarget: (targetId: string) => AppliedTextAnimation[];

  /**
   * Get animation at a specific position on a target.
   */
  getAnimationAtPosition: (
    targetId: string,
    position: TextAnimationPosition
  ) => AppliedTextAnimation | undefined;

  /**
   * Select an animation by its ID.
   */
  selectAnimation: (animationId: string | null) => void;

  /**
   * Get a preset definition by ID.
   */
  getPreset: (presetId: string) => TextAnimationPreset | undefined;

  /**
   * Get all presets in a specific category.
   */
  getPresetsByCategory: (category: TextAnimationCategory) => TextAnimationPreset[];

  /**
   * Get the currently selected applied animation.
   */
  getSelectedAnimation: () => AppliedTextAnimation | undefined;

  /**
   * Remove all animations for a specific target.
   */
  removeAnimationsForTarget: (targetId: string) => void;

  /**
   * Clear all applied animations.
   */
  clearAllAnimations: () => void;
}

export type TextAnimationsStore = TextAnimationsState & TextAnimationsActions;

export const useTextAnimationsStore = create<TextAnimationsStore>((set, get) => ({
  presets: BUILTIN_TEXT_ANIMATION_PRESETS,
  applied: [],
  selectedAnimationId: null,

  applyAnimation: (targetId, targetType, presetId, position) => {
    const preset = get().getPreset(presetId);
    if (!preset) return '';

    // Remove existing animation at this position
    const existing = get().getAnimationAtPosition(targetId, position);
    if (existing) {
      get().removeAnimation(existing.id);
    }

    const id = generateId();
    const animation: AppliedTextAnimation = {
      id,
      targetId,
      targetType,
      presetId,
      position,
      duration: preset.duration,
      delay: 0,
      parameters: { ...preset.parameters },
    };

    set((state) => ({ applied: [...state.applied, animation] }));
    return id;
  },

  removeAnimation: (animationId) => {
    set((state) => ({
      applied: state.applied.filter((a) => a.id !== animationId),
      selectedAnimationId:
        state.selectedAnimationId === animationId ? null : state.selectedAnimationId,
    }));
  },

  updateAnimation: (animationId, updates) => {
    set((state) => ({
      applied: state.applied.map((a) => (a.id === animationId ? { ...a, ...updates } : a)),
    }));
  },

  getAnimationsForTarget: (targetId) => {
    return get().applied.filter((a) => a.targetId === targetId);
  },

  getAnimationAtPosition: (targetId, position) => {
    return get().applied.find((a) => a.targetId === targetId && a.position === position);
  },

  selectAnimation: (animationId) => set({ selectedAnimationId: animationId }),

  getPreset: (presetId) => get().presets.find((p) => p.id === presetId),

  getPresetsByCategory: (category) => get().presets.filter((p) => p.category === category),

  getSelectedAnimation: () => {
    const { applied, selectedAnimationId } = get();
    return applied.find((a) => a.id === selectedAnimationId);
  },

  removeAnimationsForTarget: (targetId) => {
    const animationsToRemove = get().applied.filter((a) => a.targetId === targetId);
    const animationIds = animationsToRemove.map((a) => a.id);
    set((state) => ({
      applied: state.applied.filter((a) => a.targetId !== targetId),
      selectedAnimationId: animationIds.includes(state.selectedAnimationId || '')
        ? null
        : state.selectedAnimationId,
    }));
  },

  clearAllAnimations: () => set({ applied: [], selectedAnimationId: null }),
}));
