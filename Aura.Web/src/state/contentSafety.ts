import { create } from 'zustand';

export type SafetyPolicyPreset = 'Unrestricted' | 'Minimal' | 'Moderate' | 'Strict' | 'Custom';

export type SafetyCategoryType =
  | 'Profanity'
  | 'Violence'
  | 'SexualContent'
  | 'HateSpeech'
  | 'DrugAlcohol'
  | 'ControversialTopics'
  | 'Copyright'
  | 'SelfHarm'
  | 'GraphicImagery'
  | 'Misinformation';

export type SafetyAction =
  | 'Block'
  | 'Warn'
  | 'RequireReview'
  | 'AutoFix'
  | 'AddDisclaimer'
  | 'LogOnly';

export type KeywordMatchType = 'WholeWord' | 'Substring' | 'Regex';

export type ContentRating = 'General' | 'ParentalGuidance' | 'Teen' | 'Mature' | 'Adult';

export interface SafetyCategory {
  type: SafetyCategoryType;
  threshold: number;
  isEnabled: boolean;
  defaultAction: SafetyAction;
  severityActions?: Record<number, SafetyAction>;
  customGuidelines?: string;
}

export interface KeywordRule {
  id?: string;
  keyword: string;
  matchType: KeywordMatchType;
  isCaseSensitive: boolean;
  action: SafetyAction;
  replacement?: string;
  contextExceptions?: string[];
  isRegex: boolean;
}

export interface TopicFilter {
  id?: string;
  topic: string;
  isBlocked: boolean;
  confidenceThreshold: number;
  action: SafetyAction;
  subtopics?: string[];
  allowedContexts?: string[];
}

export interface BrandSafetySettings {
  requiredKeywords?: string[];
  bannedCompetitors?: string[];
  brandTerminology?: string[];
  brandVoiceGuidelines?: string;
  requiredDisclaimers?: string[];
  minBrandVoiceScore: number;
}

export interface AgeAppropriatenessSettings {
  minimumAge: number;
  maximumAge: number;
  targetRating: ContentRating;
  requireParentalGuidance: boolean;
  ageSpecificRestrictions?: string[];
}

export interface CulturalSensitivitySettings {
  targetRegions?: string[];
  culturalTaboos?: Record<string, string[]>;
  avoidStereotypes: boolean;
  requireInclusiveLanguage: boolean;
  religiousSensitivities?: string[];
}

export interface ComplianceSettings {
  requiredDisclosures?: string[];
  coppaCompliant: boolean;
  gdprCompliant: boolean;
  ftcCompliant: boolean;
  industryRegulations?: string[];
  autoDisclosures?: Record<string, string>;
}

export interface SafetyPolicy {
  id: string;
  name: string;
  description?: string;
  isEnabled: boolean;
  allowUserOverride: boolean;
  preset: SafetyPolicyPreset;
  categories: Record<string, SafetyCategory>;
  keywordRules: KeywordRule[];
  topicFilters: TopicFilter[];
  brandSafety?: BrandSafetySettings;
  ageSettings?: AgeAppropriatenessSettings;
  culturalSettings?: CulturalSensitivitySettings;
  complianceSettings?: ComplianceSettings;
  isDefault: boolean;
  usageCount: number;
  lastUsedAt?: string;
  createdAt: string;
  updatedAt: string;
}

export interface SafetyViolation {
  id: string;
  category: string;
  severityScore: number;
  reason: string;
  matchedContent?: string;
  position?: number;
  recommendedAction: string;
  suggestedFix?: string;
  canOverride: boolean;
}

export interface SafetyWarning {
  id: string;
  category: string;
  message: string;
  context?: string;
  suggestions: string[];
}

export interface SafetyAnalysisResult {
  isSafe: boolean;
  overallSafetyScore: number;
  violations: SafetyViolation[];
  warnings: SafetyWarning[];
  categoryScores: Record<string, number>;
  requiresReview: boolean;
  allowWithDisclaimer: boolean;
  recommendedDisclaimer?: string;
  suggestedFixes: string[];
}

interface ContentSafetyState {
  policies: SafetyPolicy[];
  currentPolicyId: string | null;
  isLoading: boolean;
  error: string | null;
  lastAnalysisResult: SafetyAnalysisResult | null;

  loadPolicies: () => Promise<void>;
  loadPresets: () => Promise<void>;
  getPolicy: (id: string) => Promise<SafetyPolicy | null>;
  createPolicy: (policy: Partial<SafetyPolicy>) => Promise<SafetyPolicy | null>;
  updatePolicy: (id: string, policy: Partial<SafetyPolicy>) => Promise<SafetyPolicy | null>;
  deletePolicy: (id: string) => Promise<boolean>;
  setCurrentPolicy: (id: string) => void;
  analyzeContent: (content: string, policyId?: string) => Promise<SafetyAnalysisResult | null>;
  importKeywords: (text: string, defaultAction?: string) => Promise<KeywordRule[] | null>;
  getStarterLists: () => Promise<Record<string, string[]> | null>;
  getCommonTopics: () => Promise<string[] | null>;
  clearError: () => void;
}

