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

export interface SnapConfiguration {
  enabled: boolean;
  thresholdMs: number;
  snapToClips: boolean;
  snapToMarkers: boolean;
  snapToCaptions: boolean;
  snapToAudioPeaks: boolean;
  snapToPlayhead: boolean;
}

interface TimelineState {
  tracks: Track[];
  markers: ChapterMarker[];
  overlays: TextOverlay[];
  snappingEnabled: boolean;
  snapConfig: SnapConfiguration;
  currentTime: number;
  zoom: number;
  selectedClipId: string | null;
  selectedClipIds: string[];
  selectedOverlayId: string | null;
  isPlaying: boolean;
  inPoint?: number;
  outPoint?: number;
  rippleEditMode: boolean;
  magneticTimelineEnabled: boolean;

  setSnappingEnabled: (enabled: boolean) => void;
  setSnapConfig: (config: Partial<SnapConfiguration>) => void;
  setCurrentTime: (time: number) => void;
  setZoom: (zoom: number) => void;
  setSelectedClipId: (id: string | null) => void;
  setSelectedClipIds: (ids: string[]) => void;
  toggleClipSelection: (id: string) => void;
  selectClipRange: (startId: string, endId: string) => void;
  clearSelection: () => void;
  setSelectedOverlayId: (id: string | null) => void;
  setPlaying: (playing: boolean) => void;
  setInPoint: (time?: number) => void;
  setOutPoint: (time?: number) => void;
  setRippleEditMode: (enabled: boolean) => void;
  setMagneticTimelineEnabled: (enabled: boolean) => void;

  addClip: (trackId: string, clip: TimelineClip) => void;
  updateClip: (clip: TimelineClip) => void;
  removeClip: (clipId: string) => void;
  removeClips: (clipIds: string[]) => void;
  rippleDeleteClip: (clipId: string) => void;
  rippleDeleteClips: (clipIds: string[]) => void;

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
  snapConfig: {
    enabled: true,
    thresholdMs: 100,
    snapToClips: true,
    snapToMarkers: true,
    snapToCaptions: true,
    snapToAudioPeaks: true,
    snapToPlayhead: true,
  },
  currentTime: 0,
  zoom: 50,
  selectedClipId: null,
  selectedClipIds: [],
  selectedOverlayId: null,
  isPlaying: false,
  inPoint: undefined,
  outPoint: undefined,
  rippleEditMode: false,
  magneticTimelineEnabled: false,

  setSnappingEnabled: (enabled) => set({ snappingEnabled: enabled }),
  setSnapConfig: (config) =>
    set((state) => ({
      snapConfig: { ...state.snapConfig, ...config },
    })),
  setCurrentTime: (time) => set({ currentTime: time }),
  setZoom: (zoom) => set({ zoom }),
  setSelectedClipId: (id) =>
    set({
      selectedClipId: id,
      selectedClipIds: id ? [id] : [],
    }),
  setSelectedClipIds: (ids) =>
    set({
      selectedClipIds: ids,
      selectedClipId: ids.length > 0 ? ids[0] : null,
    }),
  toggleClipSelection: (id) =>
    set((state) => {
      const isSelected = state.selectedClipIds.includes(id);
      const newSelection = isSelected
        ? state.selectedClipIds.filter((clipId) => clipId !== id)
        : [...state.selectedClipIds, id];
      return {
        selectedClipIds: newSelection,
        selectedClipId: newSelection.length > 0 ? newSelection[0] : null,
      };
    }),
  selectClipRange: (startId, endId) =>
    set((state) => {
      const allClips: TimelineClip[] = [];
      state.tracks.forEach((track) => {
        allClips.push(...track.clips);
      });
      allClips.sort((a, b) => a.timelineStart - b.timelineStart);

      const startIndex = allClips.findIndex((c) => c.id === startId);
      const endIndex = allClips.findIndex((c) => c.id === endId);

      if (startIndex === -1 || endIndex === -1) {
        return {};
      }

      const [start, end] = startIndex < endIndex ? [startIndex, endIndex] : [endIndex, startIndex];
      const selectedIds = allClips.slice(start, end + 1).map((c) => c.id);

      return {
        selectedClipIds: selectedIds,
        selectedClipId: selectedIds.length > 0 ? selectedIds[0] : null,
      };
    }),
  clearSelection: () =>
    set({
      selectedClipId: null,
      selectedClipIds: [],
    }),
  setSelectedOverlayId: (id) => set({ selectedOverlayId: id }),
  setPlaying: (playing) => set({ isPlaying: playing }),
  setInPoint: (time) => set({ inPoint: time }),
  setOutPoint: (time) => set({ outPoint: time }),
  setRippleEditMode: (enabled) => set({ rippleEditMode: enabled }),
  setMagneticTimelineEnabled: (enabled) => set({ magneticTimelineEnabled: enabled }),

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

