/**
 * Audio Intelligence API service for AI-powered music, voice direction, and audio mixing
 */

import { apiUrl } from '../config/api';

const API_BASE = `${apiUrl}/audio`;

// Enums
export enum MusicMood {
  Neutral = 'Neutral',
  Happy = 'Happy',
  Sad = 'Sad',
  Energetic = 'Energetic',
  Calm = 'Calm',
  Dramatic = 'Dramatic',
  Tense = 'Tense',
  Uplifting = 'Uplifting',
  Melancholic = 'Melancholic',
  Mysterious = 'Mysterious',
  Playful = 'Playful',
  Serious = 'Serious',
  Romantic = 'Romantic',
  Epic = 'Epic',
  Ambient = 'Ambient',
}

export enum MusicGenre {
  Cinematic = 'Cinematic',
  Electronic = 'Electronic',
  Rock = 'Rock',
  Pop = 'Pop',
  Ambient = 'Ambient',
  Classical = 'Classical',
  Jazz = 'Jazz',
  HipHop = 'HipHop',
  Folk = 'Folk',
  Corporate = 'Corporate',
  Orchestral = 'Orchestral',
  Indie = 'Indie',
  LoFi = 'LoFi',
  Motivational = 'Motivational',
}

export enum EnergyLevel {
  VeryLow = 'VeryLow',
  Low = 'Low',
  Medium = 'Medium',
  High = 'High',
  VeryHigh = 'VeryHigh',
}

export enum EmotionalDelivery {
  Neutral = 'Neutral',
  Excited = 'Excited',
  Serious = 'Serious',
  Warm = 'Warm',
  Friendly = 'Friendly',
  Professional = 'Professional',
  Urgent = 'Urgent',
  Calm = 'Calm',
  Enthusiastic = 'Enthusiastic',
  Authoritative = 'Authoritative',
}

export enum SoundEffectType {
  Transition = 'Transition',
  Impact = 'Impact',
  Whoosh = 'Whoosh',
  Click = 'Click',
  UI = 'UI',
  Ambient = 'Ambient',
  Nature = 'Nature',
  Technology = 'Technology',
  Action = 'Action',
  Notification = 'Notification',
}

// Interfaces
export interface MusicTrack {
  trackId: string;
  title: string;
  artist?: string;
  genre: MusicGenre;
  mood: MusicMood;
  energy: EnergyLevel;
  bpm: number;
  duration: string; // ISO 8601 duration
  filePath: string;
  beatTimestamps?: number[];
  metadata?: Record<string, unknown>;
}

export interface VoiceDirection {
  lineId: string;
  emotion: EmotionalDelivery;
  emphasisWords: string[];
  paceMultiplier: number; // 0.5-2.0, where 1.0 is normal
  tone: string;
  pauses: PausePoint[];
  pronunciationGuide?: Record<string, string>;
}

export interface PausePoint {
  characterPosition: number;
  duration: string; // ISO 8601 duration
}

export interface SoundEffect {
  effectId: string;
  type: SoundEffectType;
  description: string;
  timestamp: string; // ISO 8601 duration
  duration: string; // ISO 8601 duration
  volume: number; // 0-100
  purpose: string;
  filePath?: string;
}

export interface AudioMixing {
  musicVolume: number; // 0-100
  narrationVolume: number; // 0-100
  soundEffectsVolume: number; // 0-100
  ducking: DuckingSettings;
  eq: EqualizationSettings;
  compression: CompressionSettings;
  normalize: boolean;
  targetLUFS: number;
}

export interface DuckingSettings {
  duckDepthDb: number;
  attackTime: string; // ISO 8601 duration
  releaseTime: string; // ISO 8601 duration
  threshold: number;
}

export interface EqualizationSettings {
  highPassFrequency: number;
  presenceBoost: number;
  deEsserReduction: number;
}

export interface CompressionSettings {
  threshold: number;
  ratio: number;
  attackTime: string; // ISO 8601 duration
  releaseTime: string; // ISO 8601 duration
  makeupGain: number;
}

export interface BeatMarker {
  timestamp: number; // seconds
  strength: number; // 0-1
  isDownbeat: boolean;
  musicalPhrase: number;
}

export interface MusicPrompt {
  promptId: string;
  mood: MusicMood;
  genre: MusicGenre;
  energy: EnergyLevel;
  targetDuration: string; // ISO 8601 duration
  targetBPM?: number;
  instrumentation: string;
  style: string;
  referenceTrackId?: string;
  createdAt: string;
}

export interface MusicRecommendation {
  track: MusicTrack;
  relevanceScore: number; // 0-100
  reasoning: string;
  matchingAttributes: string[];
  suggestedStartTime?: string; // ISO 8601 duration
  suggestedDuration?: string; // ISO 8601 duration
}

