import { create } from 'zustand';
import type {
  GuidedModeConfigDto,
  PromptDiffDto,
  LockedSectionDto,
  ExplainArtifactResponse,
} from '../types/api-v1';

/**
 * Guided mode state management
 * Handles beginner-first experience with tooltips, explanations, and prompt diffs
 */

export type ExperienceLevel = 'beginner' | 'intermediate' | 'advanced';

export interface TooltipState {
  id: string;
  visible: boolean;
  content: string;
  targetElement: string;
}

export interface ExplanationState {
  artifactType: string;
  content: string;
  visible: boolean;
  response?: ExplainArtifactResponse;
  loading: boolean;
}

export interface PromptDiffState {
  visible: boolean;
  promptDiff?: PromptDiffDto;
  onConfirm?: () => void;
  onCancel?: () => void;
}

export interface ImprovementMenuState {
  visible: boolean;
  artifactType: string;
  position: { x: number; y: number };
  availableActions: string[];
}

export interface GuidedModeState {
  config: GuidedModeConfigDto;
  activeTooltips: Map<string, TooltipState>;
  explanationPanel: ExplanationState | null;
  promptDiffModal: PromptDiffState;
  improvementMenu: ImprovementMenuState | null;
  lockedSections: Map<string, LockedSectionDto[]>;
  showOnboardingWizard: boolean;
  completedSteps: Set<string>;

  setConfig: (config: Partial<GuidedModeConfigDto>) => void;
  setExperienceLevel: (level: ExperienceLevel) => void;

  showTooltip: (id: string, content: string, targetElement: string) => void;
  hideTooltip: (id: string) => void;
  hideAllTooltips: () => void;

  showExplanation: (artifactType: string, content: string) => void;
  hideExplanation: () => void;
  setExplanationResponse: (response: ExplainArtifactResponse) => void;
  setExplanationLoading: (loading: boolean) => void;

  showPromptDiff: (promptDiff: PromptDiffDto, onConfirm: () => void, onCancel: () => void) => void;
  hidePromptDiff: () => void;
  confirmPromptDiff: () => void;
  cancelPromptDiff: () => void;

  showImprovementMenu: (
    artifactType: string,
    position: { x: number; y: number },
    availableActions: string[]
  ) => void;
  hideImprovementMenu: () => void;

  lockSection: (artifactId: string, section: LockedSectionDto) => void;
  unlockSection: (artifactId: string, sectionIndex: number) => void;
  getLockedSections: (artifactId: string) => LockedSectionDto[];
  clearLockedSections: (artifactId: string) => void;

  markStepCompleted: (stepId: string) => void;
  isStepCompleted: (stepId: string) => boolean;

  setShowOnboardingWizard: (show: boolean) => void;

  reset: () => void;
}

const defaultConfig: GuidedModeConfigDto = {
  enabled: true,
  experienceLevel: 'beginner',
  showTooltips: true,
  showWhyLinks: true,
  requirePromptDiffConfirmation: true,
};

