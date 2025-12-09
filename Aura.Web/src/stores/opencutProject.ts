/**
 * OpenCut Project Store
 *
 * Manages OpenCut project state including project metadata, canvas settings,
 * and project-level operations. This store works alongside Aura's existing
 * timeline state for native OpenCut integration.
 */

import { create } from 'zustand';

export interface OpenCutProject {
  id: string;
  name: string;
  createdAt: Date;
  updatedAt: Date;
  fps: number;
  canvasWidth: number;
  canvasHeight: number;
  backgroundColor: string;
  backgroundType: 'solid' | 'transparent' | 'blur';
}

export interface OpenCutProjectState {
  activeProject: OpenCutProject | null;
  isLoading: boolean;
  error: string | null;

  // Actions
  createProject: (name: string) => void;
  updateProject: (updates: Partial<OpenCutProject>) => void;
  setActiveProject: (project: OpenCutProject | null) => void;
  updateCanvasSize: (width: number, height: number) => void;
  updateFps: (fps: number) => void;
  updateBackground: (color: string, type: 'solid' | 'transparent' | 'blur') => void;
  clearProject: () => void;
}

const DEFAULT_PROJECT: Omit<OpenCutProject, 'id' | 'createdAt' | 'updatedAt'> = {
  name: 'Untitled Project',
  fps: 30,
  canvasWidth: 1920,
  canvasHeight: 1080,
  backgroundColor: '#000000',
  backgroundType: 'solid',
};

function generateId(): string {
  return `opencut-${Date.now()}-${Math.random().toString(36).substring(2, 9)}`;
}

export const useOpenCutProjectStore = create<OpenCutProjectState>((set, get) => ({
  activeProject: null,
  isLoading: false,
  error: null,

  createProject: (name: string) => {
    const now = new Date();
    const project: OpenCutProject = {
      ...DEFAULT_PROJECT,
      id: generateId(),
      name,
      createdAt: now,
      updatedAt: now,
    };
    set({ activeProject: project, error: null });
  },

  updateProject: (updates: Partial<OpenCutProject>) => {
    const { activeProject } = get();
    if (!activeProject) return;

    set({
      activeProject: {
        ...activeProject,
        ...updates,
        updatedAt: new Date(),
      },
    });
  },

  setActiveProject: (project: OpenCutProject | null) => {
    set({ activeProject: project, error: null });
  },

  updateCanvasSize: (width: number, height: number) => {
    const { activeProject, updateProject } = get();
    if (!activeProject) return;

    updateProject({ canvasWidth: width, canvasHeight: height });
  },

  updateFps: (fps: number) => {
    const { updateProject } = get();
    updateProject({ fps: Math.max(1, Math.min(120, fps)) });
  },

  updateBackground: (color: string, type: 'solid' | 'transparent' | 'blur') => {
    const { updateProject } = get();
    updateProject({ backgroundColor: color, backgroundType: type });
  },

  clearProject: () => {
    set({ activeProject: null, error: null });
  },
}));
