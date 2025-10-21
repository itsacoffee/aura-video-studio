import { create } from 'zustand';

export interface TimelineClip {
  id: string;
  sourcePath: string;
  sourceIn: number;
  sourceOut: number;
  timelineStart: number;
  trackId: string;
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
  alignment: 'topLeft' | 'topCenter' | 'topRight' | 'middleLeft' | 'middleCenter' | 'middleRight' | 'bottomLeft' | 'bottomCenter' | 'bottomRight' | 'custom';
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
    { id: 'V1', name: 'Video 1', type: 'video', clips: [], muted: false, volume: 100, pan: 0, locked: false, height: 60 },
    { id: 'V2', name: 'Video 2', type: 'video', clips: [], muted: false, volume: 100, pan: 0, locked: false, height: 60 },
    { id: 'A1', name: 'Audio 1', type: 'audio', clips: [], muted: false, volume: 100, pan: 0, locked: false, height: 80 },
    { id: 'A2', name: 'Audio 2', type: 'audio', clips: [], muted: false, volume: 100, pan: 0, locked: false, height: 80 },
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
  
  addClip: (trackId, clip) => set((state) => {
    const tracks = state.tracks.map((track) =>
      track.id === trackId
        ? { ...track, clips: [...track.clips, clip].sort((a, b) => a.timelineStart - b.timelineStart) }
        : track
    );
    return { tracks };
  }),
  
  updateClip: (clip) => set((state) => {
    const tracks = state.tracks.map((track) => ({
      ...track,
      clips: track.clips.map((c) => (c.id === clip.id ? clip : c)).sort((a, b) => a.timelineStart - b.timelineStart),
    }));
    return { tracks };
  }),
  
  removeClip: (clipId) => set((state) => {
    const tracks = state.tracks.map((track) => ({
      ...track,
      clips: track.clips.filter((c) => c.id !== clipId),
    }));
    return { tracks };
  }),
  
  splitClip: (clipId, splitTime) => set((state) => {
    const tracks = state.tracks.map((track) => {
      const clipIndex = track.clips.findIndex((c) => c.id === clipId);
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
  
  addMarker: (marker) => set((state) => ({
    markers: [...state.markers, marker].sort((a, b) => a.time - b.time),
  })),
  
  removeMarker: (markerId) => set((state) => ({
    markers: state.markers.filter((m) => m.id !== markerId),
  })),
  
  addOverlay: (overlay) => set((state) => ({
    overlays: [...state.overlays, overlay].sort((a, b) => a.inTime - b.inTime),
  })),
  
  updateOverlay: (overlay) => set((state) => ({
    overlays: state.overlays.map((o) => (o.id === overlay.id ? overlay : o)).sort((a, b) => a.inTime - b.inTime),
  })),
  
  removeOverlay: (overlayId) => set((state) => ({
    overlays: state.overlays.filter((o) => o.id !== overlayId),
  })),
  
  // Audio track controls
  updateTrack: (trackId, updates) => set((state) => ({
    tracks: state.tracks.map((track) =>
      track.id === trackId ? { ...track, ...updates } : track
    ),
  })),
  
  toggleMute: (trackId) => set((state) => ({
    tracks: state.tracks.map((track) =>
      track.id === trackId ? { ...track, muted: !track.muted } : track
    ),
  })),
  
  toggleSolo: (trackId) => set((state) => ({
    tracks: state.tracks.map((track) =>
      track.id === trackId ? { ...track, solo: !track.solo } : track
    ),
  })),
  
  toggleLock: (trackId) => set((state) => ({
    tracks: state.tracks.map((track) =>
      track.id === trackId ? { ...track, locked: !track.locked } : track
    ),
  })),
  
  exportChapters: () => {
    const { markers } = get();
    return markers
      .sort((a, b) => a.time - b.time)
      .map((marker) => {
        const totalSeconds = Math.floor(marker.time);
        const hours = Math.floor(totalSeconds / 3600);
        const minutes = Math.floor((totalSeconds % 3600) / 60);
        const seconds = totalSeconds % 60;
        
        const timeStr = hours > 0
          ? `${hours}:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`
          : `${minutes}:${seconds.toString().padStart(2, '0')}`;
        
        return `${timeStr} ${marker.title}`;
      })
      .join('\n');
  },
}));
