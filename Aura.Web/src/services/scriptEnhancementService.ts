/**
 * Script Enhancement API service for AI-powered script improvements
 */

// Enums and Types
export enum StoryFrameworkType {
  HeroJourney = 'HeroJourney',
  ThreeAct = 'ThreeAct',
  ProblemSolution = 'ProblemSolution',
  AIDA = 'AIDA',
  BeforeAfter = 'BeforeAfter',
  Comparison = 'Comparison',
  Chronological = 'Chronological',
  CauseEffect = 'CauseEffect',
}

export enum EmotionalTone {
  Neutral = 'Neutral',
  Curious = 'Curious',
  Excited = 'Excited',
  Concerned = 'Concerned',
  Hopeful = 'Hopeful',
  Satisfied = 'Satisfied',
  Inspired = 'Inspired',
  Empowered = 'Empowered',
  Entertained = 'Entertained',
  Thoughtful = 'Thoughtful',
  Urgent = 'Urgent',
  Relieved = 'Relieved',
}

export enum SuggestionType {
  Structure = 'Structure',
  Hook = 'Hook',
  Dialog = 'Dialog',
  Pacing = 'Pacing',
  Emotion = 'Emotion',
  Clarity = 'Clarity',
  Engagement = 'Engagement',
  Transition = 'Transition',
  FactCheck = 'FactCheck',
  Tone = 'Tone',
  Pronunciation = 'Pronunciation',
  Callback = 'Callback',
}

// Interfaces
export interface EmotionalPoint {
  timePosition: number; // 0-1 position in video
  tone: EmotionalTone;
  intensity: number; // 0-100
  context?: string;
}

export interface ScriptAnalysis {
  structureScore: number; // 0-100
  emotionalCurveScore: number;
  engagementScore: number;
  clarityScore: number;
  hookStrength: number;
  issues: string[];
  strengths: string[];
  detectedFramework?: StoryFrameworkType;
  emotionalCurve: EmotionalPoint[];
  readabilityMetrics: Record<string, number>;
  analyzedAt: string;
}

export interface EnhancementSuggestion {
  suggestionId: string;
  type: SuggestionType;
  sceneIndex?: number;
  lineNumber?: number;
  originalText: string;
  suggestedText: string;
  explanation: string;
  confidenceScore: number; // 0-100
  benefits: string[];
  createdAt: string;
}

export interface StoryFramework {
  type: StoryFrameworkType;
  elements: Record<string, string>;
  missingElements: string[];
  complianceScore: number; // 0-100
  applicationNotes: string;
}

export interface ToneProfile {
  formalityLevel: number; // 0-100
  energyLevel: number;
  emotionLevel: number;
  personalityTraits: string[];
  brandVoice?: string;
  customAttributes?: Record<string, unknown>;
}

export interface EmotionalArc {
  targetCurve: EmotionalPoint[];
  curveSmoothnessScore: number;
  varietyScore: number;
  peakMoments: string[];
  valleyMoments: string[];
  arcStrategy: string;
  generatedAt: string;
}

export interface FactCheckFinding {
  claimText: string;
  verification: string; // "verified", "uncertain", "disputed", "incorrect"
  confidenceScore: number;
  source?: string;
  explanation?: string;
  suggestion?: string;
}

export interface ScriptDiff {
  type: string; // "added", "removed", "modified"
  lineNumber: number;
  oldText?: string;
  newText?: string;
  context?: string;
}

// Request/Response types
export interface ScriptAnalysisRequest {
  script: string;
  contentType?: string;
  targetAudience?: string;
  currentTone?: string;
}

export interface ScriptAnalysisResponse {
  success: boolean;
  analysis?: ScriptAnalysis;
  errorMessage?: string;
}

export interface ScriptEnhanceRequest {
  script: string;
  contentType?: string;
  targetAudience?: string;
  desiredTone?: string;
  focusAreas?: SuggestionType[];
  autoApply?: boolean;
  targetFramework?: StoryFrameworkType;
}

export interface ScriptEnhanceResponse {
  success: boolean;
  enhancedScript?: string;
  suggestions: EnhancementSuggestion[];
  changesSummary?: string;
  beforeAnalysis?: ScriptAnalysis;
  afterAnalysis?: ScriptAnalysis;
  errorMessage?: string;
}

export interface OptimizeHookRequest {
  script: string;
  contentType?: string;
  targetAudience?: string;
  targetSeconds?: number;
}

