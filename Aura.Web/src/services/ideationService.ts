/**
 * Ideation API service for AI-powered brainstorming and concept generation
 */

import { apiUrl } from '../config/api';

export interface ConceptIdea {
  conceptId: string;
  title: string;
  description: string;
  angle: string;
  targetAudience: string;
  pros: string[];
  cons: string[];
  appealScore: number;
  hook: string;
  talkingPoints?: string[];
  demographicScores?: Record<string, number>;
  createdAt?: string;
  // Enhanced high-value fields for better concept generation
  uniqueValue?: string; // What makes this concept stand out from competitors
  contentGap?: string; // What competitors are missing that this addresses
  keyInsights?: string[]; // Specific actionable insights
  visualSuggestions?: string[]; // Specific visual ideas for the video
  monetizationPotential?: string; // High/Medium/Low with reasoning
  viralityScore?: number; // 0-100 score for potential virality
}

export interface BriefRequirements {
  topic: string;
  goal?: string;
  audience?: string;
  tone?: string;
  durationSeconds?: number;
  platform?: string;
  keywords?: string[];
  additionalDetails?: Record<string, string>;
}

export interface TrendingTopicInsights {
  whyTrending: string;
  audienceEngagement: string;
  contentAngles: string[];
  demographicAppeal: string;
  viralityScore: number;
}

export interface TrendingTopic {
  topicId: string;
  topic: string;
  trendScore: number;
  searchVolume?: string;
  competition?: string;
  seasonality?: string;
  lifecycle?: string;
  relatedTopics?: string[];
  detectedAt?: string;
  aiInsights?: TrendingTopicInsights;
  hashtags?: string[];
  trendVelocity?: number;
  estimatedAudience?: number;
}

export interface ResearchFinding {
  findingId: string;
  fact: string;
  source?: string;
  credibilityScore: number;
  relevanceScore: number;
  example?: string;
  gatheredAt?: string;
}

export interface StoryboardScene {
  sceneNumber: number;
  description: string;
  visualStyle: string;
  durationSeconds: number;
  purpose: string;
  shotList?: string[];
  transitionType?: string;
}

export interface ClarifyingQuestion {
  questionId: string;
  question: string;
  context: string;
  suggestedAnswers?: string[];
  questionType: string;
}

export interface BrainstormRequest {
  topic: string;
  audience?: string;
  tone?: string;
  targetDuration?: number;
  platform?: string;
  conceptCount?: number;
  ragConfiguration?: RagConfigurationDto;
  llmParameters?: {
    temperature?: number;
    topP?: number;
    topK?: number;
    maxTokens?: number;
    frequencyPenalty?: number;
    presencePenalty?: number;
    stopSequences?: string[];
    modelOverride?: string;
  };
}

/**
 * RAG (Retrieval-Augmented Generation) configuration
 * Matches the RagConfigurationDto in the backend
 */
export interface RagConfigurationDto {
  enabled: boolean;
  topK?: number; // Default: 5
  minimumScore?: number; // Default: 0.6
  maxContextTokens?: number; // Default: 2000
  includeCitations?: boolean; // Default: true
  tightenClaims?: boolean; // Default: false
}

export interface BrainstormResponse {
  success: boolean;
  concepts: ConceptIdea[];
  originalTopic: string;
  generatedAt: string;
  count: number;
}

export interface ExpandBriefRequest {
  projectId: string;
  currentBrief: BriefRequirements;
  userMessage?: string;
}

export interface ExpandBriefResponse {
  success: boolean;
  updatedBrief?: BriefRequirements;
  questions?: ClarifyingQuestion[];
  aiResponse?: string;
}

export interface TrendingTopicsResponse {
  success: boolean;
  topics: TrendingTopic[];
  analyzedAt: string;
  count: number;
}

export interface GapAnalysisRequest {
  niche?: string;
  existingTopics?: string[];
  competitorTopics?: string[];
}

export interface GapAnalysisResponse {
  success: boolean;
  missingTopics: string[];
  opportunities: TrendingTopic[];
  oversaturatedTopics: string[];
  uniqueAngles?: Record<string, string[]>;
}

export interface ResearchRequest {
  topic: string;
  maxFindings?: number;
}