export interface AudioContinuity {
  styleConsistencyScore: number; // 0-100
  volumeConsistencyScore: number; // 0-100
  toneConsistencyScore: number; // 0-100
  issues: string[];
  suggestions: string[];
  checkedAt: string;
}

export interface SyncPoint {
  timestamp: string; // ISO 8601 duration
  audioEvent: string;
  visualEvent: string;
  offset: number; // seconds
  isAligned: boolean;
}

export interface SyncAnalysis {
  syncPoints: SyncPoint[];
  overallSyncScore: number; // 0-100
  recommendations: string[];
  analyzedAt: string;
}

export interface ScriptAudioAnalysis {
  emotionalArc: MusicMood[];
  energyProgression: EnergyLevel[];
  soundEffects: SoundEffectSuggestion[];
  voiceHints: VoiceDirectionHint[];
  overallTone: string;
  estimatedDuration: string; // ISO 8601 duration
}

export interface SoundEffectSuggestion {
  sceneIndex: number;
  estimatedTimestamp: string; // ISO 8601 duration
  type: SoundEffectType;
  trigger: string;
  description: string;
  confidence: number; // 0-100
}

export interface VoiceDirectionHint {
  sceneIndex: number;
  context: string;
  suggestedEmotion: EmotionalDelivery;
  keyWords: string[];
  reasoning: string;
}

export interface MusicSearchParams {
  mood?: MusicMood;
  genre?: MusicGenre;
  energy?: EnergyLevel;
  minBPM?: number;
  maxBPM?: number;
  minDuration?: string; // ISO 8601 duration
  maxDuration?: string; // ISO 8601 duration
  searchQuery?: string;
  limit?: number;
}

// Request interfaces
export interface AnalyzeScriptRequest {
  script: string;
  contentType?: string;
  targetAudience?: string;
  sceneDurations?: string[]; // ISO 8601 durations
}

export interface SuggestMusicRequest {
  mood: MusicMood;
  preferredGenre?: MusicGenre;
  energy: EnergyLevel;
  duration: string; // ISO 8601 duration
  context?: string;
  maxResults?: number;
}

export interface DetectBeatsRequest {
  filePath: string;
  minBPM?: number;
  maxBPM?: number;
}

export interface VoiceDirectionRequest {
  script: string;
  contentType?: string;
  targetAudience?: string;
  keyMessages?: string[];
}

export interface SoundEffectRequest {
  script: string;
  sceneDurations?: string[]; // ISO 8601 durations
  contentType?: string;
}

export interface MixingSuggestionsRequest {
  contentType: string;
  hasNarration: boolean;
  hasMusic: boolean;
  hasSoundEffects: boolean;
  targetLUFS?: number;
}

export interface MusicPromptRequest {
  mood: MusicMood;
  genre: MusicGenre;
  energy: EnergyLevel;
  duration: string; // ISO 8601 duration
  additionalContext?: string;
}

export interface SyncAnalysisRequest {
  audioBeatTimestamps: string[]; // ISO 8601 durations
  visualTransitionTimestamps: string[]; // ISO 8601 durations
  videoDuration: string; // ISO 8601 duration
}

export interface ContinuityCheckRequest {
  audioSegmentPaths: string[];
  targetStyle?: string;
}

// Audio Intelligence Service
class AudioIntelligenceService {
  /**
   * Analyze script for audio requirements
   */
  async analyzeScript(request: AnalyzeScriptRequest): Promise<ScriptAudioAnalysis> {
    const response = await fetch(`${API_BASE}/analyze-script`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error(`Failed to analyze script: ${response.statusText}`);
    }

    const data = await response.json();
    if (!data.success) {
      throw new Error(data.error || 'Failed to analyze script');
    }

    return data.analysis;
  }

  /**
   * Get music recommendations based on mood and context
   */
  async suggestMusic(request: SuggestMusicRequest): Promise<MusicRecommendation[]> {
    const response = await fetch(`${API_BASE}/suggest-music`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error(`Failed to suggest music: ${response.statusText}`);
    }

    const data = await response.json();
    if (!data.success) {
      throw new Error(data.error || 'Failed to suggest music');
    }

    return data.recommendations;
  }

  /**
   * Detect beats in a music file
   */
  async detectBeats(request: DetectBeatsRequest): Promise<{
    beats: BeatMarker[];
    bpm: number;
    phrases: Array<{ phraseNumber: number; start: string; end: string }>;
    climaxMoments: string[];
  }> {
    const response = await fetch(`${API_BASE}/detect-beats`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error(`Failed to detect beats: ${response.statusText}`);
    }

    const data = await response.json();
    if (!data.success) {
      throw new Error(data.error || 'Failed to detect beats');
    }

    return {
      beats: data.beats,
      bpm: data.bpm,
      phrases: data.phrases,
      climaxMoments: data.climaxMoments,
    };
  }