export interface OptimizeHookResponse {
  success: boolean;
  optimizedHook?: string;
  hookStrengthBefore: number;
  hookStrengthAfter: number;
  techniques: string[];
  explanation?: string;
  errorMessage?: string;
}

export interface EmotionalArcRequest {
  script: string;
  contentType?: string;
  targetAudience?: string;
  desiredJourney?: string;
}

export interface EmotionalArcResponse {
  success: boolean;
  currentArc?: EmotionalArc;
  optimizedArc?: EmotionalArc;
  suggestions: EnhancementSuggestion[];
  errorMessage?: string;
}

export interface AudienceConnectionRequest {
  script: string;
  targetAudience?: string;
  contentType?: string;
}

export interface AudienceConnectionResponse {
  success: boolean;
  enhancedScript?: string;
  suggestions: EnhancementSuggestion[];
  connectionScoreBefore: number;
  connectionScoreAfter: number;
  errorMessage?: string;
}

export interface FactCheckRequest {
  script: string;
  includeSources?: boolean;
}

export interface FactCheckResponse {
  success: boolean;
  findings: FactCheckFinding[];
  totalClaims: number;
  verifiedClaims: number;
  uncertainClaims: number;
  disclaimerSuggestion?: string;
  errorMessage?: string;
}

export interface ToneAdjustRequest {
  script: string;
  targetTone: ToneProfile;
  contentType?: string;
}

export interface ToneAdjustResponse {
  success: boolean;
  adjustedScript?: string;
  originalTone?: ToneProfile;
  achievedTone?: ToneProfile;
  changes: EnhancementSuggestion[];
  errorMessage?: string;
}

export interface ApplyFrameworkRequest {
  script: string;
  framework: StoryFrameworkType;
  contentType?: string;
  targetAudience?: string;
}

export interface ApplyFrameworkResponse {
  success: boolean;
  enhancedScript?: string;
  appliedFramework?: StoryFramework;
  suggestions: EnhancementSuggestion[];
  errorMessage?: string;
}

export interface GetSuggestionsRequest {
  script: string;
  contentType?: string;
  targetAudience?: string;
  filterTypes?: SuggestionType[];
  maxSuggestions?: number;
}

export interface GetSuggestionsResponse {
  success: boolean;
  suggestions: EnhancementSuggestion[];
  totalCount: number;
  errorMessage?: string;
}

export interface CompareVersionsRequest {
  versionA: string;
  versionB: string;
  includeAnalysis?: boolean;
}

export interface CompareVersionsResponse {
  success: boolean;
  differences: ScriptDiff[];
  analysisA?: ScriptAnalysis;
  analysisB?: ScriptAnalysis;
  improvementMetrics: Record<string, number>;
  errorMessage?: string;
}

const API_BASE = '/api/script';

