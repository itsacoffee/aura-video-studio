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
  
  setSnappingEnabled: (enabled: boolean) => void;
  setCurrentTime: (time: number) => void;
  setZoom: (zoom: number) => void;
  setSelectedClipId: (id: string | null) => void;
  setSelectedOverlayId: (id: string | null) => void;
  
  addClip: (trackId: string, clip: TimelineClip) => void;
  updateClip: (clip: TimelineClip) => void;
  removeClip: (clipId: string) => void;
  
  splitClip: (clipId: string, splitTime: number) => void;
  
  addMarker: (marker: ChapterMarker) => void;
  removeMarker: (markerId: string) => void;
  
  addOverlay: (overlay: TextOverlay) => void;
  updateOverlay: (overlay: TextOverlay) => void;
  removeOverlay: (overlayId: string) => void;
  
  exportChapters: () => string;
}

export const useTimelineStore = create<TimelineState>((set, get) => ({
  tracks: [
    { id: 'V1', name: 'Video 1', type: 'video', clips: [] },
    { id: 'V2', name: 'Video 2', type: 'video', clips: [] },
    { id: 'A1', name: 'Audio 1', type: 'audio', clips: [] },
    { id: 'A2', name: 'Audio 2', type: 'audio', clips: [] },
  ],
  markers: [],
  overlays: [],
  snappingEnabled: true,
  currentTime: 0,
  zoom: 1.0,
  selectedClipId: null,
  selectedOverlayId: null,
  
  setSnappingEnabled: (enabled) => set({ snappingEnabled: enabled }),
  setCurrentTime: (time) => set({ currentTime: time }),
  setZoom: (zoom) => set({ zoom }),
  setSelectedClipId: (id) => set({ selectedClipId: id }),
  setSelectedOverlayId: (id) => set({ selectedOverlayId: id }),
  
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
