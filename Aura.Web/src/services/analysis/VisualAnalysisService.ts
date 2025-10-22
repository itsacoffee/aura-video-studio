/**
 * Visual Analysis Service
 * Frontend service for AI-powered visual aesthetics enhancement
 */

import { apiUrl } from '../../config/api';

const API_BASE = `${apiUrl}/aesthetics`;

// Enums
export enum ColorMood {
  Cinematic = 'Cinematic',
  Vibrant = 'Vibrant',
  Warm = 'Warm',
  Cool = 'Cool',
  Dramatic = 'Dramatic',
  Natural = 'Natural',
  Vintage = 'Vintage',
  HighContrast = 'HighContrast',
  LowKey = 'LowKey',
  HighKey = 'HighKey',
}

export enum TimeOfDay {
  Dawn = 'Dawn',
  Morning = 'Morning',
  Midday = 'Midday',
  Afternoon = 'Afternoon',
  Sunset = 'Sunset',
  Dusk = 'Dusk',
  Night = 'Night',
  Unknown = 'Unknown',
}

export enum QualityLevel {
  Excellent = 'Excellent',
  Good = 'Good',
  Acceptable = 'Acceptable',
  Poor = 'Poor',
  Unacceptable = 'Unacceptable',
}

export enum CompositionRule {
  RuleOfThirds = 'RuleOfThirds',
  GoldenRatio = 'GoldenRatio',
  CenterComposition = 'CenterComposition',
  SymmetricalBalance = 'SymmetricalBalance',
  LeadingLines = 'LeadingLines',
  FramingElements = 'FramingElements',
  NegativeSpace = 'NegativeSpace',
}

// Types
export interface ColorGradingProfile {
  name: string;
  mood: ColorMood;
  colorAdjustments: Record<string, number>;
  saturation: number;
  contrast: number;
  brightness: number;
  temperature: number;
  tint: number;
}

export interface Point {
  x: number;
  y: number;
}

export interface Rectangle {
  x: number;
  y: number;
  width: number;
  height: number;
}

export interface CompositionAnalysisResult {
  suggestedRule: CompositionRule;
  compositionScore: number;
  recommendations: string[];
  focalPoint?: Point;
  suggestedCrop?: Rectangle;
  balanceScore: number;
}

export interface QualityMetrics {
  resolution: number;
  sharpness: number;
  noiseLevel: number;
  compressionQuality: number;
  colorAccuracy: number;
  overallQuality: QualityLevel;
  issues: string[];
}

export interface VisualCoherenceReport {
  styleConsistencyScore: number;
  colorConsistencyScore: number;
  lightingConsistencyScore: number;
  overallCoherenceScore: number;
  inconsistencies: string[];
  recommendations: string[];
}

export interface SceneVisualContext {
  sceneIndex: number;
  timeOfDay: TimeOfDay;
  dominantMood: ColorMood;
  tags: string[];
  colorHistogram: Record<string, number>;
}

export interface MotionEffect {
  name: string;
  description: string;
  duration: number;
  style: string;
  parameters: Record<string, number>;
}

export interface LowerThird {
  text: string;
  subText: string;
  displayDuration: number;
  animationIn: string;
  animationOut: string;
  position: string;
}

export interface KenBurnsEffect {
  startZoom: number;
  endZoom: number;
  startX: number;
  startY: number;
  endX: number;
  endY: number;
  duration: number;
  style: string;
}

/**
 * Visual Analysis Service Class
 */
