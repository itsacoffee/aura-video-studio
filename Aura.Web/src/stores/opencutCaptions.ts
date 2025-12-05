/**
 * OpenCut Captions Store
 *
 * Manages caption tracks and captions for subtitle/caption support.
 * Supports SRT and VTT import/export, caption styling, and track management.
 */

import { create } from 'zustand';
import type { Caption, CaptionTrack, CaptionStyle, VerticalPosition } from '../types/opencut';

function generateId(): string {
  return `caption-${Date.now()}-${Math.random().toString(36).substring(2, 9)}`;
}

const defaultCaptionStyle: CaptionStyle = {
  fontFamily: 'Inter, system-ui, sans-serif',
  fontSize: 24,
  fontWeight: 600,
  color: '#FFFFFF',
  textAlign: 'center',
  backgroundColor: '#000000',
  backgroundPadding: 8,
  strokeColor: '#000000',
  strokeWidth: 2,
  letterSpacing: 0,
  lineHeight: 1.4,
};

interface CaptionsState {
  tracks: CaptionTrack[];
  selectedCaptionId: string | null;
  selectedTrackId: string | null;
  defaultStyle: CaptionStyle;
}

interface CaptionsActions {
  addTrack: (name: string, language?: string) => string;
  removeTrack: (trackId: string) => void;
  updateTrack: (trackId: string, updates: Partial<CaptionTrack>) => void;
  addCaption: (trackId: string, startTime: number, endTime: number, text: string) => string;
  removeCaption: (captionId: string) => void;
  updateCaption: (captionId: string, updates: Partial<Caption>) => void;
  setCaptionTiming: (captionId: string, startTime: number, endTime: number) => void;
  setCaptionStyle: (captionId: string, style: Partial<CaptionStyle>) => void;
  setDefaultStyle: (style: Partial<CaptionStyle>) => void;
  selectCaption: (captionId: string | null) => void;
  selectTrack: (trackId: string | null) => void;
  getCaptionAtTime: (trackId: string, time: number) => Caption | undefined;
  getCaptionById: (captionId: string) => Caption | undefined;
  getTrackById: (trackId: string) => CaptionTrack | undefined;
  importSRT: (trackId: string, srtContent: string) => void;
  exportSRT: (trackId: string) => string;
  importVTT: (trackId: string, vttContent: string) => void;
  exportVTT: (trackId: string) => string;
  splitCaption: (captionId: string, splitTime: number) => [string, string] | null;
  mergeCaptions: (captionId1: string, captionId2: string) => string | null;
  getCaptionsForTrack: (trackId: string) => Caption[];
  getVisibleTracks: () => CaptionTrack[];
  setTrackVisibility: (trackId: string, visible: boolean) => void;
  setTrackLocked: (trackId: string, locked: boolean) => void;
}

export type OpenCutCaptionsStore = CaptionsState & CaptionsActions;

