import { create } from 'zustand';

export interface CustomAudienceProfile {
  id: string;
  name: string;
  baseProfileId?: string;
  createdAt: Date;
  updatedAt: Date;
  isCustom: boolean;
  minAge: number;
  maxAge: number;
  educationLevel: string;
  educationLevelDescription?: string;
  culturalSensitivities: string[];
  topicsToAvoid: string[];
  topicsToEmphasize: string[];
  vocabularyLevel: number;
  sentenceStructurePreference: string;
  readingLevel: number;
  violenceThreshold: number;
  profanityThreshold: number;
  sexualContentThreshold: number;
  controversialTopicsThreshold: number;
  humorStyle: string;
  sarcasmLevel: number;
  jokeTypes: string[];
  culturalHumorPreferences: string[];
  formalityLevel: number;
  attentionSpanSeconds: number;
  pacingPreference: string;
  informationDensity: number;
  technicalDepthTolerance: number;
  jargonAcceptability: number;
  familiarTechnicalTerms: string[];
  emotionalTone: string;
  emotionalIntensity: number;
  ctaAggressiveness: number;
  ctaStyle: string;
  brandVoiceGuidelines?: string;
  brandToneKeywords: string[];
  brandPersonality?: string;
  description?: string;
  tags: string[];
  isFavorite: boolean;
  usageCount: number;
  lastUsedAt?: Date;
}

export interface ContentFilteringPolicy {
  id: string;
  name: string;
  createdAt: Date;
  updatedAt: Date;
  filteringEnabled: boolean;
  allowOverrideAll: boolean;
  profanityFilter: string;
  customBannedWords: string[];
  customAllowedWords: string[];
  violenceThreshold: number;
  blockGraphicContent: boolean;
  sexualContentThreshold: number;
  blockExplicitContent: boolean;
  bannedTopics: string[];
  allowedControversialTopics: string[];
  politicalContent: string;
  politicalContentGuidelines?: string;
  religiousContent: string;
  religiousContentGuidelines?: string;
  substanceReferences: string;
  blockHateSpeech: boolean;
  hateSpeechExceptions: string[];
  copyrightPolicy: string;
  blockedConcepts: string[];
  allowedConcepts: string[];
  blockedPeople: string[];
  allowedPeople: string[];
  blockedBrands: string[];
  allowedBrands: string[];
  description?: string;
  isDefault: boolean;
  usageCount: number;
  lastUsedAt?: Date;
}

export interface AIBehaviorSettings {
  id: string;
  name: string;
  createdAt: Date;
  updatedAt: Date;
  scriptGeneration: LLMStageParameters;
  sceneDescription: LLMStageParameters;
  contentOptimization: LLMStageParameters;
  translation: LLMStageParameters;
  qualityAnalysis: LLMStageParameters;
  creativityVsAdherence: number;
  enableChainOfThought: boolean;
  showPromptsBeforeSending: boolean;
  description?: string;
  isDefault: boolean;
  usageCount: number;
  lastUsedAt?: Date;
}

export interface LLMStageParameters {
  stageName: string;
  temperature: number;
  topP: number;
  frequencyPenalty: number;
  presencePenalty: number;
  maxTokens: number;
  customSystemPrompt?: string;
  preferredModel?: string;
  strictnessLevel: number;
}

export interface CustomPromptTemplate {
  id: string;
  name: string;
  stage: string;
  templateText: string;
  variables: string[];
  createdAt: Date;
  updatedAt: Date;
  variantGroup?: string;
  successCount: number;
  totalUses: number;
  successRate: number;
  description?: string;
  tags: string[];
  isFavorite: boolean;
}

export interface CustomQualityThresholds {
  id: string;
  name: string;
  createdAt: Date;
  updatedAt: Date;
  skipValidation: boolean;
  minScriptWordCount: number;
  maxScriptWordCount: number;
  acceptableGrammarErrors: number;
  requiredKeywords: string[];
  excludedKeywords: string[];
  minImageResolutionWidth: number;
  minImageResolutionHeight: number;
  minImageClarityScore: number;
  allowLowQualityImages: boolean;
  minAudioBitrate: number;
  minAudioClarity: number;
  maxBackgroundNoise: number;
  requireStereo: boolean;
  minSubtitleAccuracy: number;
  requireSubtitles: boolean;
  customMetricThresholds: Record<string, number>;
  description?: string;
  isDefault: boolean;
  usageCount: number;
  lastUsedAt?: Date;
}

