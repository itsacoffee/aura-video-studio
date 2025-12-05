/**
 * OpenCut Captions Store
 *
 * Manages captions and subtitles for the OpenCut video editor.
 * Provides caption tracks, individual captions with timing,
 * styling options, and import/export capabilities.
 */

import { create } from 'zustand';

/**
 * Text alignment options for captions
 */
export type CaptionAlignment = 'left' | 'center' | 'right';

/**
 * Vertical position for caption display
 */
export type CaptionPosition = 'top' | 'middle' | 'bottom';

/**
 * Style settings for caption appearance
 */
export interface CaptionStyle {
  /** Font family */
  fontFamily: string;
  /** Font size in pixels */
  fontSize: number;
  /** Font weight (normal, bold, etc.) */
  fontWeight: number;
  /** Text color (hex format) */
  color: string;
  /** Background color (hex format with alpha) */
  backgroundColor: string;
  /** Background opacity (0-1) */
  backgroundOpacity: number;
  /** Text stroke/outline color */
  strokeColor: string;
  /** Text stroke width in pixels */
  strokeWidth: number;
  /** Text alignment */
  textAlign: CaptionAlignment;
  /** Vertical position */
  position: CaptionPosition;
  /** Padding around text in pixels */
  padding: number;
  /** Border radius for background */
  borderRadius: number;
}

/**
 * Individual caption entry with timing and text
 */
export interface Caption {
  /** Unique identifier */
  id: string;
  /** Caption track this belongs to */
  trackId: string;
  /** Start time in seconds */
  startTime: number;
  /** End time in seconds */
  endTime: number;
  /** Caption text content */
  text: string;
  /** Optional speaker identifier */
  speaker?: string;
  /** Override styles for this caption */
  styleOverrides?: Partial<CaptionStyle>;
}

/**
 * Caption track containing multiple captions
 */
export interface CaptionTrack {
  /** Unique identifier */
  id: string;
  /** Track name (e.g., "English", "Spanish") */
  name: string;
  /** Language code (e.g., "en", "es") */
  language: string;
  /** Whether this track is visible in preview */
  visible: boolean;
  /** Whether this track is locked from editing */
  locked: boolean;
  /** Default style for captions in this track */
  style: CaptionStyle;
}

/**
 * State for the captions store
 */
interface CaptionsState {
  /** All caption tracks */
  tracks: CaptionTrack[];
  /** All caption entries */
  captions: Caption[];
  /** Currently selected caption track ID */
  selectedTrackId: string | null;
  /** Currently selected caption ID */
  selectedCaptionId: string | null;
  /** Active track ID for display */
  activeTrackId: string | null;
}

/**
 * Generate unique ID for captions and tracks
 */
function generateId(): string {
  return `caption-${Date.now()}-${Math.random().toString(36).substring(2, 9)}`;
}

/**
 * Default caption style settings
 */
export const DEFAULT_CAPTION_STYLE: CaptionStyle = {
  fontFamily: 'Inter, system-ui, sans-serif',
  fontSize: 24,
  fontWeight: 500,
  color: '#FFFFFF',
  backgroundColor: '#000000',
  backgroundOpacity: 0.75,
  strokeColor: '#000000',
  strokeWidth: 0,
  textAlign: 'center',
  position: 'bottom',
  padding: 8,
  borderRadius: 4,
};

/**
 * Actions for the captions store
 */
interface CaptionsActions {
  // Track management
  addTrack: (name: string, language: string, style?: Partial<CaptionStyle>) => string;
  updateTrack: (trackId: string, updates: Partial<Omit<CaptionTrack, 'id'>>) => void;
  removeTrack: (trackId: string) => void;
  selectTrack: (trackId: string | null) => void;
  setActiveTrack: (trackId: string | null) => void;

  // Caption management
  addCaption: (
    trackId: string,
    startTime: number,
    endTime: number,
    text: string,
    speaker?: string
  ) => string;
  updateCaption: (captionId: string, updates: Partial<Omit<Caption, 'id' | 'trackId'>>) => void;
  removeCaption: (captionId: string) => void;
  selectCaption: (captionId: string | null) => void;

  // Batch operations
  addCaptions: (
    trackId: string,
    captions: Array<{ startTime: number; endTime: number; text: string; speaker?: string }>
  ) => void;
  clearTrackCaptions: (trackId: string) => void;

  // Queries
  getCaptionsForTrack: (trackId: string) => Caption[];
  getCaptionAtTime: (trackId: string, time: number) => Caption | null;
  getVisibleCaptions: (time: number) => Caption[];

