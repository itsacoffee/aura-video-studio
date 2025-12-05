/**
 * OpenCut Captions Store
 *
 * Manages caption tracks and entries for video projects.
 * Supports multiple caption tracks, styling, animations, and import/export.
 */

import { create } from 'zustand';
import type {
  Caption,
  CaptionTrack,
  CaptionStyle,
  CaptionAnimation,
  VerticalPosition,
} from '../types/opencut';

function generateId(): string {
  return `caption-${Date.now()}-${Math.random().toString(36).substring(2, 9)}`;
}

function generateTrackId(): string {
  return `track-${Date.now()}-${Math.random().toString(36).substring(2, 9)}`;
}

/**
 * Default caption style
 */
export const DEFAULT_CAPTION_STYLE: CaptionStyle = {
  fontFamily: 'Arial',
  fontSize: 24,
  fontWeight: 400,
  color: '#FFFFFF',
  textAlign: 'center',
  backgroundColor: 'rgba(0, 0, 0, 0.75)',
  backgroundPadding: 8,
  strokeColor: '#000000',
  strokeWidth: 2,
};

interface OpenCutCaptionsState {
  /** All caption tracks */
  tracks: CaptionTrack[];
  /** Currently selected track ID */
  selectedTrackId: string | null;
  /** Currently selected caption ID */
  selectedCaptionId: string | null;
  /** Track being edited (for style panel) */
  editingTrackId: string | null;
  /** Caption being edited inline */
  editingCaptionId: string | null;
}

interface OpenCutCaptionsActions {
  // Track CRUD
  addTrack: (name: string, language?: string) => string;
  removeTrack: (trackId: string) => void;
  updateTrack: (trackId: string, updates: Partial<Omit<CaptionTrack, 'id' | 'captions'>>) => void;
  duplicateTrack: (trackId: string) => string | null;

  // Track visibility and locking
  toggleTrackVisibility: (trackId: string) => void;
  toggleTrackLock: (trackId: string) => void;
  setTrackPosition: (trackId: string, position: VerticalPosition) => void;

  // Caption CRUD
  addCaption: (trackId: string, startTime: number, endTime: number, text: string) => string | null;
  removeCaption: (trackId: string, captionId: string) => void;
  updateCaption: (trackId: string, captionId: string, updates: Partial<Caption>) => void;
  moveCaption: (trackId: string, captionId: string, newStartTime: number) => void;
  resizeCaption: (
    trackId: string,
    captionId: string,
    newStartTime: number,
    newEndTime: number
  ) => void;

  // Selection
  selectTrack: (trackId: string | null) => void;
  selectCaption: (captionId: string | null) => void;
  setEditingTrack: (trackId: string | null) => void;
  setEditingCaption: (captionId: string | null) => void;

  // Styling
  updateTrackStyle: (trackId: string, style: Partial<CaptionStyle>) => void;
  updateCaptionStyle: (trackId: string, captionId: string, style: Partial<CaptionStyle>) => void;
  resetCaptionStyle: (trackId: string, captionId: string) => void;

  // Animation
  setCaptionAnimation: (
    trackId: string,
    captionId: string,
    type: 'enter' | 'exit',
    animation: CaptionAnimation | undefined
  ) => void;

  // Navigation
  goToNextCaption: (currentTime: number) => Caption | null;
  goToPreviousCaption: (currentTime: number) => Caption | null;
  getCaptionAtTime: (time: number) => { track: CaptionTrack; caption: Caption } | null;

  // Bulk operations
  deleteAllCaptions: (trackId: string) => void;
  splitCaption: (trackId: string, captionId: string, splitTime: number) => string | null;
  mergeCaptions: (trackId: string, captionId1: string, captionId2: string) => void;

  // Import/Export
  importSRT: (trackId: string, srtContent: string) => number;
  importVTT: (trackId: string, vttContent: string) => number;
  exportToSRT: (trackId: string) => string;
  exportToVTT: (trackId: string) => string;

