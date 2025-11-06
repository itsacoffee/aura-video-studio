import { create } from 'zustand';
import type { SceneVisualSelection } from '@/services/visualSelectionService';

interface VisualSelectionState {
  selections: Map<string, SceneVisualSelection>;
  projectThresholds: Map<string, ThresholdConfig>;

  getSelection: (jobId: string, sceneIndex: number) => SceneVisualSelection | undefined;
  setSelection: (selection: SceneVisualSelection) => void;
  updateSelection: (
    jobId: string,
    sceneIndex: number,
    updates: Partial<SceneVisualSelection>
  ) => void;
  removeSelection: (jobId: string, sceneIndex: number) => void;
  clearJobSelections: (jobId: string) => void;

  getThresholds: (projectId: string) => ThresholdConfig;
  setThresholds: (projectId: string, thresholds: ThresholdConfig) => void;
}

export interface ThresholdConfig {
  projectId: string;
  minimumAestheticThreshold: number;
  autoAcceptThreshold: number;
  enableAutoSelection: boolean;
}

const defaultThresholds: Omit<ThresholdConfig, 'projectId'> = {
  minimumAestheticThreshold: 60.0,
  autoAcceptThreshold: 85.0,
  enableAutoSelection: false,
};

export const useVisualSelectionStore = create<VisualSelectionState>((set, get) => ({
  selections: new Map(),
  projectThresholds: new Map(),

  getSelection: (jobId: string, sceneIndex: number) => {
    const key = `${jobId}-${sceneIndex}`;
    return get().selections.get(key);
  },

  setSelection: (selection: SceneVisualSelection) => {
    set((state) => {
      const newSelections = new Map(state.selections);
      const key = `${selection.jobId}-${selection.sceneIndex}`;
      newSelections.set(key, selection);
      return { selections: newSelections };
    });
  },

  updateSelection: (jobId: string, sceneIndex: number, updates: Partial<SceneVisualSelection>) => {
    set((state) => {
      const key = `${jobId}-${sceneIndex}`;
      const existing = state.selections.get(key);
      if (!existing) return state;

      const newSelections = new Map(state.selections);
      newSelections.set(key, { ...existing, ...updates });
      return { selections: newSelections };
    });
  },

  removeSelection: (jobId: string, sceneIndex: number) => {
    set((state) => {
      const newSelections = new Map(state.selections);
      const key = `${jobId}-${sceneIndex}`;
      newSelections.delete(key);
      return { selections: newSelections };
    });
  },

  clearJobSelections: (jobId: string) => {
    set((state) => {
      const newSelections = new Map(state.selections);
      for (const key of newSelections.keys()) {
        if (key.startsWith(`${jobId}-`)) {
          newSelections.delete(key);
        }
      }
      return { selections: newSelections };
    });
  },

  getThresholds: (projectId: string) => {
    const thresholds = get().projectThresholds.get(projectId);
    return thresholds || { projectId, ...defaultThresholds };
  },

  setThresholds: (projectId: string, thresholds: ThresholdConfig) => {
    set((state) => {
      const newThresholds = new Map(state.projectThresholds);
      newThresholds.set(projectId, thresholds);
      return { projectThresholds: newThresholds };
    });
  },
}));
