import { create } from 'zustand';
import type {
  PromptModifiers,
  PromptPreview,
  FewShotExample,
  PromptVersion,
  PromptPreset,
} from '../types';

interface PromptCustomizationState {
  // Current prompt modifiers
  promptModifiers: PromptModifiers | null;

  // Available resources
  fewShotExamples: FewShotExample[];
  promptVersions: PromptVersion[];
  videoTypes: string[];

  // Current preview
  currentPreview: PromptPreview | null;

  // Saved presets
  savedPresets: PromptPreset[];

  // UI state
  isCustomizationPanelOpen: boolean;
  isLoadingPreview: boolean;
  isLoadingExamples: boolean;

  // Chain of thought state
  chainOfThoughtEnabled: boolean;
  currentStage: 'TopicAnalysis' | 'Outline' | 'FullScript' | null;
  stageResults: Record<string, string>;

  // Actions
  setPromptModifiers: (modifiers: PromptModifiers | null) => void;
  updatePromptModifier: (key: keyof PromptModifiers, value: unknown) => void;
  setFewShotExamples: (examples: FewShotExample[], videoTypes: string[]) => void;
  setPromptVersions: (versions: PromptVersion[]) => void;
  setCurrentPreview: (preview: PromptPreview | null) => void;
  setCustomizationPanelOpen: (open: boolean) => void;
  setLoadingPreview: (loading: boolean) => void;
  setLoadingExamples: (loading: boolean) => void;
  setChainOfThoughtEnabled: (enabled: boolean) => void;
  setCurrentStage: (stage: 'TopicAnalysis' | 'Outline' | 'FullScript' | null) => void;
  setStageResult: (stage: string, content: string) => void;
  savePreset: (preset: PromptPreset) => void;
  loadPreset: (presetName: string) => void;
  deletePreset: (presetName: string) => void;
  resetPromptModifiers: () => void;
  clearPreview: () => void;
}

export const usePromptCustomizationStore = create<PromptCustomizationState>((set, get) => ({
  // Initial state
  promptModifiers: null,
  fewShotExamples: [],
  promptVersions: [],
  videoTypes: [],
  currentPreview: null,
  savedPresets: loadPresetsFromStorage(),
  isCustomizationPanelOpen: false,
  isLoadingPreview: false,
  isLoadingExamples: false,
  chainOfThoughtEnabled: false,
  currentStage: null,
  stageResults: {},

  // Actions
  setPromptModifiers: (modifiers) => set({ promptModifiers: modifiers }),

  updatePromptModifier: (key, value) =>
    set((state) => ({
      promptModifiers: {
        ...(state.promptModifiers || {
          enableChainOfThought: false,
        }),
        [key]: value,
      } as PromptModifiers,
    })),

  setFewShotExamples: (examples, videoTypes) => set({ fewShotExamples: examples, videoTypes }),

  setPromptVersions: (versions) => set({ promptVersions: versions }),

  setCurrentPreview: (preview) => set({ currentPreview: preview }),

  setCustomizationPanelOpen: (open) => set({ isCustomizationPanelOpen: open }),

  setLoadingPreview: (loading) => set({ isLoadingPreview: loading }),

  setLoadingExamples: (loading) => set({ isLoadingExamples: loading }),

  setChainOfThoughtEnabled: (enabled) =>
    set((state) => ({
      chainOfThoughtEnabled: enabled,
      promptModifiers: {
        ...(state.promptModifiers || { enableChainOfThought: false }),
        enableChainOfThought: enabled,
      },
      currentStage: enabled ? 'TopicAnalysis' : null,
      stageResults: enabled ? {} : state.stageResults,
    })),

  setCurrentStage: (stage) => set({ currentStage: stage }),

  setStageResult: (stage, content) =>
    set((state) => ({
      stageResults: {
        ...state.stageResults,
        [stage]: content,
      },
    })),

  savePreset: (preset) => {
    const newPresets = [...get().savedPresets, preset];
    savePresetsToStorage(newPresets);
    set({ savedPresets: newPresets });
  },

  loadPreset: (presetName) => {
    const preset = get().savedPresets.find((p) => p.name === presetName);
    if (preset) {
      const modifiers: PromptModifiers = {
        additionalInstructions: preset.additionalInstructions,
        exampleStyle: preset.exampleStyle,
        enableChainOfThought: preset.enableChainOfThought,
        promptVersion: preset.promptVersion,
      };
      set({
        promptModifiers: modifiers,
        chainOfThoughtEnabled: preset.enableChainOfThought,
      });

      const updatedPresets = get().savedPresets.map((p) =>
        p.name === presetName ? { ...p, lastUsedAt: new Date().toISOString() } : p
      );
      savePresetsToStorage(updatedPresets);
      set({ savedPresets: updatedPresets });
    }
  },

  deletePreset: (presetName) => {
    const newPresets = get().savedPresets.filter((p) => p.name !== presetName);
    savePresetsToStorage(newPresets);
    set({ savedPresets: newPresets });
  },

  resetPromptModifiers: () =>
    set({
      promptModifiers: null,
      currentPreview: null,
      chainOfThoughtEnabled: false,
      currentStage: null,
      stageResults: {},
    }),

  clearPreview: () => set({ currentPreview: null }),
}));

// Helper functions for localStorage
function loadPresetsFromStorage(): PromptPreset[] {
  try {
    const stored = localStorage.getItem('prompt-presets');
    return stored ? JSON.parse(stored) : [];
  } catch (error) {
    console.error('Failed to load prompt presets from storage:', error);
    return [];
  }
}

function savePresetsToStorage(presets: PromptPreset[]): void {
  try {
    localStorage.setItem('prompt-presets', JSON.stringify(presets));
  } catch (error) {
    console.error('Failed to save prompt presets to storage:', error);
  }
}