  // Getters
  getTrackById: (trackId: string) => CaptionTrack | undefined;
  getCaptionById: (trackId: string, captionId: string) => Caption | undefined;
  getCaptionsInRange: (trackId: string, startTime: number, endTime: number) => Caption[];
  getVisibleTracks: () => CaptionTrack[];
  getActiveCaption: (time: number) => { track: CaptionTrack; caption: Caption } | null;
}

export type OpenCutCaptionsStore = OpenCutCaptionsState & OpenCutCaptionsActions;

/**
 * Parse SRT timecode (HH:MM:SS,mmm) to seconds
 */
function parseSRTTimecode(timecode: string): number {
  const match = timecode.match(/(\d{2}):(\d{2}):(\d{2}),(\d{3})/);
  if (!match) return 0;
  const [, hours, minutes, seconds, millis] = match;
  return (
    parseInt(hours, 10) * 3600 +
    parseInt(minutes, 10) * 60 +
    parseInt(seconds, 10) +
    parseInt(millis, 10) / 1000
  );
}

/**
 * Parse VTT timecode (HH:MM:SS.mmm) to seconds
 */
function parseVTTTimecode(timecode: string): number {
  const match = timecode.match(/(\d{2}):(\d{2}):(\d{2})\.(\d{3})/);
  if (!match) return 0;
  const [, hours, minutes, seconds, millis] = match;
  return (
    parseInt(hours, 10) * 3600 +
    parseInt(minutes, 10) * 60 +
    parseInt(seconds, 10) +
    parseInt(millis, 10) / 1000
  );
}

/**
 * Format seconds to SRT timecode (HH:MM:SS,mmm)
 */
function formatSRTTimecode(seconds: number): string {
  const hours = Math.floor(seconds / 3600);
  const minutes = Math.floor((seconds % 3600) / 60);
  const secs = Math.floor(seconds % 60);
  const millis = Math.floor((seconds % 1) * 1000);
  return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')},${millis.toString().padStart(3, '0')}`;
}

/**
 * Format seconds to VTT timecode (HH:MM:SS.mmm)
 */