export const scriptEnhancementService = {
  /**
   * Analyze script structure and quality
   */
  async analyzeScript(request: ScriptAnalysisRequest): Promise<ScriptAnalysisResponse> {
    const response = await fetch(`${API_BASE}/analyze`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error('Failed to analyze script');
    }

    return response.json();
  },

  /**
   * Apply comprehensive enhancements to a script
   */
  async enhanceScript(request: ScriptEnhanceRequest): Promise<ScriptEnhanceResponse> {
    const response = await fetch(`${API_BASE}/enhance`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error('Failed to enhance script');
    }

    return response.json();
  },

  /**
   * Optimize the opening hook (first 15 seconds)
   */
  async optimizeHook(request: OptimizeHookRequest): Promise<OptimizeHookResponse> {
    const response = await fetch(`${API_BASE}/optimize-hook`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error('Failed to optimize hook');
    }

    return response.json();
  },

  /**
   * Analyze and optimize emotional arc
   */
  async analyzeEmotionalArc(request: EmotionalArcRequest): Promise<EmotionalArcResponse> {
    const response = await fetch(`${API_BASE}/emotional-arc`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error('Failed to analyze emotional arc');
    }

    return response.json();
  },

  /**
   * Enhance audience connection
   */
  async enhanceAudienceConnection(
    request: AudienceConnectionRequest
  ): Promise<AudienceConnectionResponse> {
    const response = await fetch(`${API_BASE}/audience-connect`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error('Failed to enhance audience connection');
    }

    return response.json();
  },

  /**
   * Perform fact-checking on script claims
   */
  async factCheck(request: FactCheckRequest): Promise<FactCheckResponse> {
    const response = await fetch(`${API_BASE}/fact-check`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error('Failed to fact-check script');
    }

    return response.json();
  },

  /**
   * Adjust script tone and voice
   */
  async adjustTone(request: ToneAdjustRequest): Promise<ToneAdjustResponse> {
    const response = await fetch(`${API_BASE}/tone-adjust`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error('Failed to adjust tone');
    }

    return response.json();
  },

  /**
   * Apply specific storytelling framework
   */
  async applyFramework(request: ApplyFrameworkRequest): Promise<ApplyFrameworkResponse> {
    const response = await fetch(`${API_BASE}/apply-framework`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error('Failed to apply framework');
    }

    return response.json();
  },

  /**
   * Get individual enhancement suggestions
   */
  async getSuggestions(request: GetSuggestionsRequest): Promise<GetSuggestionsResponse> {
    const response = await fetch(`${API_BASE}/suggestions`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error('Failed to get suggestions');
    }

    return response.json();
  },

  /**
   * Compare two script versions
   */
  async compareVersions(request: CompareVersionsRequest): Promise<CompareVersionsResponse> {
    const response = await fetch(`${API_BASE}/compare-versions`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error('Failed to compare versions');
    }

    return response.json();
  },

  /**
   * Format score for display
   */
  formatScore(score: number): string {
    if (score >= 90) return 'Excellent';
    if (score >= 75) return 'Very Good';
    if (score >= 60) return 'Good';
    if (score >= 45) return 'Fair';
    return 'Needs Work';
  },

  /**
   * Get color for score
   */
  getScoreColor(score: number): string {
    if (score >= 75) return '#107c10'; // green
    if (score >= 60) return '#00ad56'; // lighter green
    if (score >= 45) return '#faa700'; // orange
    return '#d13438'; // red
  },

  /**
   * Get icon for suggestion type
   */
  getSuggestionTypeIcon(type: SuggestionType): string {
    const iconMap: Record<SuggestionType, string> = {
      [SuggestionType.Structure]: '🏗️',
      [SuggestionType.Hook]: '🎣',
      [SuggestionType.Dialog]: '💬',
      [SuggestionType.Pacing]: '⏱️',
      [SuggestionType.Emotion]: '❤️',
      [SuggestionType.Clarity]: '💡',
      [SuggestionType.Engagement]: '✨',
      [SuggestionType.Transition]: '🔄',
      [SuggestionType.FactCheck]: '✓',
      [SuggestionType.Tone]: '🎵',
      [SuggestionType.Pronunciation]: '🗣️',
      [SuggestionType.Callback]: '🔗',
    };
    return iconMap[type] || '📝';
  },

  /**
   * Get description for framework
   */
  getFrameworkDescription(framework: StoryFrameworkType): string {
    const descriptions: Record<StoryFrameworkType, string> = {
      [StoryFrameworkType.HeroJourney]:
        "Hero's Journey - Classic narrative structure following a protagonist's transformation",
      [StoryFrameworkType.ThreeAct]: 'Three-Act Structure - Setup, confrontation, and resolution',
      [StoryFrameworkType.ProblemSolution]:
        'Problem-Solution - Identify problem, explore impact, present solution',
      [StoryFrameworkType.AIDA]: 'AIDA - Attention, Interest, Desire, Action marketing framework',
      [StoryFrameworkType.BeforeAfter]:
        'Before-After-Bridge - Show transformation and how to achieve it',
      [StoryFrameworkType.Comparison]: 'Comparison - Compare and contrast different options',
      [StoryFrameworkType.Chronological]: 'Chronological - Time-based narrative sequence',
      [StoryFrameworkType.CauseEffect]: 'Cause-Effect - Explain causal relationships',
    };
    return descriptions[framework];
  },

  /**
   * Get emotional tone color
   */
  getEmotionalToneColor(tone: EmotionalTone): string {
    const colorMap: Record<EmotionalTone, string> = {
      [EmotionalTone.Neutral]: '#8a8886',
      [EmotionalTone.Curious]: '#8764B8',
      [EmotionalTone.Excited]: '#FFB900',
      [EmotionalTone.Concerned]: '#E81123',
      [EmotionalTone.Hopeful]: '#00B7C3',
      [EmotionalTone.Satisfied]: '#107C10',
      [EmotionalTone.Inspired]: '#00CC6A',
      [EmotionalTone.Empowered]: '#0078D4',
      [EmotionalTone.Entertained]: '#FF8C00',
      [EmotionalTone.Thoughtful]: '#5C2D91',
      [EmotionalTone.Urgent]: '#D13438',
      [EmotionalTone.Relieved]: '#00B294',
    };
    return colorMap[tone];
  },
};