  // Import/Export
  importSrt: (trackId: string, srtContent: string) => void;
  importVtt: (trackId: string, vttContent: string) => void;
  exportSrt: (trackId: string) => string;
  exportVtt: (trackId: string) => string;

  // Reset
  reset: () => void;
}

/**
 * Parse SRT time format (HH:MM:SS,mmm) to seconds
 */
function parseSrtTime(timeStr: string): number {
  // Pattern: 00:00:00,000 - anchored, simple digits and colons
  const match = timeStr.match(/^(\d{2}):(\d{2}):(\d{2})[,.](\d{3})$/);
  if (!match) return 0;

  const hours = parseInt(match[1], 10);
  const minutes = parseInt(match[2], 10);
  const seconds = parseInt(match[3], 10);
  const ms = parseInt(match[4], 10);

  return hours * 3600 + minutes * 60 + seconds + ms / 1000;
}

/**
 * Format seconds to SRT time format (HH:MM:SS,mmm)
 */
function formatSrtTime(seconds: number): string {
  const hours = Math.floor(seconds / 3600);
  const minutes = Math.floor((seconds % 3600) / 60);
  const secs = Math.floor(seconds % 60);
  const ms = Math.floor((seconds % 1) * 1000);

  return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')},${ms.toString().padStart(3, '0')}`;
}

/**
 * Format seconds to VTT time format (HH:MM:SS.mmm)
 */
