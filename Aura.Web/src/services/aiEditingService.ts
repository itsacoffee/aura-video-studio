/**
 * AI Editing Service
 * Provides AI-powered auto-editing features including scene detection,
 * highlight detection, beat sync, auto-framing, and auto-captions
 */

const API_BASE = '/api/ai-editing';

// Scene Detection
export interface SceneChange {
  timestamp: string;
  frameIndex: number;
  confidence: number;
  changeType: string;
  description: string;
}

export interface SceneDetectionResult {
  scenes: SceneChange[];
  totalDuration: string;
  totalFramesAnalyzed: number;
  summary: string;
}

export interface DetectScenesRequest {
  videoPath: string;
  threshold?: number;
}

// Highlight Detection
export interface HighlightMoment {
  startTime: string;
  endTime: string;
  score: number;
  type: string;
  reasoning: string;
  features: string[];
}

export interface HighlightDetectionResult {
  highlights: HighlightMoment[];
  totalDuration: string;
  averageEngagement: number;
  summary: string;
}

export interface DetectHighlightsRequest {
  videoPath: string;
  maxHighlights?: number;
}

// Beat Detection
export interface BeatPoint {
  timestamp: string;
  strength: number;
  tempo: number;
  isDownbeat: boolean;
}

export interface BeatDetectionResult {
  beats: BeatPoint[];
  averageTempo: number;
  duration: string;
  totalBeats: number;
  summary: string;
}

export interface DetectBeatsRequest {
  filePath: string;
}

export interface BeatCutsRequest {
  filePath: string;
  cutEveryNBeats?: number;
}

// Auto Framing
export interface FramingSuggestion {
  startTime: string;
  duration: string;
  targetWidth: number;
  targetHeight: number;
  cropX: number;
  cropY: number;
  cropWidth: number;
  cropHeight: number;
  confidence: number;
  reasoning: string;
}

export interface AutoFramingResult {
  suggestions: FramingSuggestion[];
  sourceWidth: number;
  sourceHeight: number;
  summary: string;
}

export interface AutoFrameRequest {
  videoPath: string;
  targetWidth: number;
  targetHeight: number;
}

export interface ConvertFormatRequest {
  videoPath: string;
}

// Speech Recognition / Captions
export interface Caption {
  startTime: string;
  endTime: string;
  text: string;
  confidence: number;
}

export interface SpeechRecognitionResult {
  captions: Caption[];
  duration: string;
  language: string;
  averageConfidence: number;
  summary: string;
}

export interface GenerateCaptionsRequest {
  filePath: string;
  language?: string;
}

export interface ExportCaptionsRequest {
  filePath: string;
  outputPath: string;
}

class AIEditingService {
  /**
   * Detect scene changes in video
   */
  async detectScenes(request: DetectScenesRequest): Promise<SceneDetectionResult> {
    const response = await fetch(`${API_BASE}/detect-scenes`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error(`Failed to detect scenes: ${response.statusText}`);
    }

    const data = await response.json();
    return data.result;
  }

  /**
   * Generate chapter markers from detected scenes
   */
  async generateChapters(videoPath: string): Promise<Array<{ timestamp: string; title: string }>> {
    const response = await fetch(`${API_BASE}/generate-chapters`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ videoPath }),
    });

    if (!response.ok) {
      throw new Error(`Failed to generate chapters: ${response.statusText}`);
    }

    const data = await response.json();
    return data.chapters;
  }

  /**
   * Detect highlight moments in video
   */
  async detectHighlights(request: DetectHighlightsRequest): Promise<HighlightDetectionResult> {
    const response = await fetch(`${API_BASE}/detect-highlights`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error(`Failed to detect highlights: ${response.statusText}`);
    }

    const data = await response.json();
    return data.result;
  }

  /**
   * Detect beats in audio for music synchronization
   */
  async detectBeats(request: DetectBeatsRequest): Promise<BeatDetectionResult> {
    const response = await fetch(`${API_BASE}/detect-beats`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error(`Failed to detect beats: ${response.statusText}`);
    }

    const data = await response.json();
    return data.result;
  }

  /**
   * Generate beat-aligned cut points
   */
  async generateBeatCuts(request: BeatCutsRequest): Promise<string[]> {
    const response = await fetch(`${API_BASE}/beat-cuts`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error(`Failed to generate beat cuts: ${response.statusText}`);
    }

    const data = await response.json();
    return data.cuts;
  }

  /**
   * Analyze video for auto-framing suggestions
   */
  async analyzeAutoFraming(request: AutoFrameRequest): Promise<AutoFramingResult> {
    const response = await fetch(`${API_BASE}/auto-frame`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error(`Failed to analyze auto-framing: ${response.statusText}`);
    }

    const data = await response.json();
    return data.result;
  }

  /**
   * Convert video to vertical format (9:16)
   */
  async convertToVertical(videoPath: string): Promise<AutoFramingResult> {
    const response = await fetch(`${API_BASE}/convert-vertical`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ videoPath }),
    });

    if (!response.ok) {
      throw new Error(`Failed to convert to vertical: ${response.statusText}`);
    }

    const data = await response.json();
    return data.result;
  }

  /**
   * Convert video to square format (1:1)
   */
  async convertToSquare(videoPath: string): Promise<AutoFramingResult> {
    const response = await fetch(`${API_BASE}/convert-square`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ videoPath }),
    });

    if (!response.ok) {
      throw new Error(`Failed to convert to square: ${response.statusText}`);
    }

    const data = await response.json();
    return data.result;
  }

  /**
   * Generate captions from video/audio
   */
  async generateCaptions(request: GenerateCaptionsRequest): Promise<SpeechRecognitionResult> {
    const response = await fetch(`${API_BASE}/generate-captions`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error(`Failed to generate captions: ${response.statusText}`);
    }

    const data = await response.json();
    return data.result;
  }

  /**
   * Export captions to SRT format
   */
  async exportToSrt(request: ExportCaptionsRequest): Promise<string> {
    const response = await fetch(`${API_BASE}/export-srt`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error(`Failed to export SRT: ${response.statusText}`);
    }

    const data = await response.json();
    return data.outputPath;
  }

  /**
   * Export captions to VTT format
   */
  async exportToVtt(request: ExportCaptionsRequest): Promise<string> {
    const response = await fetch(`${API_BASE}/export-vtt`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error(`Failed to export VTT: ${response.statusText}`);
    }

    const data = await response.json();
    return data.outputPath;
  }
}

// Export singleton instance
export const aiEditingService = new AIEditingService();
