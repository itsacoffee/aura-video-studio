/**
 * Editing Intelligence Service
 * Provides AI-powered editing recommendations and timeline optimization
 */

const API_BASE = '/api/editing';

export interface CutPoint {
  timestamp: string;
  type:
    | 'NaturalPause'
    | 'SentenceBoundary'
    | 'BreathPoint'
    | 'FillerRemoval'
    | 'SceneTransition'
    | 'EmphasisPoint';
  confidence: number;
  reasoning: string;
  durationToRemove?: string;
}

export interface ScenePacingRecommendation {
  sceneIndex: number;
  currentDuration: string;
  recommendedDuration: string;
  engagementScore: number;
  issueType?:
    | 'TooSlow'
    | 'TooFast'
    | 'Monotonous'
    | 'PoorRhythm'
    | 'InformationOverload'
    | 'AttentionSpanExceeded';
  reasoning: string;
}

export interface PacingAnalysis {
  sceneRecommendations: ScenePacingRecommendation[];
  overallEngagementScore: number;
  slowSegments: string[];
  fastSegments: string[];
  contentDensity: number;
  summary: string;
}

export interface TransitionSuggestion {
  fromSceneIndex: number;
  toSceneIndex: number;
  location: string;
  type: 'Cut' | 'Fade' | 'Dissolve' | 'Wipe' | 'Zoom' | 'Slide' | 'None';
  duration: string;
  reasoning: string;
  confidence: number;
}

export interface EffectSuggestion {
  startTime: string;
  duration: string;
  effectType:
    | 'SlowMotion'
    | 'SpeedUp'
    | 'Zoom'
    | 'Pan'
    | 'ColorGrade'
    | 'Blur'
    | 'Vignette'
    | 'TextOverlay'
    | 'SplitScreen';
  purpose: 'Emphasis' | 'Transition' | 'Style' | 'Correction' | 'Engagement';
  parameters: Record<string, any>;
  reasoning: string;
  confidence: number;
}

export interface EngagementPoint {
  timestamp: string;
  predictedEngagement: number;
  context: string;
}

export interface EngagementCurve {
  points: EngagementPoint[];
  averageEngagement: number;
  retentionRisks: string[];
  hookStrength: number;
  endingImpact: number;
  boosterSuggestions: string[];
}

export interface QualityIssue {
  type:
    | 'LowResolution'
    | 'AudioClipping'
    | 'ColorInconsistency'
    | 'BlackFrame'
    | 'AudioDesync'
    | 'ContinuityError'
    | 'RenderingArtifact'
    | 'MissingAsset';
  severity: 'Info' | 'Warning' | 'Error' | 'Critical';
  location?: string;
  description: string;
  fixSuggestion?: string;
}

export interface TimelineAnalysisResult {
  cutPoints?: CutPoint[];
  pacingAnalysis?: PacingAnalysis;
  engagementAnalysis?: EngagementCurve;
  qualityIssues?: QualityIssue[];
  generalRecommendations: string[];
}

export interface AnalyzeTimelineRequest {
  jobId: string;
  includeCutPoints?: boolean;
  includePacing?: boolean;
  includeEngagement?: boolean;
  includeQuality?: boolean;
}

export interface EditingPacingRequest {
  jobId: string;
  targetDuration?: string;
  pacingStyle?: string;
}

export interface OptimizeDurationRequest {
  jobId: string;
  targetDuration: string;
  strategy: string;
}

/**
 * Analyze timeline for all issues and recommendations
 */
export async function analyzeTimeline(
  request: AnalyzeTimelineRequest
): Promise<TimelineAnalysisResult> {
  const response = await fetch(`${API_BASE}/analyze-timeline`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    throw new Error(`Failed to analyze timeline: ${response.statusText}`);
  }

  const result = await response.json();
  return result.analysis;
}

/**
 * Get cut point suggestions for timeline
 */
export async function suggestCuts(
  jobId: string
): Promise<{ cutPoints: CutPoint[]; awkwardPauses: CutPoint[] }> {
  const response = await fetch(`${API_BASE}/suggest-cuts`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(jobId),
  });

  if (!response.ok) {
    throw new Error(`Failed to suggest cuts: ${response.statusText}`);
  }

  const result = await response.json();
  return { cutPoints: result.cutPoints, awkwardPauses: result.awkwardPauses };
}

/**
 * Optimize timeline pacing
 */
export async function optimizePacing(
  request: EditingPacingRequest
): Promise<{ analysis: PacingAnalysis; slowSegments: any[] }> {
  const response = await fetch(`${API_BASE}/optimize-pacing`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    throw new Error(`Failed to optimize pacing: ${response.statusText}`);
  }

  const result = await response.json();
  return { analysis: result.analysis, slowSegments: result.slowSegments };
}

/**
 * Recommend transitions between scenes
 */
export async function recommendTransitions(
  jobId: string
): Promise<{ suggestions: TransitionSuggestion[]; jarringTransitions: TransitionSuggestion[] }> {
  const response = await fetch(`${API_BASE}/transitions`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(jobId),
  });

  if (!response.ok) {
    throw new Error(`Failed to recommend transitions: ${response.statusText}`);
  }

  const result = await response.json();
  return { suggestions: result.suggestions, jarringTransitions: result.jarringTransitions };
}

/**
 * Suggest effect applications
 */
export async function suggestEffects(jobId: string): Promise<EffectSuggestion[]> {
  const response = await fetch(`${API_BASE}/effects`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(jobId),
  });

  if (!response.ok) {
    throw new Error(`Failed to suggest effects: ${response.statusText}`);
  }

  const result = await response.json();
  return result.suggestions;
}

/**
 * Generate engagement analysis
 */
export async function analyzeEngagement(
  jobId: string
): Promise<{ engagementCurve: EngagementCurve; fatiguePoints: any[] }> {
  const response = await fetch(`${API_BASE}/engagement`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(jobId),
  });

  if (!response.ok) {
    throw new Error(`Failed to analyze engagement: ${response.statusText}`);
  }

  const result = await response.json();
  return { engagementCurve: result.engagementCurve, fatiguePoints: result.fatiguePoints };
}

/**
 * Run quality control checks
 */
export async function runQualityCheck(
  jobId: string
): Promise<{ issues: QualityIssue[]; summary: any }> {
  const response = await fetch(`${API_BASE}/quality-check`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(jobId),
  });

  if (!response.ok) {
    throw new Error(`Failed to run quality check: ${response.statusText}`);
  }

  const result = await response.json();
  return { issues: result.issues, summary: result.summary };
}

/**
 * Optimize timeline to target duration
 */
export async function optimizeDuration(request: OptimizeDurationRequest): Promise<any> {
  const response = await fetch(`${API_BASE}/optimize-duration`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    throw new Error(`Failed to optimize duration: ${response.statusText}`);
  }

  const result = await response.json();
  return result.timeline;
}
