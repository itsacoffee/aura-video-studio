import { create } from 'zustand';
import { AppliedEffect } from '../types/effects';

export interface TimelineClip {
  id: string;
  sourcePath: string;
  sourceIn: number;
  sourceOut: number;
  timelineStart: number;
  trackId: string;
  effects?: AppliedEffect[];
  layerIndex?: number; // For multi-layer compositing
}

export interface Track {
  id: string;
  name: string;
  type: 'video' | 'audio';
  clips: TimelineClip[];
  muted?: boolean;
  solo?: boolean;
  volume?: number; // 0-200
  pan?: number; // -100 to 100
  locked?: boolean;
  height?: number; // pixels
  compositeMode?: 'normal' | 'multiply' | 'screen' | 'overlay' | 'add';
}

export interface ChapterMarker {
  id: string;
  title: string;
  time: number;
}

export interface TextOverlay {
  id: string;
  type: 'title' | 'lowerThird' | 'callout';
  text: string;
  inTime: number;
  outTime: number;
  alignment:
    | 'topLeft'
    | 'topCenter'
    | 'topRight'
    | 'middleLeft'
    | 'middleCenter'
    | 'middleRight'
    | 'bottomLeft'
    | 'bottomCenter'
    | 'bottomRight'
    | 'custom';
  x: number;
  y: number;
  fontSize: number;
  fontColor: string;
  backgroundColor?: string;
  backgroundOpacity: number;
  borderWidth: number;
  borderColor?: string;
}

interface TimelineState {
  tracks: Track[];
  markers: ChapterMarker[];
  overlays: TextOverlay[];
  snappingEnabled: boolean;
  currentTime: number;
  zoom: number;
  selectedClipId: string | null;
  selectedOverlayId: string | null;
  isPlaying: boolean;
  inPoint?: number;
  outPoint?: number;

  setSnappingEnabled: (enabled: boolean) => void;
  setCurrentTime: (time: number) => void;
  setZoom: (zoom: number) => void;
  setSelectedClipId: (id: string | null) => void;
  setSelectedOverlayId: (id: string | null) => void;
  setPlaying: (playing: boolean) => void;
  setInPoint: (time?: number) => void;
  setOutPoint: (time?: number) => void;

  addClip: (trackId: string, clip: TimelineClip) => void;
  updateClip: (clip: TimelineClip) => void;
  removeClip: (clipId: string) => void;

  splitClip: (clipId: string, splitTime: number) => void;

  addMarker: (marker: ChapterMarker) => void;
  removeMarker: (markerId: string) => void;

  addOverlay: (overlay: TextOverlay) => void;
  updateOverlay: (overlay: TextOverlay) => void;
  removeOverlay: (overlayId: string) => void;

  // Audio track controls
  updateTrack: (trackId: string, updates: Partial<Track>) => void;
  toggleMute: (trackId: string) => void;
  toggleSolo: (trackId: string) => void;
  toggleLock: (trackId: string) => void;

  exportChapters: () => string;
}