export interface ResearchResponse {
  success: boolean;
  findings: ResearchFinding[];
  topic: string;
  gatheredAt: string;
  count: number;
}

export interface StoryboardRequest {
  concept: ConceptIdea;
  targetDurationSeconds: number;
}

export interface StoryboardResponse {
  success: boolean;
  scenes: StoryboardScene[];
  conceptTitle: string;
  totalDurationSeconds: number;
  generatedAt: string;
  sceneCount: number;
}

export interface RefineConceptRequest {
  concept: ConceptIdea;
  refinementDirection: string;
  secondConcept?: ConceptIdea;
  targetAudience?: string;
}

export interface RefineConceptResponse {
  success: boolean;
  refinedConcept: ConceptIdea;
  changesSummary: string;
}

export interface QuestionsRequest {
  projectId: string;
  currentBrief?: BriefRequirements;
}

export interface QuestionsResponse {
  success: boolean;
  questions: ClarifyingQuestion[];
  context: string;
  count: number;
}

export interface EnhanceTopicRequest {
  topic: string;
  videoType?: string;
  targetAudience?: string;
  keyMessage?: string;
}

export interface PromptQualityAnalysisResponse {
  success: boolean;
  score: number;
  level: 'excellent' | 'good' | 'fair' | 'poor';
  metrics: {
    length: number;
    specificity: number;
    clarity: number;
    actionability: number;
    engagement: number;
    alignment: number;
  };
  suggestions: Array<{
    type: 'success' | 'warning' | 'info' | 'tip';
    message: string;
  }>;
  generatedAt: string;
}

export interface EnhanceTopicResponse {
  success: boolean;
  enhancedTopic: string;
  originalTopic: string;
  improvements?: string;
  generatedAt: string;
}

const API_BASE = '/api/ideation';

