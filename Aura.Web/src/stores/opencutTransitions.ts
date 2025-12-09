/**
 * OpenCut Transitions Store
 *
 * Manages video transitions including a library of built-in transitions,
 * applied transitions on clips, and transition parameters.
 */

import { create } from 'zustand';
import type {
  TransitionCategory,
  TransitionDefinition,
  TransitionParameter,
} from '../types/opencut';

function generateId(): string {
  return `trans-${Date.now()}-${Math.random().toString(36).substring(2, 9)}`;
}

/**
 * Extended transition category including blur.
 */
export type ExtendedTransitionCategory = TransitionCategory | 'blur';

/**
 * Position where transition is applied on a clip.
 */
export type TransitionPosition = 'start' | 'end';

/**
 * Extended transition definition with blur category support.
 */
export interface ExtendedTransitionDefinition extends Omit<TransitionDefinition, 'category'> {
  category: ExtendedTransitionCategory;
}

/**
 * Applied transition on a clip.
 */
export interface ClipTransition {
  /** Unique instance identifier */
  id: string;
  /** Reference to transition definition ID */
  transitionId: string;
  /** Clip ID this transition is applied to */
  clipId: string;
  /** Position on the clip (start or end) */
  position: TransitionPosition;
  /** Duration in seconds */
  duration: number;
  /** Parameter values (key is parameter id, value is the parameter value) */
  parameters: Record<string, string | number | boolean>;
}

// Built-in transition definitions
export const BUILTIN_TRANSITIONS: ExtendedTransitionDefinition[] = [
  {
    id: 'cross-dissolve',
    name: 'Cross Dissolve',
    description: 'Smoothly blend between two clips',
    category: 'dissolve',
    defaultDuration: 0.5,
    parameters: [],
  },
  {
    id: 'fade-to-black',
    name: 'Fade to Black',
    description: 'Fade out to black, then fade in',
    category: 'dissolve',
    defaultDuration: 0.5,
    parameters: [
      {
        id: 'color',
        name: 'Color',
        type: 'color',
        defaultValue: '#000000',
      },
    ],
  },
  {
    id: 'fade-to-white',
    name: 'Fade to White',
    description: 'Fade out to white, then fade in',
    category: 'dissolve',
    defaultDuration: 0.5,
    parameters: [
      {
        id: 'color',
        name: 'Color',
        type: 'color',
        defaultValue: '#FFFFFF',
      },
    ],
  },
  {
    id: 'wipe-left',
    name: 'Wipe Left',
    description: 'Wipe from right to left',
    category: 'wipe',
    defaultDuration: 0.5,
    parameters: [
      {
        id: 'softness',
        name: 'Softness',
        type: 'number',
        defaultValue: 0,
        min: 0,
        max: 100,
        unit: '%',
      },
    ],
  },
  {
    id: 'wipe-right',
    name: 'Wipe Right',
    description: 'Wipe from left to right',
    category: 'wipe',
    defaultDuration: 0.5,
    parameters: [
      {
        id: 'softness',
        name: 'Softness',
        type: 'number',
        defaultValue: 0,
        min: 0,
        max: 100,
        unit: '%',
      },
    ],
  },
  {
    id: 'wipe-up',
    name: 'Wipe Up',
    description: 'Wipe from bottom to top',
    category: 'wipe',
    defaultDuration: 0.5,
    parameters: [
      {
        id: 'softness',
        name: 'Softness',
        type: 'number',
        defaultValue: 0,
        min: 0,
        max: 100,
        unit: '%',
      },
    ],
  },
  {
    id: 'wipe-down',
    name: 'Wipe Down',
    description: 'Wipe from top to bottom',
    category: 'wipe',
    defaultDuration: 0.5,
    parameters: [
      {
        id: 'softness',
        name: 'Softness',
        type: 'number',
        defaultValue: 0,
        min: 0,
        max: 100,
        unit: '%',
      },
    ],
  },
  {
    id: 'slide-left',
    name: 'Slide Left',
    description: 'Slide the new clip in from the right',
    category: 'slide',
    defaultDuration: 0.5,
    parameters: [],
  },
  {
    id: 'slide-right',
    name: 'Slide Right',
    description: 'Slide the new clip in from the left',
    category: 'slide',
    defaultDuration: 0.5,
    parameters: [],
  },
  {
    id: 'zoom-in',
    name: 'Zoom In',
    description: 'Zoom into the center of the clip',
    category: 'zoom',
    defaultDuration: 0.5,
    parameters: [
      {
        id: 'scale',
        name: 'Scale',
        type: 'number',
        defaultValue: 150,
        min: 100,
        max: 300,
        unit: '%',
      },
    ],
  },
  {
    id: 'zoom-out',
    name: 'Zoom Out',
    description: 'Zoom out from the center of the clip',
    category: 'zoom',
    defaultDuration: 0.5,
    parameters: [
      {
        id: 'scale',
        name: 'Scale',
        type: 'number',
        defaultValue: 50,
        min: 10,
        max: 100,
        unit: '%',
      },
    ],
  },
  {
    id: 'blur-transition',
    name: 'Blur',
    description: 'Blur transition between clips',
    category: 'blur',
    defaultDuration: 0.5,
    parameters: [
      {
        id: 'amount',
        name: 'Amount',
        type: 'number',
        defaultValue: 20,
        min: 0,
        max: 100,
        unit: 'px',
      },
    ],
  },
];

