/**
 * SSML Editor State Management
 * Manages SSML editing, per-scene controls, and provider selection
 */

import { create } from 'zustand';
import type {
  SSMLSegmentResult,
  SSMLPlanningResult,
  ProsodyAdjustments,
  ProviderSSMLConstraints,
} from '@/services/ssmlService';

export interface SceneSSMLState {
  sceneIndex: number;
  originalText: string;
  ssmlMarkup: string;
  userModified: boolean;
  estimatedDurationMs: number;
  targetDurationMs: number;
  deviationPercent: number;
  adjustments: ProsodyAdjustments;
}

interface SSMLEditorState {
  // Scene SSML data
  scenes: Map<number, SceneSSMLState>;
  selectedSceneIndex: number | null;

  // Provider and constraints
  selectedProvider: string | null;
  providerConstraints: ProviderSSMLConstraints | null;

  // Planning state
  isPlanning: boolean;
  planningResult: SSMLPlanningResult | null;

  // Validation state
  validationErrors: Map<number, string[]>;
  validationWarnings: Map<number, string[]>;

  // UI state
  showWaveform: boolean;
  showTimingMarkers: boolean;
  autoFitEnabled: boolean;

  // Actions
  setScenes: (segments: SSMLSegmentResult[]) => void;
  updateScene: (sceneIndex: number, updates: Partial<SceneSSMLState>) => void;
  selectScene: (sceneIndex: number | null) => void;

  setProvider: (provider: string) => void;
  setProviderConstraints: (constraints: ProviderSSMLConstraints) => void;

  setIsPlanning: (isPlanning: boolean) => void;
  setPlanningResult: (result: SSMLPlanningResult | null) => void;

  setValidationErrors: (sceneIndex: number, errors: string[]) => void;
  setValidationWarnings: (sceneIndex: number, warnings: string[]) => void;
  clearValidation: (sceneIndex: number) => void;

  toggleWaveform: () => void;
  toggleTimingMarkers: () => void;
  setAutoFitEnabled: (enabled: boolean) => void;

  reset: () => void;
}

export const useSSMLEditorStore = create<SSMLEditorState>((set, get) => ({
  // Initial state
  scenes: new Map(),
  selectedSceneIndex: null,
  selectedProvider: null,
  providerConstraints: null,
  isPlanning: false,
  planningResult: null,
  validationErrors: new Map(),
  validationWarnings: new Map(),
  showWaveform: true,
  showTimingMarkers: false,
  autoFitEnabled: true,

  // Actions
  setScenes: (segments) => {
    const scenes = new Map<number, SceneSSMLState>();
    segments.forEach((segment) => {
      scenes.set(segment.sceneIndex, {
        sceneIndex: segment.sceneIndex,
        originalText: segment.originalText,
        ssmlMarkup: segment.ssmlMarkup,
        userModified: false,
        estimatedDurationMs: segment.estimatedDurationMs,
        targetDurationMs: segment.targetDurationMs,
        deviationPercent: segment.deviationPercent,
        adjustments: segment.adjustments,
      });
    });
    set({ scenes });
  },

  updateScene: (sceneIndex, updates) => {
    const scenes = new Map(get().scenes);
    const scene = scenes.get(sceneIndex);
    if (scene) {
      scenes.set(sceneIndex, {
        ...scene,
        ...updates,
        userModified: true,
      });
      set({ scenes });
    }
  },

  selectScene: (sceneIndex) => {
    set({ selectedSceneIndex: sceneIndex });
  },

  setProvider: (provider) => {
    set({ selectedProvider: provider });
  },

  setProviderConstraints: (constraints) => {
    set({ providerConstraints: constraints });
  },

  setIsPlanning: (isPlanning) => {
    set({ isPlanning });
  },

  setPlanningResult: (result) => {
    set({ planningResult: result });
    if (result) {
      get().setScenes(result.segments);
    }
  },

  setValidationErrors: (sceneIndex, errors) => {
    const validationErrors = new Map(get().validationErrors);
    validationErrors.set(sceneIndex, errors);
    set({ validationErrors });
  },

  setValidationWarnings: (sceneIndex, warnings) => {
    const validationWarnings = new Map(get().validationWarnings);
    validationWarnings.set(sceneIndex, warnings);
    set({ validationWarnings });
  },

  clearValidation: (sceneIndex) => {
    const validationErrors = new Map(get().validationErrors);
    const validationWarnings = new Map(get().validationWarnings);
    validationErrors.delete(sceneIndex);
    validationWarnings.delete(sceneIndex);
    set({ validationErrors, validationWarnings });
  },

  toggleWaveform: () => {
    set({ showWaveform: !get().showWaveform });
  },

  toggleTimingMarkers: () => {
    set({ showTimingMarkers: !get().showTimingMarkers });
  },

  setAutoFitEnabled: (enabled) => {
    set({ autoFitEnabled: enabled });
  },

  reset: () => {
    set({
      scenes: new Map(),
      selectedSceneIndex: null,
      selectedProvider: null,
      providerConstraints: null,
      isPlanning: false,
      planningResult: null,
      validationErrors: new Map(),
      validationWarnings: new Map(),
      showWaveform: true,
      showTimingMarkers: false,
      autoFitEnabled: true,
    });
  },
}));