export interface CustomVisualStyle {
  id: string;
  name: string;
  createdAt: Date;
  updatedAt: Date;
  colorPalette: string[];
  primaryColor?: string;
  secondaryColor?: string;
  accentColor?: string;
  visualComplexity: number;
  artisticStyle: string;
  compositionPreference: string;
  lightingPreference: string;
  preferredCameraAngles: string[];
  transitionStyle: string;
  transitionDurationMs: number;
  referenceImagePaths: string[];
  description?: string;
  tags: string[];
  isFavorite: boolean;
  usageCount: number;
}

interface UserPreferencesState {
  // Custom Audience Profiles
  customAudienceProfiles: CustomAudienceProfile[];
  selectedAudienceProfileId?: string;

  // Content Filtering Policies
  contentFilteringPolicies: ContentFilteringPolicy[];
  selectedFilteringPolicyId?: string;

  // AI Behavior Settings
  aiBehaviorSettings: AIBehaviorSettings[];
  selectedAIBehaviorId?: string;

  // Custom Prompt Templates
  customPromptTemplates: CustomPromptTemplate[];

  // Custom Quality Thresholds
  customQualityThresholds: CustomQualityThresholds[];
  selectedQualityThresholdsId?: string;

  // Custom Visual Styles
  customVisualStyles: CustomVisualStyle[];
  selectedVisualStyleId?: string;

  // UI State
  advancedMode: boolean;
  isLoading: boolean;
  error?: string;

  // Actions
  setAdvancedMode: (enabled: boolean) => void;

  // Custom Audience Profile Actions
  loadCustomAudienceProfiles: () => Promise<void>;
  selectAudienceProfile: (id?: string) => void;
  createCustomAudienceProfile: (
    profile: Omit<CustomAudienceProfile, 'id' | 'createdAt' | 'updatedAt'>
  ) => Promise<CustomAudienceProfile>;
  updateCustomAudienceProfile: (
    id: string,
    profile: Partial<CustomAudienceProfile>
  ) => Promise<void>;
  deleteCustomAudienceProfile: (id: string) => Promise<void>;

  // Content Filtering Policy Actions
  loadContentFilteringPolicies: () => Promise<void>;
  selectFilteringPolicy: (id?: string) => void;
  createContentFilteringPolicy: (
    policy: Omit<ContentFilteringPolicy, 'id' | 'createdAt' | 'updatedAt'>
  ) => Promise<ContentFilteringPolicy>;
  updateContentFilteringPolicy: (
    id: string,
    policy: Partial<ContentFilteringPolicy>
  ) => Promise<void>;
  deleteContentFilteringPolicy: (id: string) => Promise<void>;

  // AI Behavior Settings Actions
  loadAIBehaviorSettings: () => Promise<void>;
  selectAIBehaviorSetting: (id?: string) => void;
  createAIBehaviorSetting: (
    settings: Omit<AIBehaviorSettings, 'id' | 'createdAt' | 'updatedAt'>
  ) => Promise<AIBehaviorSettings>;
  updateAIBehaviorSetting: (id: string, settings: Partial<AIBehaviorSettings>) => Promise<void>;
  deleteAIBehaviorSetting: (id: string) => Promise<void>;
  resetAIBehaviorSetting: (id: string) => Promise<void>;

  // Export/Import
  exportPreferences: () => Promise<string>;
  importPreferences: (jsonData: string) => Promise<void>;

  // Reset
  reset: () => void;
}