function formatVTTTimecode(seconds: number): string {
  const hours = Math.floor(seconds / 3600);
  const minutes = Math.floor((seconds % 3600) / 60);
  const secs = Math.floor(seconds % 60);
  const millis = Math.floor((seconds % 1) * 1000);
  return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}.${millis.toString().padStart(3, '0')}`;
}

export const useOpenCutCaptionsStore = create<OpenCutCaptionsStore>((set, get) => ({
  tracks: [],
  selectedTrackId: null,
  selectedCaptionId: null,
  editingTrackId: null,
  editingCaptionId: null,

  // Track CRUD
  addTrack: (name, language = 'en-US') => {
    const id = generateTrackId();
    const track: CaptionTrack = {
      id,
      name,
      language,
      defaultStyle: { ...DEFAULT_CAPTION_STYLE },
      position: 'bottom',
      margin: 40,
      captions: [],
      visible: true,
      locked: false,
    };
    set((state) => ({
      tracks: [...state.tracks, track],
      selectedTrackId: id,
    }));
    return id;
  },

  removeTrack: (trackId) => {
    set((state) => ({
      tracks: state.tracks.filter((t) => t.id !== trackId),
      selectedTrackId: state.selectedTrackId === trackId ? null : state.selectedTrackId,
      selectedCaptionId: state.tracks
        .find((t) => t.id === trackId)
        ?.captions.some((c) => c.id === state.selectedCaptionId)
        ? null
        : state.selectedCaptionId,
    }));
  },

  updateTrack: (trackId, updates) => {
    set((state) => ({
      tracks: state.tracks.map((t) => (t.id === trackId ? { ...t, ...updates } : t)),
    }));
  },

  duplicateTrack: (trackId) => {
    const track = get().getTrackById(trackId);
    if (!track) return null;

    const newId = generateTrackId();
    const newTrack: CaptionTrack = {
      ...track,
      id: newId,
      name: `${track.name} (Copy)`,
      captions: track.captions.map((c) => ({ ...c, id: generateId() })),
    };
    set((state) => ({
      tracks: [...state.tracks, newTrack],
      selectedTrackId: newId,
    }));
    return newId;
  },

  // Track visibility and locking
  toggleTrackVisibility: (trackId) => {
    set((state) => ({
      tracks: state.tracks.map((t) => (t.id === trackId ? { ...t, visible: !t.visible } : t)),
    }));
  },

  toggleTrackLock: (trackId) => {
    set((state) => ({
      tracks: state.tracks.map((t) => (t.id === trackId ? { ...t, locked: !t.locked } : t)),
    }));
  },

  setTrackPosition: (trackId, position) => {
    get().updateTrack(trackId, { position });
  },

  // Caption CRUD
  addCaption: (trackId, startTime, endTime, text) => {
    const track = get().getTrackById(trackId);
    if (!track || track.locked) return null;

    const id = generateId();
    const caption: Caption = {
      id,
      startTime,
      endTime,
      text,
    };

    set((state) => ({
      tracks: state.tracks.map((t) =>
        t.id === trackId
          ? {
              ...t,
              captions: [...t.captions, caption].sort((a, b) => a.startTime - b.startTime),
            }
          : t
      ),
      selectedCaptionId: id,
    }));
    return id;
  },

  removeCaption: (trackId, captionId) => {
    const track = get().getTrackById(trackId);
    if (!track || track.locked) return;

    set((state) => ({
      tracks: state.tracks.map((t) =>
        t.id === trackId ? { ...t, captions: t.captions.filter((c) => c.id !== captionId) } : t
      ),
      selectedCaptionId: state.selectedCaptionId === captionId ? null : state.selectedCaptionId,
    }));
  },

  updateCaption: (trackId, captionId, updates) => {
    const track = get().getTrackById(trackId);
    if (!track || track.locked) return;

    set((state) => ({
      tracks: state.tracks.map((t) =>
        t.id === trackId
          ? {
              ...t,
              captions: t.captions
                .map((c) => (c.id === captionId ? { ...c, ...updates } : c))
                .sort((a, b) => a.startTime - b.startTime),
            }
          : t
      ),
    }));
  },

  moveCaption: (trackId, captionId, newStartTime) => {
    const track = get().getTrackById(trackId);
    const caption = track?.captions.find((c) => c.id === captionId);
    if (!caption || !track || track.locked) return;

    const duration = caption.endTime - caption.startTime;
    get().updateCaption(trackId, captionId, {
      startTime: Math.max(0, newStartTime),
      endTime: Math.max(0, newStartTime) + duration,
    });
  },

  resizeCaption: (trackId, captionId, newStartTime, newEndTime) => {
    const track = get().getTrackById(trackId);
    if (!track || track.locked) return;

    if (newStartTime >= newEndTime) return;
    get().updateCaption(trackId, captionId, {
      startTime: Math.max(0, newStartTime),
      endTime: newEndTime,
    });
  },

  // Selection
  selectTrack: (trackId) => set({ selectedTrackId: trackId }),
  selectCaption: (captionId) => set({ selectedCaptionId: captionId }),
  setEditingTrack: (trackId) => set({ editingTrackId: trackId }),
  setEditingCaption: (captionId) => set({ editingCaptionId: captionId }),

  // Styling
  updateTrackStyle: (trackId, style) => {
    const track = get().getTrackById(trackId);
    if (!track) return;

    set((state) => ({
      tracks: state.tracks.map((t) =>
        t.id === trackId
          ? {
              ...t,
              defaultStyle: { ...t.defaultStyle, ...style },
            }
          : t
      ),
    }));
  },

  updateCaptionStyle: (trackId, captionId, style) => {
    const track = get().getTrackById(trackId);
    const caption = track?.captions.find((c) => c.id === captionId);
    if (!caption || !track || track.locked) return;

    get().updateCaption(trackId, captionId, {
      style: { ...(caption.style || {}), ...style },
    });
  },

  resetCaptionStyle: (trackId, captionId) => {
    get().updateCaption(trackId, captionId, { style: undefined });
  },

  // Animation
  setCaptionAnimation: (trackId, captionId, type, animation) => {
    const track = get().getTrackById(trackId);
    if (!track || track.locked) return;

    get().updateCaption(trackId, captionId, {
      [type === 'enter' ? 'enterAnimation' : 'exitAnimation']: animation,
    });
  },

  // Navigation
  goToNextCaption: (currentTime) => {
    const visibleTracks = get().getVisibleTracks();
    let nextCaption: Caption | null = null;
    let nextTime = Infinity;

    for (const track of visibleTracks) {
      for (const caption of track.captions) {
        if (caption.startTime > currentTime + 0.01 && caption.startTime < nextTime) {
          nextTime = caption.startTime;
          nextCaption = caption;
        }
      }
    }

    if (nextCaption) {
      set({ selectedCaptionId: nextCaption.id });
    }
    return nextCaption;
  },

  goToPreviousCaption: (currentTime) => {
    const visibleTracks = get().getVisibleTracks();
    let prevCaption: Caption | null = null;
    let prevTime = -Infinity;

    for (const track of visibleTracks) {
      for (const caption of track.captions) {
        if (caption.startTime < currentTime - 0.01 && caption.startTime > prevTime) {
          prevTime = caption.startTime;
          prevCaption = caption;
        }
      }
    }

    if (prevCaption) {
      set({ selectedCaptionId: prevCaption.id });
    }
    return prevCaption;
  },

  getCaptionAtTime: (time) => {
    const visibleTracks = get().getVisibleTracks();
    for (const track of visibleTracks) {
      const caption = track.captions.find((c) => c.startTime <= time && c.endTime >= time);
      if (caption) {
        return { track, caption };
      }
    }
    return null;
  },

  // Bulk operations
  deleteAllCaptions: (trackId) => {
    const track = get().getTrackById(trackId);
    if (!track || track.locked) return;

    set((state) => ({
      tracks: state.tracks.map((t) => (t.id === trackId ? { ...t, captions: [] } : t)),
      selectedCaptionId: null,
    }));
  },

  splitCaption: (trackId, captionId, splitTime) => {
    const track = get().getTrackById(trackId);
    const caption = track?.captions.find((c) => c.id === captionId);
    if (!caption || !track || track.locked) return null;

    if (splitTime <= caption.startTime || splitTime >= caption.endTime) return null;

    const newId = generateId();
    const firstHalf: Caption = {
      ...caption,
      endTime: splitTime,
    };
    const secondHalf: Caption = {
      ...caption,
      id: newId,
      startTime: splitTime,
    };

    set((state) => ({
      tracks: state.tracks.map((t) =>
        t.id === trackId
          ? {
              ...t,
              captions: t.captions
                .map((c) => (c.id === captionId ? firstHalf : c))
                .concat(secondHalf)
                .sort((a, b) => a.startTime - b.startTime),
            }
          : t
      ),
    }));
    return newId;
  },

  mergeCaptions: (trackId, captionId1, captionId2) => {
    const track = get().getTrackById(trackId);
    if (!track || track.locked) return;

    const caption1 = track.captions.find((c) => c.id === captionId1);
    const caption2 = track.captions.find((c) => c.id === captionId2);
    if (!caption1 || !caption2) return;

    const [first, second] =
      caption1.startTime < caption2.startTime ? [caption1, caption2] : [caption2, caption1];

    const merged: Caption = {
      ...first,
      endTime: second.endTime,
      text: `${first.text} ${second.text}`,
    };

    set((state) => ({
      tracks: state.tracks.map((t) =>
        t.id === trackId
          ? {
              ...t,
              captions: t.captions
                .filter((c) => c.id !== second.id)
                .map((c) => (c.id === first.id ? merged : c)),
            }
          : t
      ),
      selectedCaptionId: first.id,
    }));
  },

  // Import/Export
  importSRT: (trackId, srtContent) => {
    const track = get().getTrackById(trackId);
    if (!track || track.locked) return 0;

    const lines = srtContent.trim().split('\n\n');
    let imported = 0;

    for (const block of lines) {
      const parts = block.split('\n');
      if (parts.length < 3) continue;

      const timecodeMatch = parts[1].match(
        /(\d{2}:\d{2}:\d{2},\d{3}) --> (\d{2}:\d{2}:\d{2},\d{3})/
      );
      if (!timecodeMatch) continue;

      const startTime = parseSRTTimecode(timecodeMatch[1]);
      const endTime = parseSRTTimecode(timecodeMatch[2]);
      const text = parts.slice(2).join('\n');

      get().addCaption(trackId, startTime, endTime, text);
      imported++;
    }

    return imported;
  },

  importVTT: (trackId, vttContent) => {
    const track = get().getTrackById(trackId);
    if (!track || track.locked) return 0;

    const lines = vttContent
      .replace(/^WEBVTT\s*\n?/, '')
      .trim()
      .split('\n\n');
    let imported = 0;

    for (const block of lines) {
      const parts = block.split('\n');
      if (parts.length < 2) continue;

      // Skip cue identifier if present
      const timecodeLineIndex = parts[0].includes('-->') ? 0 : 1;
      if (timecodeLineIndex >= parts.length) continue;

      const timecodeMatch = parts[timecodeLineIndex].match(
        /(\d{2}:\d{2}:\d{2}\.\d{3}) --> (\d{2}:\d{2}:\d{2}\.\d{3})/
      );
      if (!timecodeMatch) continue;

      const startTime = parseVTTTimecode(timecodeMatch[1]);
      const endTime = parseVTTTimecode(timecodeMatch[2]);
      const text = parts.slice(timecodeLineIndex + 1).join('\n');

      get().addCaption(trackId, startTime, endTime, text);
      imported++;
    }

    return imported;
  },

  exportToSRT: (trackId) => {
    const track = get().getTrackById(trackId);
    if (!track) return '';

    return track.captions
      .map((caption, index) => {
        const startTime = formatSRTTimecode(caption.startTime);
        const endTime = formatSRTTimecode(caption.endTime);
        return `${index + 1}\n${startTime} --> ${endTime}\n${caption.text}`;
      })
      .join('\n\n');
  },

  exportToVTT: (trackId) => {
    const track = get().getTrackById(trackId);
    if (!track) return '';

    const header = 'WEBVTT\n\n';
    const body = track.captions
      .map((caption, index) => {
        const startTime = formatVTTTimecode(caption.startTime);
        const endTime = formatVTTTimecode(caption.endTime);
        return `${index + 1}\n${startTime} --> ${endTime}\n${caption.text}`;
      })
      .join('\n\n');

    return header + body;
  },

  // Getters
  getTrackById: (trackId) => get().tracks.find((t) => t.id === trackId),

  getCaptionById: (trackId, captionId) => {
    const track = get().getTrackById(trackId);
    return track?.captions.find((c) => c.id === captionId);
  },

  getCaptionsInRange: (trackId, startTime, endTime) => {
    const track = get().getTrackById(trackId);
    if (!track) return [];
    return track.captions.filter(
      (c) =>
        (c.startTime >= startTime && c.startTime <= endTime) ||
        (c.endTime >= startTime && c.endTime <= endTime) ||
        (c.startTime <= startTime && c.endTime >= endTime)
    );
  },

  getVisibleTracks: () => get().tracks.filter((t) => t.visible),

  getActiveCaption: (time) => {
    const visibleTracks = get().getVisibleTracks();
    for (const track of visibleTracks) {
      const caption = track.captions.find((c) => c.startTime <= time && c.endTime >= time);
      if (caption) {
        return { track, caption };
      }
    }
    return null;
  },
}));