function formatVttTime(seconds: number): string {
  const hours = Math.floor(seconds / 3600);
  const minutes = Math.floor((seconds % 3600) / 60);
  const secs = Math.floor(seconds % 60);
  const ms = Math.floor((seconds % 1) * 1000);

  return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}.${ms.toString().padStart(3, '0')}`;
}

export type OpenCutCaptionsStore = CaptionsState & CaptionsActions;

/**
 * OpenCut captions store for managing subtitles and captions
 */
export const useOpenCutCaptionsStore = create<OpenCutCaptionsStore>((set, get) => ({
  tracks: [],
  captions: [],
  selectedTrackId: null,
  selectedCaptionId: null,
  activeTrackId: null,

  // Track management
  addTrack: (name, language, style = {}) => {
    const id = generateId();
    const newTrack: CaptionTrack = {
      id,
      name,
      language,
      visible: true,
      locked: false,
      style: { ...DEFAULT_CAPTION_STYLE, ...style },
    };

    set((state) => ({
      tracks: [...state.tracks, newTrack],
      activeTrackId: state.activeTrackId ?? id,
      selectedTrackId: id,
    }));

    return id;
  },

  updateTrack: (trackId, updates) => {
    set((state) => ({
      tracks: state.tracks.map((track) =>
        track.id === trackId ? { ...track, ...updates } : track
      ),
    }));
  },

  removeTrack: (trackId) => {
    set((state) => ({
      tracks: state.tracks.filter((t) => t.id !== trackId),
      captions: state.captions.filter((c) => c.trackId !== trackId),
      selectedTrackId: state.selectedTrackId === trackId ? null : state.selectedTrackId,
      activeTrackId: state.activeTrackId === trackId ? null : state.activeTrackId,
    }));
  },

  selectTrack: (trackId) => {
    set({ selectedTrackId: trackId, selectedCaptionId: null });
  },

  setActiveTrack: (trackId) => {
    set({ activeTrackId: trackId });
  },

  // Caption management
  addCaption: (trackId, startTime, endTime, text, speaker) => {
    const id = generateId();
    const newCaption: Caption = {
      id,
      trackId,
      startTime,
      endTime,
      text,
      speaker,
    };

    set((state) => ({
      captions: [...state.captions, newCaption].sort((a, b) => a.startTime - b.startTime),
    }));

    return id;
  },

  updateCaption: (captionId, updates) => {
    set((state) => ({
      captions: state.captions
        .map((caption) => (caption.id === captionId ? { ...caption, ...updates } : caption))
        .sort((a, b) => a.startTime - b.startTime),
    }));
  },

  removeCaption: (captionId) => {
    set((state) => ({
      captions: state.captions.filter((c) => c.id !== captionId),
      selectedCaptionId: state.selectedCaptionId === captionId ? null : state.selectedCaptionId,
    }));
  },

  selectCaption: (captionId) => {
    set({ selectedCaptionId: captionId });
  },

  // Batch operations
  addCaptions: (trackId, captionsData) => {
    const newCaptions: Caption[] = captionsData.map((data) => ({
      id: generateId(),
      trackId,
      startTime: data.startTime,
      endTime: data.endTime,
      text: data.text,
      speaker: data.speaker,
    }));

    set((state) => ({
      captions: [...state.captions, ...newCaptions].sort((a, b) => a.startTime - b.startTime),
    }));
  },

  clearTrackCaptions: (trackId) => {
    set((state) => ({
      captions: state.captions.filter((c) => c.trackId !== trackId),
      selectedCaptionId: null,
    }));
  },

  // Queries
  getCaptionsForTrack: (trackId) => {
    return get().captions.filter((c) => c.trackId === trackId);
  },

  getCaptionAtTime: (trackId, time) => {
    const captions = get().captions.filter((c) => c.trackId === trackId);
    return captions.find((c) => time >= c.startTime && time <= c.endTime) ?? null;
  },

  getVisibleCaptions: (time) => {
    const { tracks, captions } = get();
    const visibleTrackIds = new Set(tracks.filter((t) => t.visible).map((t) => t.id));

    return captions.filter(
      (c) => visibleTrackIds.has(c.trackId) && time >= c.startTime && time <= c.endTime
    );
  },

  // Import SRT format
  importSrt: (trackId, srtContent) => {
    const lines = srtContent.trim().split('\n');
    const newCaptions: Array<{
      startTime: number;
      endTime: number;
      text: string;
    }> = [];

    let i = 0;
    while (i < lines.length) {
      // Skip index line (number only)
      if (/^\d+$/.test(lines[i]?.trim() ?? '')) {
        i++;
      }

      // Parse time line (anchored pattern, safe)
      const timeMatch = lines[i]?.match(
        /^(\d{2}:\d{2}:\d{2}[,.]\d{3})\s*-->\s*(\d{2}:\d{2}:\d{2}[,.]\d{3})$/
      );
      if (timeMatch) {
        const startTime = parseSrtTime(timeMatch[1]);
        const endTime = parseSrtTime(timeMatch[2]);
        i++;

        // Collect text lines until empty line or next index
        const textLines: string[] = [];
        while (
          i < lines.length &&
          lines[i]?.trim() !== '' &&
          !/^\d+$/.test(lines[i]?.trim() ?? '')
        ) {
          textLines.push(lines[i].trim());
          i++;
        }

        if (textLines.length > 0) {
          newCaptions.push({
            startTime,
            endTime,
            text: textLines.join('\n'),
          });
        }
      } else {
        i++;
      }
    }

    get().addCaptions(trackId, newCaptions);
  },

  // Import VTT format
  importVtt: (trackId, vttContent) => {
    const lines = vttContent.trim().split('\n');
    const newCaptions: Array<{
      startTime: number;
      endTime: number;
      text: string;
    }> = [];

    let i = 0;

    // Skip WEBVTT header
    if (lines[0]?.trim().toUpperCase() === 'WEBVTT') {
      i = 1;
    }

    while (i < lines.length) {
      // Skip empty lines and comments
      if (lines[i]?.trim() === '' || lines[i]?.trim().startsWith('NOTE')) {
        i++;
        continue;
      }

      // Parse time line (anchored pattern, safe)
      const timeMatch = lines[i]?.match(
        /^(\d{2}:\d{2}:\d{2}[,.]\d{3})\s*-->\s*(\d{2}:\d{2}:\d{2}[,.]\d{3})/
      );
      if (timeMatch) {
        const startTime = parseSrtTime(timeMatch[1]);
        const endTime = parseSrtTime(timeMatch[2]);
        i++;

        // Collect text lines until empty line
        const textLines: string[] = [];
        while (i < lines.length && lines[i]?.trim() !== '') {
          textLines.push(lines[i].trim());
          i++;
        }

        if (textLines.length > 0) {
          newCaptions.push({
            startTime,
            endTime,
            text: textLines.join('\n'),
          });
        }
      } else {
        i++;
      }
    }

    get().addCaptions(trackId, newCaptions);
  },

  // Export to SRT format
  exportSrt: (trackId) => {
    const captions = get().getCaptionsForTrack(trackId);

    return captions
      .map((caption, index) => {
        const start = formatSrtTime(caption.startTime);
        const end = formatSrtTime(caption.endTime);
        return `${index + 1}\n${start} --> ${end}\n${caption.text}\n`;
      })
      .join('\n');
  },

  // Export to VTT format
  exportVtt: (trackId) => {
    const captions = get().getCaptionsForTrack(trackId);
    const header = 'WEBVTT\n\n';

    const body = captions
      .map((caption) => {
        const start = formatVttTime(caption.startTime);
        const end = formatVttTime(caption.endTime);
        return `${start} --> ${end}\n${caption.text}\n`;
      })
      .join('\n');

    return header + body;
  },

  // Reset store
  reset: () => {
    set({
      tracks: [],
      captions: [],
      selectedTrackId: null,
      selectedCaptionId: null,
      activeTrackId: null,
    });
  },
}));