  removeClips: (clipIds: string[]) =>
    set((state) => {
      const clipIdsSet = new Set(clipIds);
      const tracks = state.tracks.map((track: Track) => ({
        ...track,
        clips: track.clips.filter((c: TimelineClip) => !clipIdsSet.has(c.id)),
      }));
      return { tracks, selectedClipIds: [], selectedClipId: null };
    }),

  rippleDeleteClip: (clipId: string) =>
    set((state) => {
      const tracks = state.tracks.map((track: Track) => {
        const clip = track.clips.find((c: TimelineClip) => c.id === clipId);
        if (clip) {
          const remainingClips = track.clips.filter((c: TimelineClip) => c.id !== clipId);
          const updatedClips = remainingClips.map((c: TimelineClip) => {
            if (c.timelineStart > clip.timelineStart) {
              return {
                ...c,
                timelineStart: c.timelineStart - (clip.sourceOut - clip.sourceIn),
              };
            }
            return c;
          });
          return { ...track, clips: updatedClips };
        }
        return track;
      });

      return { tracks, selectedClipId: null, selectedClipIds: [] };
    }),

  rippleDeleteClips: (clipIds: string[]) =>
    set((state) => {
      if (clipIds.length === 0) return {};

      const clipsToDelete: Array<{ clip: TimelineClip; trackId: string }> = [];
      state.tracks.forEach((track) => {
        track.clips.forEach((clip) => {
          if (clipIds.includes(clip.id)) {
            clipsToDelete.push({ clip, trackId: track.id });
          }
        });
      });

      clipsToDelete.sort((a, b) => b.clip.timelineStart - a.clip.timelineStart);

      let tracks = state.tracks;
      clipsToDelete.forEach(({ clip, trackId }) => {
        const clipDuration = clip.sourceOut - clip.sourceIn;
        tracks = tracks.map((track: Track) => {
          if (track.id === trackId) {
            const remainingClips = track.clips.filter((c: TimelineClip) => c.id !== clip.id);
            const updatedClips = remainingClips.map((c: TimelineClip) => {
              if (c.timelineStart > clip.timelineStart) {
                return {
                  ...c,
                  timelineStart: c.timelineStart - clipDuration,
                };
              }
              return c;
            });
            return { ...track, clips: updatedClips };
          }
          return track;
        });
      });

      return { tracks, selectedClipIds: [], selectedClipId: null };
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
      markers: [...state.markers, marker].sort(
        (a: ChapterMarker, b: ChapterMarker) => a.time - b.time
      ),
    })),

  removeMarker: (markerId: string) =>
    set((state) => ({
      markers: state.markers.filter((m: ChapterMarker) => m.id !== markerId),
    })),

  addOverlay: (overlay: TextOverlay) =>
    set((state) => ({
      overlays: [...state.overlays, overlay].sort(
        (a: TextOverlay, b: TextOverlay) => a.inTime - b.inTime
      ),
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