class VisualAnalysisService {
  /**
   * Analyzes and suggests color grading based on content
   */
  async analyzeColorGrading(
    contentType: string,
    sentiment: string,
    timeOfDay: TimeOfDay
  ): Promise<ColorGradingProfile> {
    const response = await fetch(`${API_BASE}/color-grading/analyze`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ contentType, sentiment, timeOfDay }),
    });

    if (!response.ok) {
      throw new Error('Failed to analyze color grading');
    }

    return response.json();
  }

  /**
   * Enforces color consistency across scenes
   */
  async enforceColorConsistency(scenes: SceneVisualContext[]): Promise<ColorGradingProfile[]> {
    const response = await fetch(`${API_BASE}/color-grading/consistency`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(scenes),
    });

    if (!response.ok) {
      throw new Error('Failed to enforce color consistency');
    }

    return response.json();
  }

  /**
   * Detects time of day from visual content
   */
  async detectTimeOfDay(colorHistogram: Record<string, number>): Promise<TimeOfDay> {
    const response = await fetch(`${API_BASE}/color-grading/detect-time`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(colorHistogram),
    });

    if (!response.ok) {
      throw new Error('Failed to detect time of day');
    }

    return response.json();
  }

  /**
   * Analyzes image composition
   */
  async analyzeComposition(
    imageWidth: number,
    imageHeight: number,
    subjectPosition?: Point
  ): Promise<CompositionAnalysisResult> {
    const response = await fetch(`${API_BASE}/composition/analyze`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ imageWidth, imageHeight, subjectPosition }),
    });

    if (!response.ok) {
      throw new Error('Failed to analyze composition');
    }

    return response.json();
  }

  /**
   * Detects focal point in image
   */
  async detectFocalPoint(width: number, height: number): Promise<Point> {
    const response = await fetch(`${API_BASE}/composition/focal-point`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ width, height }),
    });

    if (!response.ok) {
      throw new Error('Failed to detect focal point');
    }

    return response.json();
  }

  /**
   * Suggests optimal reframing
   */
  async suggestReframing(
    focalPoint: Point,
    imageWidth: number,
    imageHeight: number,
    rule: CompositionRule
  ): Promise<Rectangle> {
    const response = await fetch(`${API_BASE}/composition/reframe`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ focalPoint, imageWidth, imageHeight, rule }),
    });

    if (!response.ok) {
      throw new Error('Failed to suggest reframing');
    }

    return response.json();
  }

  /**
   * Analyzes visual coherence across scenes
   */
  async analyzeCoherence(scenes: SceneVisualContext[]): Promise<VisualCoherenceReport> {
    const response = await fetch(`${API_BASE}/coherence/analyze`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(scenes),
    });

    if (!response.ok) {
      throw new Error('Failed to analyze coherence');
    }

    return response.json();
  }

  /**
   * Analyzes lighting consistency
   */
  async analyzeLightingConsistency(scenes: SceneVisualContext[]): Promise<number> {
    const response = await fetch(`${API_BASE}/coherence/lighting`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(scenes),
    });

    if (!response.ok) {
      throw new Error('Failed to analyze lighting consistency');
    }

    return response.json();
  }

  /**
   * Detects visual theme
   */
  async detectVisualTheme(scenes: SceneVisualContext[]): Promise<string> {
    const response = await fetch(`${API_BASE}/coherence/theme`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(scenes),
    });

    if (!response.ok) {
      throw new Error('Failed to detect visual theme');
    }

    return response.json();
  }

  /**
   * Assesses technical quality
   */
  async assessQuality(
    resolutionWidth: number,
    resolutionHeight: number,
    sharpness?: number,
    noiseLevel?: number,
    compressionQuality?: number
  ): Promise<QualityMetrics> {
    const response = await fetch(`${API_BASE}/quality/assess`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        resolutionWidth,
        resolutionHeight,
        sharpness,
        noiseLevel,
        compressionQuality,
      }),
    });

    if (!response.ok) {
      throw new Error('Failed to assess quality');
    }

    return response.json();
  }

  /**
   * Calculates perceptual quality score
   */
  async calculatePerceptualQuality(metrics: QualityMetrics): Promise<number> {
    const response = await fetch(`${API_BASE}/quality/perceptual`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(metrics),
    });

    if (!response.ok) {
      throw new Error('Failed to calculate perceptual quality');
    }

    return response.json();
  }

  /**
   * Suggests quality enhancements
   */
  async suggestEnhancements(metrics: QualityMetrics): Promise<Record<string, number>> {
    const response = await fetch(`${API_BASE}/quality/enhance`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(metrics),
    });

    if (!response.ok) {
      throw new Error('Failed to suggest enhancements');
    }

    return response.json();
  }

  /**
   * Gets content-based transition effect
   */
  async getTransition(
    contentType: string,
    fromScene: string,
    toScene: string
  ): Promise<MotionEffect> {
    const response = await fetch(`${API_BASE}/motion/transition`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ contentType, fromScene, toScene }),
    });

    if (!response.ok) {
      throw new Error('Failed to get transition');
    }

    return response.json();
  }

  /**
   * Creates animated lower third
   */
  async createLowerThird(text: string, subText: string, style: string): Promise<LowerThird> {
    const response = await fetch(`${API_BASE}/motion/lower-third`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ text, subText, style }),
    });

    if (!response.ok) {
      throw new Error('Failed to create lower third');
    }

    return response.json();
  }

  /**
   * Applies Ken Burns effect
   */
  async applyKenBurnsEffect(
    imageWidth: number,
    imageHeight: number,
    duration: number,
    focusPoint: string
  ): Promise<KenBurnsEffect> {
    const response = await fetch(`${API_BASE}/motion/ken-burns`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ imageWidth, imageHeight, duration, focusPoint }),
    });

    if (!response.ok) {
      throw new Error('Failed to apply Ken Burns effect');
    }

    return response.json();
  }

  /**
   * Gets motion design presets
   */
  async getMotionPresets(): Promise<MotionEffect[]> {
    const response = await fetch(`${API_BASE}/motion/presets`);

    if (!response.ok) {
      throw new Error('Failed to get motion presets');
    }

    return response.json();
  }
}

export const visualAnalysisService = new VisualAnalysisService();