export const useUserPreferencesStore = create<UserPreferencesState>((set, get) => ({
  customAudienceProfiles: [],
  contentFilteringPolicies: [],
  aiBehaviorSettings: [],
  customPromptTemplates: [],
  customQualityThresholds: [],
  customVisualStyles: [],
  advancedMode: false,
  isLoading: false,

  setAdvancedMode: (enabled: boolean) => set({ advancedMode: enabled }),

  // Custom Audience Profile Actions
  loadCustomAudienceProfiles: async () => {
    set({ isLoading: true, error: undefined });
    try {
      const response = await fetch('/api/user-preferences/audience-profiles');
      if (!response.ok) throw new Error('Failed to load audience profiles');
      const profiles = await response.json();
      set({ customAudienceProfiles: profiles, isLoading: false });
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      set({ error: errorObj.message, isLoading: false });
    }
  },

  selectAudienceProfile: (id?: string) => set({ selectedAudienceProfileId: id }),

  createCustomAudienceProfile: async (profile) => {
    set({ isLoading: true, error: undefined });
    try {
      const response = await fetch('/api/user-preferences/audience-profiles', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(profile),
      });
      if (!response.ok) throw new Error('Failed to create audience profile');
      const newProfile = await response.json();
      set((state) => ({
        customAudienceProfiles: [...state.customAudienceProfiles, newProfile],
        isLoading: false,
      }));
      return newProfile;
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      set({ error: errorObj.message, isLoading: false });
      throw errorObj;
    }
  },

  updateCustomAudienceProfile: async (id: string, profile: Partial<CustomAudienceProfile>) => {
    set({ isLoading: true, error: undefined });
    try {
      const current = get().customAudienceProfiles.find((p) => p.id === id);
      if (!current) throw new Error('Profile not found');

      const response = await fetch(`/api/user-preferences/audience-profiles/${id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ ...current, ...profile }),
      });
      if (!response.ok) throw new Error('Failed to update audience profile');
      const updated = await response.json();
      set((state) => ({
        customAudienceProfiles: state.customAudienceProfiles.map((p) =>
          p.id === id ? updated : p
        ),
        isLoading: false,
      }));
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      set({ error: errorObj.message, isLoading: false });
      throw errorObj;
    }
  },

  deleteCustomAudienceProfile: async (id: string) => {
    set({ isLoading: true, error: undefined });
    try {
      const response = await fetch(`/api/user-preferences/audience-profiles/${id}`, {
        method: 'DELETE',
      });
      if (!response.ok) throw new Error('Failed to delete audience profile');
      set((state) => ({
        customAudienceProfiles: state.customAudienceProfiles.filter((p) => p.id !== id),
        selectedAudienceProfileId:
          state.selectedAudienceProfileId === id ? undefined : state.selectedAudienceProfileId,
        isLoading: false,
      }));
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      set({ error: errorObj.message, isLoading: false });
      throw errorObj;
    }
  },

  // Content Filtering Policy Actions
  loadContentFilteringPolicies: async () => {
    set({ isLoading: true, error: undefined });
    try {
      const response = await fetch('/api/user-preferences/filtering-policies');
      if (!response.ok) throw new Error('Failed to load filtering policies');
      const policies = await response.json();
      set({ contentFilteringPolicies: policies, isLoading: false });
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      set({ error: errorObj.message, isLoading: false });
    }
  },

  selectFilteringPolicy: (id?: string) => set({ selectedFilteringPolicyId: id }),

  createContentFilteringPolicy: async (policy) => {
    set({ isLoading: true, error: undefined });
    try {
      const response = await fetch('/api/user-preferences/filtering-policies', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(policy),
      });
      if (!response.ok) throw new Error('Failed to create filtering policy');
      const newPolicy = await response.json();
      set((state) => ({
        contentFilteringPolicies: [...state.contentFilteringPolicies, newPolicy],
        isLoading: false,
      }));
      return newPolicy;
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      set({ error: errorObj.message, isLoading: false });
      throw errorObj;
    }
  },

  updateContentFilteringPolicy: async (id: string, policy: Partial<ContentFilteringPolicy>) => {
    set({ isLoading: true, error: undefined });
    try {
      const current = get().contentFilteringPolicies.find((p) => p.id === id);
      if (!current) throw new Error('Policy not found');

      const response = await fetch(`/api/user-preferences/filtering-policies/${id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ ...current, ...policy }),
      });
      if (!response.ok) throw new Error('Failed to update filtering policy');
      const updated = await response.json();
      set((state) => ({
        contentFilteringPolicies: state.contentFilteringPolicies.map((p) =>
          p.id === id ? updated : p
        ),
        isLoading: false,
      }));
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      set({ error: errorObj.message, isLoading: false });
      throw errorObj;
    }
  },

  deleteContentFilteringPolicy: async (id: string) => {
    set({ isLoading: true, error: undefined });
    try {
      const response = await fetch(`/api/user-preferences/filtering-policies/${id}`, {
        method: 'DELETE',
      });
      if (!response.ok) throw new Error('Failed to delete filtering policy');
      set((state) => ({
        contentFilteringPolicies: state.contentFilteringPolicies.filter((p) => p.id !== id),
        selectedFilteringPolicyId:
          state.selectedFilteringPolicyId === id ? undefined : state.selectedFilteringPolicyId,
        isLoading: false,
      }));
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      set({ error: errorObj.message, isLoading: false });
      throw errorObj;
    }
  },

  // AI Behavior Settings Actions
  loadAIBehaviorSettings: async () => {
    set({ isLoading: true, error: undefined });
    try {
      const response = await fetch('/api/user-preferences/ai-behavior');
      if (!response.ok) throw new Error('Failed to load AI behavior settings');
      let settings = await response.json();

      // Ensure default settings exist
      if (settings.length === 0) {
        const defaultResponse = await fetch('/api/user-preferences/ai-behavior/default');
        if (defaultResponse.ok) {
          const defaultSettings = await defaultResponse.json();
          settings = [defaultSettings];
        }
      }

      set({ aiBehaviorSettings: settings, isLoading: false });
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      set({ error: errorObj.message, isLoading: false });
    }
  },

  selectAIBehaviorSetting: (id?: string) => set({ selectedAIBehaviorId: id }),

  createAIBehaviorSetting: async (settings) => {
    set({ isLoading: true, error: undefined });
    try {
      const response = await fetch('/api/user-preferences/ai-behavior', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(settings),
      });
      if (!response.ok) throw new Error('Failed to create AI behavior setting');
      const newSetting = await response.json();
      set((state) => ({
        aiBehaviorSettings: [...state.aiBehaviorSettings, newSetting],
        isLoading: false,
      }));
      return newSetting;
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      set({ error: errorObj.message, isLoading: false });
      throw errorObj;
    }
  },

  updateAIBehaviorSetting: async (id: string, settings: Partial<AIBehaviorSettings>) => {
    set({ isLoading: true, error: undefined });
    try {
      const current = get().aiBehaviorSettings.find((s) => s.id === id);
      if (!current) throw new Error('AI behavior setting not found');

      const response = await fetch(`/api/user-preferences/ai-behavior/${id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ ...current, ...settings }),
      });
      if (!response.ok) throw new Error('Failed to update AI behavior setting');
      const updated = await response.json();
      set((state) => ({
        aiBehaviorSettings: state.aiBehaviorSettings.map((s) => (s.id === id ? updated : s)),
        isLoading: false,
      }));
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      set({ error: errorObj.message, isLoading: false });
      throw errorObj;
    }
  },

  deleteAIBehaviorSetting: async (id: string) => {
    set({ isLoading: true, error: undefined });
    try {
      const response = await fetch(`/api/user-preferences/ai-behavior/${id}`, {
        method: 'DELETE',
      });
      if (!response.ok) throw new Error('Failed to delete AI behavior setting');
      set((state) => ({
        aiBehaviorSettings: state.aiBehaviorSettings.filter((s) => s.id !== id),
        selectedAIBehaviorId:
          state.selectedAIBehaviorId === id ? undefined : state.selectedAIBehaviorId,
        isLoading: false,
      }));
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      set({ error: errorObj.message, isLoading: false });
      throw errorObj;
    }
  },

  resetAIBehaviorSetting: async (id: string) => {
    set({ isLoading: true, error: undefined });
    try {
      const response = await fetch(`/api/user-preferences/ai-behavior/${id}/reset`, {
        method: 'POST',
      });
      if (!response.ok) throw new Error('Failed to reset AI behavior setting');
      const updated = await response.json();
      set((state) => ({
        aiBehaviorSettings: state.aiBehaviorSettings.map((s) => (s.id === id ? updated : s)),
        isLoading: false,
      }));
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      set({ error: errorObj.message, isLoading: false });
      throw errorObj;
    }
  },

  // Export/Import
  exportPreferences: async () => {
    try {
      const response = await fetch('/api/user-preferences/export');
      if (!response.ok) throw new Error('Failed to export preferences');
      const data = await response.json();
      return data.jsonData;
    } catch (error: unknown) {
      throw error instanceof Error ? error : new Error(String(error));
    }
  },

  importPreferences: async (jsonData: string) => {
    set({ isLoading: true, error: undefined });
    try {
      const response = await fetch('/api/user-preferences/import', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ jsonData }),
      });
      if (!response.ok) throw new Error('Failed to import preferences');

      await get().loadCustomAudienceProfiles();
      await get().loadContentFilteringPolicies();
      await get().loadAIBehaviorSettings();
      set({ isLoading: false });
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      set({ error: errorObj.message, isLoading: false });
      throw errorObj;
    }
  },

  reset: () =>
    set({
      customAudienceProfiles: [],
      selectedAudienceProfileId: undefined,
      contentFilteringPolicies: [],
      selectedFilteringPolicyId: undefined,
      aiBehaviorSettings: [],
      selectedAIBehaviorId: undefined,
      customPromptTemplates: [],
      customQualityThresholds: [],
      selectedQualityThresholdsId: undefined,
      customVisualStyles: [],
      selectedVisualStyleId: undefined,
      advancedMode: false,
      isLoading: false,
      error: undefined,
    }),
}));
