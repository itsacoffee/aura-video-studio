/**
 * Ideation API service for AI-powered brainstorming and concept generation
 */

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

const API_BASE = '/api/ideation';

export const ideationService = {
  /**
   * Generate creative concept variations from a topic
   */
  async brainstorm(request: BrainstormRequest): Promise<BrainstormResponse> {
    const response = await fetch(`${API_BASE}/brainstorm`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error('Failed to brainstorm concepts');
    }

    return response.json();
  },

  /**
   * Expand brief with AI clarifying questions
   */
  async expandBrief(request: ExpandBriefRequest): Promise<ExpandBriefResponse> {
    const response = await fetch(`${API_BASE}/expand-brief`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error('Failed to expand brief');
    }

    return response.json();
  },

  /**
   * Get trending topics for a niche
   */
  async getTrending(niche?: string, maxResults?: number): Promise<TrendingTopicsResponse> {
    const params = new URLSearchParams();
    if (niche) {
      params.append('niche', niche);
    }
    if (maxResults) {
      params.append('maxResults', maxResults.toString());
    }

    const response = await fetch(`${API_BASE}/trending?${params}`);

    if (!response.ok) {
      throw new Error('Failed to get trending topics');
    }

    return response.json();
  },

  /**
   * Analyze content gaps and opportunities
   */
  async analyzeGaps(request: GapAnalysisRequest): Promise<GapAnalysisResponse> {
    const response = await fetch(`${API_BASE}/gap-analysis`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error('Failed to analyze content gaps');
    }

    return response.json();
  },

  /**
   * Gather research and facts for a topic
   */
  async gatherResearch(request: ResearchRequest): Promise<ResearchResponse> {
    const response = await fetch(`${API_BASE}/research`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error('Failed to gather research');
    }

    return response.json();
  },

  /**
   * Generate visual storyboard for a concept
   */
  async generateStoryboard(request: StoryboardRequest): Promise<StoryboardResponse> {
    const response = await fetch(`${API_BASE}/storyboard`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error('Failed to generate storyboard');
    }

    return response.json();
  },

  /**
   * Refine a selected concept
   */
  async refineConcept(request: RefineConceptRequest): Promise<RefineConceptResponse> {
    const response = await fetch(`${API_BASE}/refine`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error('Failed to refine concept');
    }

    return response.json();
  },

  /**
   * Get clarifying questions from AI
   */
  async getQuestions(request: QuestionsRequest): Promise<QuestionsResponse> {
    const response = await fetch(`${API_BASE}/questions`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error('Failed to get clarifying questions');
    }

    return response.json();
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
};