export const ideationService = {
  /**
   * Generate creative concept variations from a topic
   */
  async brainstorm(request: BrainstormRequest): Promise<BrainstormResponse> {
    try {
      const response = await fetch(apiUrl(`${API_BASE}/brainstorm`), {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(request),
      });

      if (!response.ok) {
        // Try to get error details from response
        let errorMessage = 'Failed to brainstorm concepts';
        let suggestions: string[] = [];
        try {
          const errorData = await response.json();
          errorMessage = errorData.message || errorData.error || errorMessage;
          if (errorData.suggestions && Array.isArray(errorData.suggestions)) {
            suggestions = errorData.suggestions;
          }
        } catch {
          // If response isn't JSON, use status text
          errorMessage = response.statusText || errorMessage;
        }
        console.error('[ideationService] Brainstorm failed:', {
          status: response.status,
          statusText: response.statusText,
          errorMessage,
          suggestions,
        });

        // Create error with suggestions attached
        const error = new Error(errorMessage) as Error & {
          response?: { data?: { suggestions?: string[] } };
        };
        if (suggestions.length > 0) {
          error.response = { data: { suggestions } };
        }
        throw error;
      }

      return await response.json();
    } catch (error) {
      // Re-throw if it's already an Error with a message
      if (error instanceof Error) {
        throw error;
      }
      // Otherwise wrap in Error
      throw new Error(`Failed to brainstorm concepts: ${String(error)}`);
    }
  },

  /**
   * Expand brief with AI clarifying questions
   */
  async expandBrief(request: ExpandBriefRequest): Promise<ExpandBriefResponse> {
    try {
      const response = await fetch(apiUrl(`${API_BASE}/expand-brief`), {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(request),
      });

      if (!response.ok) {
        let errorMessage = 'Failed to expand brief';
        try {
          const errorData = await response.json();
          errorMessage = errorData.message || errorData.error || errorMessage;
        } catch {
          errorMessage = response.statusText || errorMessage;
        }
        throw new Error(errorMessage);
      }

      return await response.json();
    } catch (error) {
      if (error instanceof Error) {
        throw error;
      }
      throw new Error(`Failed to expand brief: ${String(error)}`);
    }
  },

  /**
   * Get trending topics for a niche
   */
  async getTrending(niche?: string, maxResults?: number): Promise<TrendingTopicsResponse> {
    try {
      const params = new URLSearchParams();
      if (niche) {
        params.append('niche', niche);
      }
      if (maxResults) {
        params.append('maxResults', maxResults.toString());
      }

      const response = await fetch(apiUrl(`${API_BASE}/trending?${params}`));

      if (!response.ok) {
        let errorMessage = 'Failed to get trending topics';
        try {
          const errorData = await response.json();
          errorMessage = errorData.message || errorData.error || errorMessage;
        } catch {
          errorMessage = response.statusText || errorMessage;
        }
        throw new Error(errorMessage);
      }

      return await response.json();
    } catch (error) {
      if (error instanceof Error) {
        throw error;
      }
      throw new Error(`Failed to get trending topics: ${String(error)}`);
    }
  },

  /**
   * Analyze content gaps and opportunities
   */
  async analyzeGaps(request: GapAnalysisRequest): Promise<GapAnalysisResponse> {
    try {
      const response = await fetch(apiUrl(`${API_BASE}/gap-analysis`), {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(request),
      });

      if (!response.ok) {
        let errorMessage = 'Failed to analyze content gaps';
        try {
          const errorData = await response.json();
          errorMessage = errorData.message || errorData.error || errorMessage;
        } catch {
          errorMessage = response.statusText || errorMessage;
        }
        throw new Error(errorMessage);
      }

      return await response.json();
    } catch (error) {
      if (error instanceof Error) {
        throw error;
      }
      throw new Error(`Failed to analyze content gaps: ${String(error)}`);
    }
  },

  /**
   * Gather research and facts for a topic
   */
  async gatherResearch(request: ResearchRequest): Promise<ResearchResponse> {
    try {
      const response = await fetch(apiUrl(`${API_BASE}/research`), {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(request),
      });

      if (!response.ok) {
        let errorMessage = 'Failed to gather research';
        try {
          const errorData = await response.json();
          errorMessage = errorData.message || errorData.error || errorMessage;
        } catch {
          errorMessage = response.statusText || errorMessage;
        }
        throw new Error(errorMessage);
      }

      return await response.json();
    } catch (error) {
      if (error instanceof Error) {
        throw error;
      }
      throw new Error(`Failed to gather research: ${String(error)}`);
    }
  },

  /**
   * Generate visual storyboard for a concept
   */
  async generateStoryboard(request: StoryboardRequest): Promise<StoryboardResponse> {
    try {
      const response = await fetch(apiUrl(`${API_BASE}/storyboard`), {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(request),
      });

      if (!response.ok) {
        let errorMessage = 'Failed to generate storyboard';
        try {
          const errorData = await response.json();
          errorMessage = errorData.message || errorData.error || errorMessage;
        } catch {
          errorMessage = response.statusText || errorMessage;
        }
        throw new Error(errorMessage);
      }

      return await response.json();
    } catch (error) {
      if (error instanceof Error) {
        throw error;
      }
      throw new Error(`Failed to generate storyboard: ${String(error)}`);
    }
  },

  /**
   * Refine a selected concept
   */
  async refineConcept(request: RefineConceptRequest): Promise<RefineConceptResponse> {
    try {
      const response = await fetch(apiUrl(`${API_BASE}/refine`), {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(request),
      });

      if (!response.ok) {
        let errorMessage = 'Failed to refine concept';
        try {
          const errorData = await response.json();
          errorMessage = errorData.message || errorData.error || errorMessage;
        } catch {
          errorMessage = response.statusText || errorMessage;
        }
        throw new Error(errorMessage);
      }

      return await response.json();
    } catch (error) {
      if (error instanceof Error) {
        throw error;
      }
      throw new Error(`Failed to refine concept: ${String(error)}`);
    }
  },

  /**
   * Analyze prompt quality using LLM-based analysis
   */
  async analyzePromptQuality(request: {
    topic: string;
    videoType?: string;
    targetAudience?: string;
    keyMessage?: string;
    ragConfiguration?: RagConfigurationDto;
  }): Promise<PromptQualityAnalysisResponse> {
    try {
      const response = await fetch(apiUrl(`${API_BASE}/analyze-prompt-quality`), {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(request),
      });

      if (!response.ok) {
        let errorMessage = 'Failed to analyze prompt quality';
        try {
          const errorData = await response.json();
          errorMessage = errorData.message || errorData.error || errorMessage;
        } catch {
          errorMessage = response.statusText || errorMessage;
        }
        throw new Error(errorMessage);
      }

      return await response.json();
    } catch (error) {
      if (error instanceof Error) {
        throw error;
      }
      throw new Error(`Failed to analyze prompt quality: ${String(error)}`);
    }
  },

  /**
   * Enhance/improve a video topic description using AI
   */
  async enhanceTopic(request: EnhanceTopicRequest): Promise<EnhanceTopicResponse> {
    try {
      const response = await fetch(apiUrl(`${API_BASE}/enhance-topic`), {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(request),
      });

      if (!response.ok) {
        let errorMessage = 'Failed to enhance topic';
        try {
          const errorData = await response.json();
          errorMessage = errorData.message || errorData.error || errorMessage;
        } catch {
          errorMessage = response.statusText || errorMessage;
        }
        throw new Error(errorMessage);
      }

      return await response.json();
    } catch (error) {
      if (error instanceof Error) {
        throw error;
      }
      throw new Error(`Failed to enhance topic: ${String(error)}`);
    }
  },

  /**
   * Get clarifying questions from AI
   */
  async getQuestions(request: QuestionsRequest): Promise<QuestionsResponse> {
    try {
      const response = await fetch(apiUrl(`${API_BASE}/questions`), {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(request),
      });

      if (!response.ok) {
        let errorMessage = 'Failed to get clarifying questions';
        try {
          const errorData = await response.json();
          errorMessage = errorData.message || errorData.error || errorMessage;
        } catch {
          errorMessage = response.statusText || errorMessage;
        }
        throw new Error(errorMessage);
      }

      return await response.json();
    } catch (error) {
      if (error instanceof Error) {
        throw error;
      }
      throw new Error(`Failed to get clarifying questions: ${String(error)}`);
    }
  },

  /**
   * Format appeal score for display
   */
  formatAppealScore(score: number): string {
    if (score >= 90) return 'Excellent';
    if (score >= 75) return 'Very Good';
    if (score >= 60) return 'Good';
    if (score >= 45) return 'Fair';
    return 'Needs Work';
  },

  /**
   * Get color for appeal score
   */
  getAppealScoreColor(score: number): string {
    if (score >= 75) return '#107c10'; // green
    if (score >= 60) return '#00ad56'; // lighter green
    if (score >= 45) return '#faa700'; // orange
    return '#d13438'; // red
  },

  /**
   * Get icon for storytelling angle
   */
  getAngleIcon(angle: string): string {
    const angleMap: Record<string, string> = {
      Tutorial: '📚',
      Narrative: '📖',
      'Case Study': '🔍',
      Comparison: '⚖️',
      Interview: '🎤',
      Documentary: '🎬',
      'Behind-the-Scenes': '🎭',
      'Expert Analysis': '🎓',
      "Beginner's Guide": '🌟',
      'Deep Dive': '🔬',
    };
    return angleMap[angle] || '💡';
  },

  /**
   * Format virality score for display
   */
  formatViralityScore(score: number | undefined): string {
    if (score === undefined || score === null) return 'N/A';
    if (score >= 85) return 'Viral Potential';
    if (score >= 70) return 'High Share';
    if (score >= 55) return 'Good Share';
    return 'Low Share';
  },

  /**
   * Get color for virality score
   */
  getViralityScoreColor(score: number | undefined): string {
    if (score === undefined || score === null) return '#888888'; // gray
    if (score >= 85) return '#9b4dca'; // purple for viral
    if (score >= 70) return '#107c10'; // green
    if (score >= 55) return '#faa700'; // orange
    return '#d13438'; // red
  },

  /**
   * Get icon for monetization potential
   */
  getMonetizationIcon(potential: string | undefined): string {
    if (!potential) return '💰';
    const lower = potential.toLowerCase();
    if (lower.includes('high')) return '💰💰💰';
    if (lower.includes('medium')) return '💰💰';
    if (lower.includes('low')) return '💰';
    return '💰';
  },

  /**
   * Get color for monetization potential
   */
  getMonetizationColor(potential: string | undefined): string {
    if (!potential) return '#888888'; // gray
    const lower = potential.toLowerCase();
    if (lower.includes('high')) return '#107c10'; // green
    if (lower.includes('medium')) return '#faa700'; // orange
    if (lower.includes('low')) return '#d13438'; // red
    return '#888888';
  },
};