interface OpenCutTransitionsState {
  /** All available transition definitions */
  definitions: ExtendedTransitionDefinition[];
  /** Applied transitions on clips */
  applied: ClipTransition[];
  /** Currently selected transition ID */
  selectedTransitionId: string | null;
  /** Default duration for new transitions */
  defaultDuration: number;
}

interface OpenCutTransitionsActions {
  // Apply transitions
  /**
   * Apply a transition to a clip at the specified position.
   * Returns the applied transition ID.
   */
  applyTransition: (transitionId: string, clipId: string, position: TransitionPosition) => string;

  /**
   * Remove an applied transition by ID.
   */
  removeTransition: (appliedId: string) => void;

  /**
   * Update an applied transition's properties.
   */
  updateTransition: (appliedId: string, updates: Partial<ClipTransition>) => void;

  // Selection
  /**
   * Select a transition by its applied ID.
   */
  selectTransition: (appliedId: string | null) => void;

  // Getters
  /**
   * Get a transition definition by ID.
   */
  getTransitionDefinition: (transitionId: string) => ExtendedTransitionDefinition | undefined;

  /**
   * Get all transitions applied to a specific clip.
   */
  getTransitionsForClip: (clipId: string) => ClipTransition[];

  /**
   * Get the transition at a specific position on a clip.
   */
  getTransitionAtPosition: (
    clipId: string,
    position: TransitionPosition
  ) => ClipTransition | undefined;

  /**
   * Get all transition definitions in a specific category.
   */
  getDefinitionsByCategory: (
    category: ExtendedTransitionCategory
  ) => ExtendedTransitionDefinition[];

  /**
   * Get the currently selected applied transition.
   */
  getSelectedTransition: () => ClipTransition | undefined;

  // Settings
  /**
   * Set the default duration for new transitions.
   */
  setDefaultDuration: (duration: number) => void;

  // Bulk operations
  /**
   * Remove all transitions for a specific clip.
   */
  removeTransitionsForClip: (clipId: string) => void;

  /**
   * Clear all applied transitions.
   */
  clearAllTransitions: () => void;
}

export type OpenCutTransitionsStore = OpenCutTransitionsState & OpenCutTransitionsActions;

export const useOpenCutTransitionsStore = create<OpenCutTransitionsStore>((set, get) => ({
  definitions: BUILTIN_TRANSITIONS,
  applied: [],
  selectedTransitionId: null,
  defaultDuration: 0.5,

  applyTransition: (transitionId, clipId, position) => {
    const definition = get().getTransitionDefinition(transitionId);
    if (!definition) return '';

    // Remove existing transition at this position
    const existing = get().getTransitionAtPosition(clipId, position);
    if (existing) {
      get().removeTransition(existing.id);
    }

    const id = generateId();
    const applied: ClipTransition = {
      id,
      transitionId,
      clipId,
      position,
      duration: get().defaultDuration,
      parameters: Object.fromEntries(
        definition.parameters.map((param: TransitionParameter) => [param.id, param.defaultValue])
      ),
    };

    set((state) => ({ applied: [...state.applied, applied] }));
    return id;
  },

  removeTransition: (appliedId) => {
    set((state) => ({
      applied: state.applied.filter((t) => t.id !== appliedId),
      selectedTransitionId:
        state.selectedTransitionId === appliedId ? null : state.selectedTransitionId,
    }));
  },

  updateTransition: (appliedId, updates) => {
    set((state) => ({
      applied: state.applied.map((t) => (t.id === appliedId ? { ...t, ...updates } : t)),
    }));
  },

  selectTransition: (appliedId) => set({ selectedTransitionId: appliedId }),

  getTransitionDefinition: (transitionId) => {
    return get().definitions.find((d) => d.id === transitionId);
  },

  getTransitionsForClip: (clipId) => {
    return get().applied.filter((t) => t.clipId === clipId);
  },

  getTransitionAtPosition: (clipId, position) => {
    return get().applied.find((t) => t.clipId === clipId && t.position === position);
  },

  getDefinitionsByCategory: (category) => {
    return get().definitions.filter((d) => d.category === category);
  },

  getSelectedTransition: () => {
    const { applied, selectedTransitionId } = get();
    return applied.find((t) => t.id === selectedTransitionId);
  },

  setDefaultDuration: (duration) => set({ defaultDuration: Math.max(0.1, Math.min(5, duration)) }),

  removeTransitionsForClip: (clipId) => {
    const transitionsToRemove = get().applied.filter((t) => t.clipId === clipId);
    const transitionIds = transitionsToRemove.map((t) => t.id);
    set((state) => ({
      applied: state.applied.filter((t) => t.clipId !== clipId),
      selectedTransitionId: transitionIds.includes(state.selectedTransitionId || '')
        ? null
        : state.selectedTransitionId,
    }));
  },

  clearAllTransitions: () => set({ applied: [], selectedTransitionId: null }),
}));