export const useContentSafetyStore = create<ContentSafetyState>((set, get) => ({
  policies: [],
  currentPolicyId: null,
  isLoading: false,
  error: null,
  lastAnalysisResult: null,

  loadPolicies: async () => {
    set({ isLoading: true, error: null });
    try {
      const response = await fetch('/api/contentsafety/policies');
      if (!response.ok) {
        throw new Error('Failed to load policies');
      }
      const policies = await response.json();
      set({ policies, isLoading: false });
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      set({ error: errorObj.message, isLoading: false });
    }
  },

  loadPresets: async () => {
    set({ isLoading: true, error: null });
    try {
      const response = await fetch('/api/contentsafety/presets');
      if (!response.ok) {
        throw new Error('Failed to load presets');
      }
      const presets = await response.json();
      set({ policies: presets, isLoading: false });
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      set({ error: errorObj.message, isLoading: false });
    }
  },

  getPolicy: async (id: string) => {
    set({ isLoading: true, error: null });
    try {
      const response = await fetch(`/api/contentsafety/policies/${id}`);
      if (!response.ok) {
        throw new Error('Failed to load policy');
      }
      const policy = await response.json();
      set({ isLoading: false });
      return policy;
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      set({ error: errorObj.message, isLoading: false });
      return null;
    }
  },

  createPolicy: async (policy: Partial<SafetyPolicy>) => {
    set({ isLoading: true, error: null });
    try {
      const response = await fetch('/api/contentsafety/policies', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(policy),
      });
      if (!response.ok) {
        throw new Error('Failed to create policy');
      }
      const newPolicy = await response.json();
      set((state) => ({
        policies: [...state.policies, newPolicy],
        isLoading: false,
      }));
      return newPolicy;
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      set({ error: errorObj.message, isLoading: false });
      return null;
    }
  },

  updatePolicy: async (id: string, policy: Partial<SafetyPolicy>) => {
    set({ isLoading: true, error: null });
    try {
      const response = await fetch(`/api/contentsafety/policies/${id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(policy),
      });
      if (!response.ok) {
        throw new Error('Failed to update policy');
      }
      const updated = await response.json();
      set((state) => ({
        policies: state.policies.map((p) => (p.id === id ? updated : p)),
        isLoading: false,
      }));
      return updated;
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      set({ error: errorObj.message, isLoading: false });
      return null;
    }
  },

  deletePolicy: async (id: string) => {
    set({ isLoading: true, error: null });
    try {
      const response = await fetch(`/api/contentsafety/policies/${id}`, {
        method: 'DELETE',
      });
      if (!response.ok) {
        throw new Error('Failed to delete policy');
      }
      set((state) => ({
        policies: state.policies.filter((p) => p.id !== id),
        isLoading: false,
      }));
      return true;
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      set({ error: errorObj.message, isLoading: false });
      return false;
    }
  },

  setCurrentPolicy: (id: string) => {
    set({ currentPolicyId: id });
  },

  analyzeContent: async (content: string, policyId?: string) => {
    set({ isLoading: true, error: null });
    try {
      const response = await fetch('/api/contentsafety/analyze', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          content,
          policyId: policyId || get().currentPolicyId,
        }),
      });
      if (!response.ok) {
        throw new Error('Failed to analyze content');
      }
      const result = await response.json();
      set({ lastAnalysisResult: result, isLoading: false });
      return result;
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      set({ error: errorObj.message, isLoading: false });
      return null;
    }
  },

  importKeywords: async (text: string, defaultAction?: string) => {
    set({ isLoading: true, error: null });
    try {
      const response = await fetch('/api/contentsafety/keywords/import', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ text, defaultAction }),
      });
      if (!response.ok) {
        throw new Error('Failed to import keywords');
      }
      const data = await response.json();
      set({ isLoading: false });
      return data.rules;
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      set({ error: errorObj.message, isLoading: false });
      return null;
    }
  },

  getStarterLists: async () => {
    try {
      const response = await fetch('/api/contentsafety/keywords/starter-lists');
      if (!response.ok) {
        throw new Error('Failed to load starter lists');
      }
      return await response.json();
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      set({ error: errorObj.message });
      return null;
    }
  },

  getCommonTopics: async () => {
    try {
      const response = await fetch('/api/contentsafety/topics/common');
      if (!response.ok) {
        throw new Error('Failed to load common topics');
      }
      return await response.json();
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      set({ error: errorObj.message });
      return null;
    }
  },

  clearError: () => set({ error: null }),
}));