export const useTimelineStore = create<TimelineState>((set, get) => ({
  tracks: [
    {
      id: 'V1',
      name: 'Video 1',
      type: 'video',
      clips: [],
      muted: false,
      volume: 100,
      pan: 0,
      locked: false,
      height: 60,
    },
    {
      id: 'V2',
      name: 'Video 2',
      type: 'video',
      clips: [],
      muted: false,
      volume: 100,
      pan: 0,
      locked: false,
      height: 60,
    },
    {
      id: 'A1',
      name: 'Audio 1',
      type: 'audio',
      clips: [],
      muted: false,
      volume: 100,
      pan: 0,
      locked: false,
      height: 80,
    },
    {
      id: 'A2',
      name: 'Audio 2',
      type: 'audio',
      clips: [],
      muted: false,
      volume: 100,
      pan: 0,
      locked: false,
      height: 80,
    },
  ],
  markers: [],
  overlays: [],
  snappingEnabled: true,
  currentTime: 0,
  zoom: 50,
  selectedClipId: null,
  selectedOverlayId: null,
  isPlaying: false,
  inPoint: undefined,
  outPoint: undefined,

  setSnappingEnabled: (enabled) => set({ snappingEnabled: enabled }),
  setCurrentTime: (time) => set({ currentTime: time }),
  setZoom: (zoom) => set({ zoom }),
  setSelectedClipId: (id) => set({ selectedClipId: id }),
  setSelectedOverlayId: (id) => set({ selectedOverlayId: id }),
  setPlaying: (playing) => set({ isPlaying: playing }),
  setInPoint: (time) => set({ inPoint: time }),
  setOutPoint: (time) => set({ outPoint: time }),

  addClip: (trackId, clip) =>
    set((state) => {
      const tracks = state.tracks.map((track) =>
        track.id === trackId
          ? {
              ...track,
              clips: [...track.clips, clip].sort((a, b) => a.timelineStart - b.timelineStart),
            }
          : track
      );
      return { tracks };
    }),

  updateClip: (clip: TimelineClip) =>
    set((state) => {
      const tracks = state.tracks.map((track: Track) => ({
        ...track,
        clips: track.clips
          .map((c: TimelineClip) => (c.id === clip.id ? clip : c))
          .sort((a: TimelineClip, b: TimelineClip) => a.timelineStart - b.timelineStart),
      }));
      return { tracks };
    }),

  removeClip: (clipId: string) =>
    set((state) => {
      const tracks = state.tracks.map((track: Track) => ({
        ...track,
        clips: track.clips.filter((c: TimelineClip) => c.id !== clipId),
      }));
      return { tracks };
    }),

  splitClip: (clipId: string, splitTime: number) =>
    set((state) => {
      const tracks = state.tracks.map((track: Track) => {
        const clipIndex = track.clips.findIndex((c: TimelineClip) => c.id === clipId);
        if (clipIndex === -1) return track;

        const clip = track.clips[clipIndex];
        const clipEnd = clip.timelineStart + (clip.sourceOut - clip.sourceIn);

        if (splitTime <= clip.timelineStart || splitTime >= clipEnd) {
          return track;
        }

        const offsetInClip = splitTime - clip.timelineStart;
        const newSourceIn = clip.sourceIn + offsetInClip;

        const firstClip = {
          ...clip,
          sourceOut: newSourceIn,
        };

        const secondClip: TimelineClip = {
          ...clip,
          id: `${clip.id}_split_${Date.now()}`,
          sourceIn: newSourceIn,
          timelineStart: splitTime,
        };

        const newClips = [
          ...track.clips.slice(0, clipIndex),
          firstClip,
          secondClip,
          ...track.clips.slice(clipIndex + 1),
        ].sort((a, b) => a.timelineStart - b.timelineStart);

        return { ...track, clips: newClips };
      });

      return { tracks };
    }),

  addMarker: (marker: ChapterMarker) =>
    set((state) => ({
      markers: [...state.markers, marker].sort((a: ChapterMarker, b: ChapterMarker) => a.time - b.time),
    })),

  removeMarker: (markerId: string) =>
    set((state) => ({
      markers: state.markers.filter((m: ChapterMarker) => m.id !== markerId),
    })),

  addOverlay: (overlay: TextOverlay) =>
    set((state) => ({
      overlays: [...state.overlays, overlay].sort((a: TextOverlay, b: TextOverlay) => a.inTime - b.inTime),
    })),

  updateOverlay: (overlay: TextOverlay) =>
    set((state) => ({
      overlays: state.overlays
        .map((o: TextOverlay) => (o.id === overlay.id ? overlay : o))
        .sort((a: TextOverlay, b: TextOverlay) => a.inTime - b.inTime),
    })),

  removeOverlay: (overlayId: string) =>
    set((state) => ({
      overlays: state.overlays.filter((o: TextOverlay) => o.id !== overlayId),
    })),

  // Audio track controls
  updateTrack: (trackId: string, updates: Partial<Track>) =>
    set((state) => ({
      tracks: state.tracks.map((track: Track) =>
        track.id === trackId ? { ...track, ...updates } : track
      ),
    })),

  toggleMute: (trackId: string) =>
    set((state) => ({
      tracks: state.tracks.map((track: Track) =>
        track.id === trackId ? { ...track, muted: !track.muted } : track
      ),
    })),

  toggleSolo: (trackId: string) =>
    set((state) => ({
      tracks: state.tracks.map((track: Track) =>
        track.id === trackId ? { ...track, solo: !track.solo } : track
      ),
    })),

  toggleLock: (trackId: string) =>
    set((state) => ({
      tracks: state.tracks.map((track: Track) =>
        track.id === trackId ? { ...track, locked: !track.locked } : track
      ),
    })),

  exportChapters: () => {
    const { markers } = get();
    return markers
      .sort((a: ChapterMarker, b: ChapterMarker) => a.time - b.time)
      .map((marker: ChapterMarker) => {
        const totalSeconds = Math.floor(marker.time);
        const hours = Math.floor(totalSeconds / 3600);
        const minutes = Math.floor((totalSeconds % 3600) / 60);
        const seconds = totalSeconds % 60;

        const timeStr =
          hours > 0
            ? `${hours}:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`
            : `${minutes}:${seconds.toString().padStart(2, '0')}`;

        return `${timeStr} ${marker.title}`;
      })
      .join('\n');
  },
}));
