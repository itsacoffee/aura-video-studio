// Types for AI Learning System

export interface DecisionPattern {
  patternId: string;
  profileId: string;
  suggestionType: string;
  patternType: string;
  strength: number;
  occurrences: number;
  firstObserved: string;
  lastObserved: string;
  patternData?: Record<string, unknown>;
}

export interface LearningInsight {
  insightId: string;
  profileId: string;
  insightType: string;
  category: string;
  description: string;
  confidence: number;
  discoveredAt: string;
  isActionable: boolean;
  suggestedAction?: string;
}

export interface InferredPreference {
  preferenceId: string;
  profileId: string;
  category: string;
  preferenceName: string;
  preferenceValue: unknown;
  confidence: number;
  confidenceLevel?: 'low' | 'medium' | 'high';
  basedOnDecisions: number;
  inferredAt: string;
  isConfirmed: boolean;
  conflictsWith?: string;
}

export interface SuggestionPrediction {
  suggestionType: string;
  suggestionData: Record<string, unknown>;
  acceptanceProbability: number;
  rejectionProbability: number;
  modificationProbability: number;
  confidence: number;
  reasoningFactors: string[];
  similarPastDecisions?: string[];
}

export interface DecisionStatistics {
  profileId: string;
  suggestionType: string;
  totalDecisions: number;
  accepted: number;
  rejected: number;
  modified: number;
  acceptanceRate: number;
  rejectionRate: number;
  modificationRate: number;
  averageDecisionTimeSeconds: number;
  lastDecisionAt?: string;
}

export interface LearningMaturity {
  profileId: string;
  totalDecisions: number;
  decisionsByCategory: Record<string, number>;
  maturityLevel: 'nascent' | 'developing' | 'mature' | 'expert';
  overallConfidence: number;
  strengthCategories: string[];
  weakCategories: string[];
  lastAnalyzedAt: string;
}

export interface RankedSuggestion {
  rank: number;
  suggestion: Record<string, unknown>;
  prediction: SuggestionPrediction;
}

export interface LearningAnalytics {
  profileId: string;
  maturity: {
    totalDecisions: number;
    maturityLevel: string;
    overallConfidence: number;
    strengthCategories: string[];
    weakCategories: string[];
  };
  statisticsByCategory: Array<{
    suggestionType: string;
    totalDecisions: number;
    acceptanceRate: number;
    rejectionRate: number;
    modificationRate: number;
  }>;
  topPatterns: Array<{
    suggestionType: string;
    patternType: string;
    strength: number;
    occurrences: number;
  }>;
  highConfidencePreferences: Array<{
    category: string;
    preferenceName: string;
    preferenceValue: unknown;
    confidence: number;
    isConfirmed: boolean;
  }>;
  totalInsights: number;
  generatedAt: string;
}

// Request types
export interface RankSuggestionsRequest {
  profileId: string;
  suggestionType: string;
  suggestions: Array<Record<string, unknown>>;
}

export interface ConfirmPreferenceRequest {
  profileId: string;
  preferenceId: string;
  isCorrect: boolean;
  correctedValue?: unknown;
}

export interface AnalyzeRequest {
  profileId: string;
}

// Response types
export interface PatternsResponse {
  success: boolean;
  profileId: string;
  patterns: DecisionPattern[];
  count: number;
}

export interface InsightsResponse {
  success: boolean;
  profileId: string;
  insights: LearningInsight[];
  count: number;
}

export interface AnalysisResponse {
  success: boolean;
  profileId: string;
  analysis: {
    patternsIdentified: number;
    insightsGenerated: number;
    preferencesInferred: number;
    analyzedAt: string;
  };
}

export interface MaturityResponse {
  success: boolean;
  profileId: string;
  maturity: LearningMaturity;
}

export interface ConfidenceResponse {
  success: boolean;
  profileId: string;
  suggestionType: string;
  confidence: number;
  confidenceLevel: 'low' | 'medium' | 'high';
}
