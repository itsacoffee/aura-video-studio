import { create } from 'zustand';

export interface ModelInfo {
  provider: string;
  modelId: string;
  maxTokens: number;
  contextWindow: number;
  aliases: string[];
  isDeprecated: boolean;
  deprecationDate?: string;
  replacementModel?: string;
}

export interface ModelSelection {
  provider: string;
  stage: string;
  modelId: string;
  scope: 'Global' | 'Project' | 'Stage' | 'Run';
  isPinned: boolean;
  setBy: string;
  setAt: string;
  reason: string;
}

export interface ModelSelectionState {
  globalDefaults: ModelSelection[];
  projectOverrides: ModelSelection[];
  stageSelections: ModelSelection[];
  allowAutomaticFallback: boolean;
}

export interface ModelTestResult {
  provider: string;
  modelId: string;
  isAvailable: boolean;
  isDeprecated: boolean;
  replacementModel?: string;
  contextWindow: number;
  maxTokens: number;
  errorMessage?: string;
  testedAt: string;
}

interface ModelSelectionStore {
  // Available models by provider
  availableModels: Record<string, ModelInfo[]>;
  isLoadingModels: boolean;
  modelsError: string | null;

  // Current selections
  selections: ModelSelectionState | null;
  isLoadingSelections: boolean;
  selectionsError: string | null;

  // Test results
  testResults: Record<string, ModelTestResult>;
  isTestingModel: boolean;

  // Deprecation warnings
  deprecationWarnings: Array<{
    provider: string;
    modelId: string;
    message: string;
    replacementModel?: string;
  }>;

  // Actions
  loadAvailableModels: (provider?: string) => Promise<void>;
  loadSelections: () => Promise<void>;
  setModelSelection: (
    provider: string,
    stage: string | undefined,
    modelId: string,
    scope: 'Global' | 'Project' | 'Stage' | 'Run',
    pin: boolean,
    reason?: string
  ) => Promise<{ success: boolean; deprecationWarning?: string; error?: string }>;
  clearSelections: (
    provider?: string,
    stage?: string,
    scope?: 'Global' | 'Project' | 'Stage' | 'Run'
  ) => Promise<void>;
  testModel: (provider: string, modelId: string, apiKey: string) => Promise<ModelTestResult>;
  setAllowAutomaticFallback: (allow: boolean) => Promise<void>;
  reset: () => void;
}

export const useModelSelectionStore = create<ModelSelectionStore>((set, get) => ({
  availableModels: {},
  isLoadingModels: false,
  modelsError: null,

  selections: null,
  isLoadingSelections: false,
  selectionsError: null,

  testResults: {},
  isTestingModel: false,

  deprecationWarnings: [],

  loadAvailableModels: async (provider?: string) => {
    set({ isLoadingModels: true, modelsError: null });

    try {
      const url = provider
        ? `/api/models/available?provider=${encodeURIComponent(provider)}`
        : '/api/models/available';

      const response = await fetch(url);
      if (!response.ok) {
        throw new Error(`Failed to load models: ${response.statusText}`);
      }

      const data = await response.json();
      set({ availableModels: data.providers, isLoadingModels: false });
    } catch (error: unknown) {
      const errorMessage =
        error instanceof Error ? error.message : 'Failed to load available models';
      set({ modelsError: errorMessage, isLoadingModels: false });
    }
  },

  loadSelections: async () => {
    set({ isLoadingSelections: true, selectionsError: null });

    try {
      const response = await fetch('/api/models/selection');
      if (!response.ok) {
        throw new Error(`Failed to load selections: ${response.statusText}`);
      }

      const data = await response.json();
      set({
        selections: {
          globalDefaults: data.globalDefaults || [],
          projectOverrides: data.projectOverrides || [],
          stageSelections: data.stageSelections || [],
          allowAutomaticFallback: data.allowAutomaticFallback || false,
        },
        isLoadingSelections: false,
      });
    } catch (error: unknown) {
      const errorMessage =
        error instanceof Error ? error.message : 'Failed to load model selections';
      set({ selectionsError: errorMessage, isLoadingSelections: false });
    }
  },

  setModelSelection: async (provider, stage, modelId, scope, pin, reason) => {
    try {
      const response = await fetch('/api/models/selection', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          provider,
          stage,
          modelId,
          scope,
          pin,
          setBy: 'user',
          reason: reason || 'User selection',
        }),
      });

      const data = await response.json();

      if (!response.ok || !data.applied) {
        return {
          success: false,
          error: data.reason || 'Failed to set model selection',
        };
      }

      // Reload selections to reflect change
      await get().loadSelections();

      return {
        success: true,
        deprecationWarning: data.deprecationWarning,
      };
    } catch (error: unknown) {
      const errorMessage = error instanceof Error ? error.message : 'Failed to set model selection';
      return { success: false, error: errorMessage };
    }
  },

  clearSelections: async (provider, stage, scope) => {
    try {
      const response = await fetch('/api/models/selection/clear', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ provider, stage, scope }),
      });

      if (!response.ok) {
        throw new Error('Failed to clear selections');
      }

      // Reload selections to reflect change
      await get().loadSelections();
    } catch (error: unknown) {
      const errorMessage = error instanceof Error ? error.message : 'Failed to clear selections';
      set({ selectionsError: errorMessage });
      throw error;
    }
  },

  testModel: async (provider, modelId, apiKey) => {
    set({ isTestingModel: true });

    try {
      const response = await fetch('/api/models/test', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ provider, modelId, apiKey }),
      });

      if (!response.ok) {
        throw new Error('Model test failed');
      }

      const result: ModelTestResult = await response.json();

      // Store test result
      set((state) => ({
        testResults: {
          ...state.testResults,
          [`${provider}:${modelId}`]: result,
        },
        isTestingModel: false,
      }));

      return result;
    } catch (error: unknown) {
      set({ isTestingModel: false });
      throw error;
    }
  },

  setAllowAutomaticFallback: async (allow) => {
    // This would be implemented via settings API endpoint
    set((state) => ({
      selections: state.selections
        ? { ...state.selections, allowAutomaticFallback: allow }
        : null,
    }));
  },

  reset: () => {
    set({
      availableModels: {},
      isLoadingModels: false,
      modelsError: null,
      selections: null,
      isLoadingSelections: false,
      selectionsError: null,
      testResults: {},
      isTestingModel: false,
      deprecationWarnings: [],
    });
  },
}));