  /**
   * Generate voice direction for TTS
   */
  async generateVoiceDirection(request: VoiceDirectionRequest): Promise<VoiceDirection[]> {
    const response = await fetch(`${API_BASE}/voice-direction`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error(`Failed to generate voice direction: ${response.statusText}`);
    }

    const data = await response.json();
    if (!data.success) {
      throw new Error(data.error || 'Failed to generate voice direction');
    }

    return data.directions;
  }

  /**
   * Get sound effect suggestions
   */
  async suggestSoundEffects(
    request: SoundEffectRequest
  ): Promise<{ soundEffects: SoundEffect[]; totalEffects: number }> {
    const response = await fetch(`${API_BASE}/sound-effects`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error(`Failed to suggest sound effects: ${response.statusText}`);
    }

    const data = await response.json();
    if (!data.success) {
      throw new Error(data.error || 'Failed to suggest sound effects');
    }

    return {
      soundEffects: data.soundEffects,
      totalEffects: data.totalEffects,
    };
  }

  /**
   * Get audio mixing suggestions
   */
  async getMixingSuggestions(request: MixingSuggestionsRequest): Promise<{
    mixing: AudioMixing;
    isValid: boolean;
    validationIssues: string[];
    frequencyConflicts: string[];
    stereoPlacement: Record<string, string>;
    ffmpegFilter: string;
  }> {
    const response = await fetch(`${API_BASE}/mixing-suggestions`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error(`Failed to get mixing suggestions: ${response.statusText}`);
    }

    const data = await response.json();
    if (!data.success) {
      throw new Error(data.error || 'Failed to get mixing suggestions');
    }

    return {
      mixing: data.mixing,
      isValid: data.isValid,
      validationIssues: data.validationIssues,
      frequencyConflicts: data.frequencyConflicts,
      stereoPlacement: data.stereoPlacement,
      ffmpegFilter: data.ffmpegFilter,
    };
  }

  /**
   * Generate AI music generation prompts
   */
  async generateMusicPrompt(
    request: MusicPromptRequest
  ): Promise<{ prompt: MusicPrompt; textPrompt: string }> {
    const response = await fetch(`${API_BASE}/music-prompts`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error(`Failed to generate music prompt: ${response.statusText}`);
    }

    const data = await response.json();
    if (!data.success) {
      throw new Error(data.error || 'Failed to generate music prompt');
    }

    return {
      prompt: data.prompt,
      textPrompt: data.textPrompt,
    };
  }

  /**
   * Analyze audio-visual synchronization
   */
  async analyzeSynchronization(request: SyncAnalysisRequest): Promise<SyncAnalysis> {
    const response = await fetch(`${API_BASE}/sync-analysis`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error(`Failed to analyze synchronization: ${response.statusText}`);
    }

    const data = await response.json();
    if (!data.success) {
      throw new Error(data.error || 'Failed to analyze synchronization');
    }

    return data.analysis;
  }

  /**
   * Check audio continuity across segments
   */
  async checkContinuity(request: ContinuityCheckRequest): Promise<AudioContinuity> {
    const response = await fetch(`${API_BASE}/continuity-check`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error(`Failed to check continuity: ${response.statusText}`);
    }

    const data = await response.json();
    if (!data.success) {
      throw new Error(data.error || 'Failed to check continuity');
    }

    return data.continuity;
  }

  /**
   * Get music library
   */
  async getMusicLibrary(params?: MusicSearchParams): Promise<{
    tracks: MusicTrack[];
    totalCount: number;
  }> {
    const queryParams = new URLSearchParams();

    if (params) {
      if (params.mood) queryParams.append('mood', params.mood);
      if (params.genre) queryParams.append('genre', params.genre);
      if (params.energy) queryParams.append('energy', params.energy);
      if (params.minBPM) queryParams.append('minBPM', params.minBPM.toString());
      if (params.maxBPM) queryParams.append('maxBPM', params.maxBPM.toString());
      if (params.minDuration) queryParams.append('minDuration', params.minDuration);
      if (params.maxDuration) queryParams.append('maxDuration', params.maxDuration);
      if (params.searchQuery) queryParams.append('searchQuery', params.searchQuery);
      if (params.limit) queryParams.append('limit', params.limit.toString());
    }

    const url = `${API_BASE}/music-library${queryParams.toString() ? `?${queryParams}` : ''}`;
    const response = await fetch(url);

    if (!response.ok) {
      throw new Error(`Failed to get music library: ${response.statusText}`);
    }

    const data = await response.json();
    if (!data.success) {
      throw new Error(data.error || 'Failed to get music library');
    }

    return {
      tracks: data.tracks,
      totalCount: data.totalCount,
    };
  }
}

// Export singleton instance
export const audioIntelligenceService = new AudioIntelligenceService();
export default audioIntelligenceService;