export const useOpenCutCaptionsStore = create<OpenCutCaptionsStore>((set, get) => ({
  tracks: [],
  selectedCaptionId: null,
  selectedTrackId: null,
  defaultStyle: defaultCaptionStyle,

  addTrack: (name, language = 'en') => {
    const id = `track-${generateId()}`;
    const track: CaptionTrack = {
      id,
      name,
      language,
      isDefault: get().tracks.length === 0,
      defaultStyle: { ...get().defaultStyle },
      position: 'bottom' as VerticalPosition,
      margin: 40,
      captions: [],
      visible: true,
      locked: false,
    };
    set((state) => ({ tracks: [...state.tracks, track] }));
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

  addCaption: (trackId, startTime, endTime, text) => {
    const id = generateId();
    const caption: Caption = {
      id,
      trackId,
      startTime,
      endTime,
      text,
      style: undefined,
    };
    set((state) => ({
      tracks: state.tracks.map((t) => {
        if (t.id === trackId) {
          const captions = [...t.captions, caption].sort((a, b) => a.startTime - b.startTime);
          return { ...t, captions };
        }
        return t;
      }),
    }));
    return id;
  },

  removeCaption: (captionId) => {
    set((state) => ({
      tracks: state.tracks.map((t) => ({
        ...t,
        captions: t.captions.filter((c) => c.id !== captionId),
      })),
      selectedCaptionId: state.selectedCaptionId === captionId ? null : state.selectedCaptionId,
    }));
  },

  updateCaption: (captionId, updates) => {
    set((state) => ({
      tracks: state.tracks.map((t) => ({
        ...t,
        captions: t.captions
          .map((c) => (c.id === captionId ? { ...c, ...updates } : c))
          .sort((a, b) => a.startTime - b.startTime),
      })),
    }));
  },

  setCaptionTiming: (captionId, startTime, endTime) => {
    get().updateCaption(captionId, { startTime, endTime });
  },

  setCaptionStyle: (captionId, style) => {
    set((state) => ({
      tracks: state.tracks.map((t) => ({
        ...t,
        captions: t.captions.map((c) =>
          c.id === captionId ? { ...c, style: { ...c.style, ...style } } : c
        ),
      })),
    }));
  },

  setDefaultStyle: (style) => {
    set((state) => ({ defaultStyle: { ...state.defaultStyle, ...style } }));
  },

  selectCaption: (captionId) => set({ selectedCaptionId: captionId }),

  selectTrack: (trackId) => set({ selectedTrackId: trackId }),

  getCaptionAtTime: (trackId, time) => {
    const track = get().tracks.find((t) => t.id === trackId);
    return track?.captions.find((c) => time >= c.startTime && time < c.endTime);
  },

  getCaptionById: (captionId) => {
    for (const track of get().tracks) {
      const caption = track.captions.find((c) => c.id === captionId);
      if (caption) return caption;
    }
    return undefined;
  },

  getTrackById: (trackId) => get().tracks.find((t) => t.id === trackId),

  importSRT: (trackId, srtContent) => {
    const blocks = srtContent.trim().split(/\n\n+/);
    blocks.forEach((block) => {
      const lines = block.split('\n');
      if (lines.length >= 3) {
        const timeLine = lines[1];
        const timeMatch = timeLine.match(
          /(\d{2}):(\d{2}):(\d{2}),(\d{3})\s*-->\s*(\d{2}):(\d{2}):(\d{2}),(\d{3})/
        );
        if (timeMatch) {
          const startTime =
            parseInt(timeMatch[1]) * 3600 +
            parseInt(timeMatch[2]) * 60 +
            parseInt(timeMatch[3]) +
            parseInt(timeMatch[4]) / 1000;
          const endTime =
            parseInt(timeMatch[5]) * 3600 +
            parseInt(timeMatch[6]) * 60 +
            parseInt(timeMatch[7]) +
            parseInt(timeMatch[8]) / 1000;
          const text = lines.slice(2).join('\n');
          get().addCaption(trackId, startTime, endTime, text);
        }
      }
    });
  },

  exportSRT: (trackId) => {
    const track = get().tracks.find((t) => t.id === trackId);
    if (!track) return '';

    const formatTime = (seconds: number): string => {
      const h = Math.floor(seconds / 3600);
      const m = Math.floor((seconds % 3600) / 60);
      const s = Math.floor(seconds % 60);
      const ms = Math.floor((seconds % 1) * 1000);
      return `${h.toString().padStart(2, '0')}:${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')},${ms.toString().padStart(3, '0')}`;
    };

    return track.captions
      .map(
        (caption, index) =>
          `${index + 1}\n${formatTime(caption.startTime)} --> ${formatTime(caption.endTime)}\n${caption.text}`
      )
      .join('\n\n');
  },

  importVTT: (trackId, vttContent) => {
    const content = vttContent.replace(/^WEBVTT\s*\n\n?/, '').trim();
    const blocks = content.split(/\n\n+/);
    blocks.forEach((block) => {
      const lines = block.split('\n');
      const timeLine = lines.find((line) => line.includes('-->'));
      if (timeLine) {
        const timeMatch = timeLine.match(
          /(\d{2}):(\d{2}):(\d{2})\.(\d{3})\s*-->\s*(\d{2}):(\d{2}):(\d{2})\.(\d{3})/
        );
        if (timeMatch) {
          const startTime =
            parseInt(timeMatch[1]) * 3600 +
            parseInt(timeMatch[2]) * 60 +
            parseInt(timeMatch[3]) +
            parseInt(timeMatch[4]) / 1000;
          const endTime =
            parseInt(timeMatch[5]) * 3600 +
            parseInt(timeMatch[6]) * 60 +
            parseInt(timeMatch[7]) +
            parseInt(timeMatch[8]) / 1000;
          const timeLineIndex = lines.indexOf(timeLine);
          const text = lines.slice(timeLineIndex + 1).join('\n');
          get().addCaption(trackId, startTime, endTime, text);
        }
      }
    });
  },

  exportVTT: (trackId) => {
    const track = get().tracks.find((t) => t.id === trackId);
    if (!track) return '';

    const formatTime = (seconds: number): string => {
      const h = Math.floor(seconds / 3600);
      const m = Math.floor((seconds % 3600) / 60);
      const s = Math.floor(seconds % 60);
      const ms = Math.floor((seconds % 1) * 1000);
      return `${h.toString().padStart(2, '0')}:${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}.${ms.toString().padStart(3, '0')}`;
    };

    const captions = track.captions
      .map(
        (caption) =>
          `${formatTime(caption.startTime)} --> ${formatTime(caption.endTime)}\n${caption.text}`
      )
      .join('\n\n');

    return `WEBVTT\n\n${captions}`;
  },

  splitCaption: (captionId, splitTime) => {
    const track = get().tracks.find((t) => t.captions.some((c) => c.id === captionId));
    const caption = track?.captions.find((c) => c.id === captionId);
    if (!caption || !track) return null;
    if (splitTime <= caption.startTime || splitTime >= caption.endTime) return null;

    const words = caption.text.split(' ');
    const midPoint = Math.floor(words.length / 2);
    const text1 = words.slice(0, midPoint).join(' ');
    const text2 = words.slice(midPoint).join(' ');

    get().updateCaption(captionId, { endTime: splitTime, text: text1 });
    const newId = get().addCaption(track.id, splitTime, caption.endTime, text2);
    return [captionId, newId];
  },

  mergeCaptions: (captionId1, captionId2) => {
    const track = get().tracks.find((t) => t.captions.some((c) => c.id === captionId1));
    const caption1 = track?.captions.find((c) => c.id === captionId1);
    const caption2 = track?.captions.find((c) => c.id === captionId2);
    if (!caption1 || !caption2 || !track) return null;

    const mergedText = `${caption1.text} ${caption2.text}`;
    const startTime = Math.min(caption1.startTime, caption2.startTime);
    const endTime = Math.max(caption1.endTime, caption2.endTime);

    get().updateCaption(captionId1, { startTime, endTime, text: mergedText });
    get().removeCaption(captionId2);
    return captionId1;
  },

  getCaptionsForTrack: (trackId) => {
    const track = get().tracks.find((t) => t.id === trackId);
    return track?.captions ?? [];
  },

  getVisibleTracks: () => get().tracks.filter((t) => t.visible),

  setTrackVisibility: (trackId, visible) => {
    get().updateTrack(trackId, { visible });
  },

  setTrackLocked: (trackId, locked) => {
    get().updateTrack(trackId, { locked });
  },
}));