export const useGuidedMode = create<GuidedModeState>((set, get) => ({
  config: defaultConfig,
  activeTooltips: new Map(),
  explanationPanel: null,
  promptDiffModal: {
    visible: false,
    promptDiff: undefined,
    onConfirm: undefined,
    onCancel: undefined,
  },
  improvementMenu: null,
  lockedSections: new Map(),
  showOnboardingWizard: false,
  completedSteps: new Set(),

  setConfig: (config) =>
    set((state) => ({
      config: { ...state.config, ...config },
    })),

  setExperienceLevel: (level) =>
    set((state) => ({
      config: {
        ...state.config,
        experienceLevel: level,
        showTooltips: level === 'beginner',
        showWhyLinks: level !== 'advanced',
        requirePromptDiffConfirmation: level !== 'advanced',
      },
    })),

  showTooltip: (id, content, targetElement) =>
    set((state) => {
      if (!state.config.showTooltips) return state;

      const newTooltips = new Map(state.activeTooltips);
      newTooltips.set(id, { id, visible: true, content, targetElement });
      return { activeTooltips: newTooltips };
    }),

  hideTooltip: (id) =>
    set((state) => {
      const newTooltips = new Map(state.activeTooltips);
      newTooltips.delete(id);
      return { activeTooltips: newTooltips };
    }),

  hideAllTooltips: () =>
    set(() => ({
      activeTooltips: new Map(),
    })),

  showExplanation: (artifactType, content) =>
    set(() => ({
      explanationPanel: {
        artifactType,
        content,
        visible: true,
        loading: false,
      },
    })),

  hideExplanation: () =>
    set(() => ({
      explanationPanel: null,
    })),

  setExplanationResponse: (response) =>
    set((state) => ({
      explanationPanel: state.explanationPanel
        ? { ...state.explanationPanel, response, loading: false }
        : null,
    })),

  setExplanationLoading: (loading) =>
    set((state) => ({
      explanationPanel: state.explanationPanel ? { ...state.explanationPanel, loading } : null,
    })),

  showPromptDiff: (promptDiff, onConfirm, onCancel) =>
    set(() => ({
      promptDiffModal: {
        visible: true,
        promptDiff,
        onConfirm,
        onCancel,
      },
    })),

  hidePromptDiff: () =>
    set(() => ({
      promptDiffModal: {
        visible: false,
        promptDiff: undefined,
        onConfirm: undefined,
        onCancel: undefined,
      },
    })),

  confirmPromptDiff: () => {
    const { promptDiffModal } = get();
    if (promptDiffModal.onConfirm) {
      promptDiffModal.onConfirm();
    }
    get().hidePromptDiff();
  },

  cancelPromptDiff: () => {
    const { promptDiffModal } = get();
    if (promptDiffModal.onCancel) {
      promptDiffModal.onCancel();
    }
    get().hidePromptDiff();
  },

  showImprovementMenu: (artifactType, position, availableActions) =>
    set(() => ({
      improvementMenu: {
        visible: true,
        artifactType,
        position,
        availableActions,
      },
    })),

  hideImprovementMenu: () =>
    set(() => ({
      improvementMenu: null,
    })),

  lockSection: (artifactId, section) =>
    set((state) => {
      const newLockedSections = new Map(state.lockedSections);
      const currentSections = newLockedSections.get(artifactId) || [];
      newLockedSections.set(artifactId, [...currentSections, section]);
      return { lockedSections: newLockedSections };
    }),

  unlockSection: (artifactId, sectionIndex) =>
    set((state) => {
      const newLockedSections = new Map(state.lockedSections);
      const currentSections = newLockedSections.get(artifactId) || [];
      const updatedSections = currentSections.filter((_, idx) => idx !== sectionIndex);

      if (updatedSections.length === 0) {
        newLockedSections.delete(artifactId);
      } else {
        newLockedSections.set(artifactId, updatedSections);
      }

      return { lockedSections: newLockedSections };
    }),

  getLockedSections: (artifactId) => {
    return get().lockedSections.get(artifactId) || [];
  },

  clearLockedSections: (artifactId) =>
    set((state) => {
      const newLockedSections = new Map(state.lockedSections);
      newLockedSections.delete(artifactId);
      return { lockedSections: newLockedSections };
    }),

  markStepCompleted: (stepId) =>
    set((state) => ({
      completedSteps: new Set(state.completedSteps).add(stepId),
    })),

  isStepCompleted: (stepId) => {
    return get().completedSteps.has(stepId);
  },

  setShowOnboardingWizard: (show) =>
    set(() => ({
      showOnboardingWizard: show,
    })),

  reset: () =>
    set(() => ({
      config: defaultConfig,
      activeTooltips: new Map(),
      explanationPanel: null,
      promptDiffModal: {
        visible: false,
        promptDiff: undefined,
        onConfirm: undefined,
        onCancel: undefined,
      },
      improvementMenu: null,
      lockedSections: new Map(),
      showOnboardingWizard: false,
      completedSteps: new Set(),
    })),
}));
