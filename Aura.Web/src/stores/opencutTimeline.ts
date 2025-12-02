/**
 * OpenCut Timeline Store
 *
 * Manages timeline state including tracks, clips, selection, and undo/redo history.
 * Follows professional NLE (Non-Linear Editor) patterns for video editing.
 */

import { create } from 'zustand';

/** Clip types in the timeline */
export type ClipType = 'video' | 'audio' | 'image' | 'text';

/** Transform properties for clips */
export interface ClipTransform {
  scaleX: number;
  scaleY: number;
  positionX: number;
  positionY: number;
  rotation: number;
  opacity: number;
  anchorX: number;
  anchorY: number;
}

/** Blend mode options */
export type BlendMode =
  | 'normal'
  | 'multiply'
  | 'screen'
  | 'overlay'
  | 'darken'
  | 'lighten'
  | 'color-dodge'
  | 'color-burn'
  | 'hard-light'
  | 'soft-light'
  | 'difference'
  | 'exclusion';

/** Audio properties for clips */
export interface ClipAudio {
  volume: number;
  pan: number;
  fadeInDuration: number;
  fadeOutDuration: number;
  muted: boolean;
}

/** Text properties for text clips */
export interface ClipText {
  content: string;
  fontFamily: string;
  fontSize: number;
  fontWeight: number;
  fontStyle: 'normal' | 'italic';
  textAlign: 'left' | 'center' | 'right';
  color: string;
  strokeColor: string;
  strokeWidth: number;
  shadowColor: string;
  shadowBlur: number;
  shadowOffsetX: number;
  shadowOffsetY: number;
}

/** Timeline clip */
export interface TimelineClip {
  id: string;
  trackId: string;
  type: ClipType;
  name: string;
  mediaId: string | null;
  startTime: number;
  duration: number;
  inPoint: number;
  outPoint: number;
  thumbnailUrl?: string;
  transform: ClipTransform;
  blendMode: BlendMode;
  audio?: ClipAudio;
  text?: ClipText;
  speed: number;
  locked: boolean;
}

/** Timeline track */
export interface TimelineTrack {
  id: string;
  type: ClipType;
  name: string;
  order: number;
  height: number;
  muted: boolean;
  solo: boolean;
  locked: boolean;
  visible: boolean;
}

/** Snapshot for undo/redo */
interface TimelineSnapshot {
  tracks: TimelineTrack[];
  clips: TimelineClip[];
  description: string;
}

/** Timeline state */
export interface OpenCutTimelineState {
  tracks: TimelineTrack[];
  clips: TimelineClip[];
  selectedClipIds: string[];
  selectedTrackId: string | null;
  zoom: number;
  scrollPosition: number;
  snapEnabled: boolean;
  rippleEnabled: boolean;
  undoStack: TimelineSnapshot[];
  redoStack: TimelineSnapshot[];
  maxHistorySize: number;
}

/** Timeline actions */
export interface OpenCutTimelineActions {
  // Track operations
  addTrack: (type: ClipType, name?: string) => string;
  removeTrack: (trackId: string) => void;
  updateTrack: (trackId: string, updates: Partial<TimelineTrack>) => void;
  reorderTracks: (trackIds: string[]) => void;
  muteTrack: (trackId: string, muted: boolean) => void;
  soloTrack: (trackId: string, solo: boolean) => void;
  lockTrack: (trackId: string, locked: boolean) => void;

  // Clip operations
  addClip: (clip: Omit<TimelineClip, 'id'>) => string;
  removeClip: (clipId: string) => void;
  updateClip: (clipId: string, updates: Partial<TimelineClip>) => void;
  moveClip: (clipId: string, trackId: string, startTime: number) => void;
  trimClipStart: (clipId: string, newStartTime: number) => void;
  trimClipEnd: (clipId: string, newDuration: number) => void;
  splitClip: (clipId: string, splitTime: number) => [string, string] | null;
  duplicateClip: (clipId: string) => string | null;

  // Transform operations
  updateClipTransform: (clipId: string, transform: Partial<ClipTransform>) => void;
  updateClipBlendMode: (clipId: string, blendMode: BlendMode) => void;
  updateClipAudio: (clipId: string, audio: Partial<ClipAudio>) => void;
  updateClipText: (clipId: string, text: Partial<ClipText>) => void;

  // Selection
  selectClip: (clipId: string, addToSelection?: boolean) => void;
  selectClips: (clipIds: string[]) => void;
  deselectClip: (clipId: string) => void;
  clearSelection: () => void;
  selectTrack: (trackId: string | null) => void;
  selectAllClipsOnTrack: (trackId: string) => void;

  // Bulk operations on selected clips
  deleteSelectedClips: () => void;
  duplicateSelectedClips: () => string[];
  splitSelectedClips: (time: number) => void;

  // Zoom and scroll
  setZoom: (zoom: number) => void;
  zoomIn: () => void;
  zoomOut: () => void;
  fitToWindow: (containerWidth: number, totalDuration: number) => void;
  setScrollPosition: (position: number) => void;

  // Snap and ripple
  setSnapEnabled: (enabled: boolean) => void;
  setRippleEnabled: (enabled: boolean) => void;

  // Undo/Redo
  saveSnapshot: (description: string) => void;
  undo: () => void;
  redo: () => void;
  canUndo: () => boolean;
  canRedo: () => boolean;
  clearHistory: () => void;

  // Getters
  getClipById: (clipId: string) => TimelineClip | undefined;
  getTrackById: (trackId: string) => TimelineTrack | undefined;
  getClipsOnTrack: (trackId: string) => TimelineClip[];
  getSelectedClips: () => TimelineClip[];
  getTotalDuration: () => number;
}

export type OpenCutTimelineStore = OpenCutTimelineState & OpenCutTimelineActions;

function generateId(): string {
  return `${Date.now()}-${Math.random().toString(36).substring(2, 9)}`;
}

const defaultTransform: ClipTransform = {
  scaleX: 100,
  scaleY: 100,
  positionX: 0,
  positionY: 0,
  rotation: 0,
  opacity: 100,
  anchorX: 50,
  anchorY: 50,
};

const defaultAudio: ClipAudio = {
  volume: 100,
  pan: 0,
  fadeInDuration: 0,
  fadeOutDuration: 0,
  muted: false,
};

const defaultText: ClipText = {
  content: 'New Text',
  fontFamily: 'Inter, system-ui, sans-serif',
  fontSize: 48,
  fontWeight: 400,
  fontStyle: 'normal',
  textAlign: 'center',
  color: '#ffffff',
  strokeColor: '#000000',
  strokeWidth: 0,
  shadowColor: 'rgba(0, 0, 0, 0.5)',
  shadowBlur: 0,
  shadowOffsetX: 0,
  shadowOffsetY: 0,
};

export const useOpenCutTimelineStore = create<OpenCutTimelineStore>((set, get) => ({
  tracks: [
    {
      id: 'track-video-1',
      type: 'video',
      name: 'Video 1',
      order: 0,
      height: 56,
      muted: false,
      solo: false,
      locked: false,
      visible: true,
    },
    {
      id: 'track-audio-1',
      type: 'audio',
      name: 'Audio 1',
      order: 1,
      height: 56,
      muted: false,
      solo: false,
      locked: false,
      visible: true,
    },
    {
      id: 'track-text-1',
      type: 'text',
      name: 'Text 1',
      order: 2,
      height: 56,
      muted: false,
      solo: false,
      locked: false,
      visible: true,
    },
  ],
  clips: [],
  selectedClipIds: [],
  selectedTrackId: null,
  zoom: 1,
  scrollPosition: 0,
  snapEnabled: true,
  rippleEnabled: false,
  undoStack: [],
  redoStack: [],
  maxHistorySize: 50,

  // Track operations
  addTrack: (type, name) => {
    const { tracks, saveSnapshot } = get();
    saveSnapshot('Add track');
    const id = `track-${type}-${generateId()}`;
    const newTrack: TimelineTrack = {
      id,
      type,
      name:
        name ||
        `${type.charAt(0).toUpperCase() + type.slice(1)} ${tracks.filter((t) => t.type === type).length + 1}`,
      order: tracks.length,
      height: 56,
      muted: false,
      solo: false,
      locked: false,
      visible: true,
    };
    set((state) => ({ tracks: [...state.tracks, newTrack] }));
    return id;
  },

  removeTrack: (trackId) => {
    const { saveSnapshot } = get();
    saveSnapshot('Remove track');
    set((state) => ({
      tracks: state.tracks.filter((t) => t.id !== trackId),
      clips: state.clips.filter((c) => c.trackId !== trackId),
      selectedTrackId: state.selectedTrackId === trackId ? null : state.selectedTrackId,
    }));
  },

  updateTrack: (trackId, updates) => {
    set((state) => ({
      tracks: state.tracks.map((t) => (t.id === trackId ? { ...t, ...updates } : t)),
    }));
  },

  reorderTracks: (trackIds) => {
    set((state) => ({
      tracks: trackIds
        .map((id, index) => {
          const track = state.tracks.find((t) => t.id === id);
          return track ? { ...track, order: index } : null;
        })
        .filter((t): t is TimelineTrack => t !== null),
    }));
  },

  muteTrack: (trackId, muted) => get().updateTrack(trackId, { muted }),
  soloTrack: (trackId, solo) => get().updateTrack(trackId, { solo }),
  lockTrack: (trackId, locked) => get().updateTrack(trackId, { locked }),

  // Clip operations
  addClip: (clipData) => {
    const { saveSnapshot } = get();
    saveSnapshot('Add clip');
    const id = `clip-${generateId()}`;
    const clip: TimelineClip = {
      ...clipData,
      id,
      transform: clipData.transform || { ...defaultTransform },
      blendMode: clipData.blendMode || 'normal',
      audio:
        clipData.type === 'audio' || clipData.type === 'video'
          ? { ...defaultAudio, ...clipData.audio }
          : undefined,
      text: clipData.type === 'text' ? { ...defaultText, ...clipData.text } : undefined,
      speed: clipData.speed || 1,
      locked: clipData.locked || false,
    };
    set((state) => ({ clips: [...state.clips, clip] }));
    return id;
  },

  removeClip: (clipId) => {
    const { saveSnapshot } = get();
    saveSnapshot('Remove clip');
    set((state) => ({
      clips: state.clips.filter((c) => c.id !== clipId),
      selectedClipIds: state.selectedClipIds.filter((id) => id !== clipId),
    }));
  },

  updateClip: (clipId, updates) => {
    set((state) => ({
      clips: state.clips.map((c) => (c.id === clipId ? { ...c, ...updates } : c)),
    }));
  },

  moveClip: (clipId, trackId, startTime) => {
    const { saveSnapshot } = get();
    saveSnapshot('Move clip');
    get().updateClip(clipId, { trackId, startTime: Math.max(0, startTime) });
  },

  trimClipStart: (clipId, newStartTime) => {
    const { clips, saveSnapshot } = get();
    const clip = clips.find((c) => c.id === clipId);
    if (!clip) return;

    saveSnapshot('Trim clip start');
    const timeDelta = newStartTime - clip.startTime;
    const newInPoint = clip.inPoint + timeDelta;
    const newDuration = clip.duration - timeDelta;

    if (newDuration > 0 && newInPoint >= 0) {
      get().updateClip(clipId, {
        startTime: newStartTime,
        inPoint: newInPoint,
        duration: newDuration,
      });
    }
  },

  trimClipEnd: (clipId, newDuration) => {
    const { saveSnapshot } = get();
    saveSnapshot('Trim clip end');
    if (newDuration > 0) {
      get().updateClip(clipId, { duration: newDuration });
    }
  },

  splitClip: (clipId, splitTime) => {
    const { clips, saveSnapshot, addClip } = get();
    const clip = clips.find((c) => c.id === clipId);
    if (!clip) return null;

    const relativeTime = splitTime - clip.startTime;
    if (relativeTime <= 0 || relativeTime >= clip.duration) return null;

    saveSnapshot('Split clip');

    // Update first part
    get().updateClip(clipId, { duration: relativeTime });

    // Create second part
    const secondPartId = addClip({
      ...clip,
      startTime: splitTime,
      duration: clip.duration - relativeTime,
      inPoint: clip.inPoint + relativeTime,
    });

    return [clipId, secondPartId];
  },

  duplicateClip: (clipId) => {
    const { clips, saveSnapshot, addClip } = get();
    const clip = clips.find((c) => c.id === clipId);
    if (!clip) return null;

    saveSnapshot('Duplicate clip');
    return addClip({
      ...clip,
      name: `${clip.name} (copy)`,
      startTime: clip.startTime + clip.duration,
    });
  },

  // Transform operations
  updateClipTransform: (clipId, transform) => {
    const { clips } = get();
    const clip = clips.find((c) => c.id === clipId);
    if (!clip) return;
    get().updateClip(clipId, {
      transform: { ...clip.transform, ...transform },
    });
  },

  updateClipBlendMode: (clipId, blendMode) => {
    get().updateClip(clipId, { blendMode });
  },

  updateClipAudio: (clipId, audio) => {
    const { clips } = get();
    const clip = clips.find((c) => c.id === clipId);
    if (!clip?.audio) return;
    get().updateClip(clipId, {
      audio: { ...clip.audio, ...audio },
    });
  },

  updateClipText: (clipId, text) => {
    const { clips } = get();
    const clip = clips.find((c) => c.id === clipId);
    if (!clip?.text) return;
    get().updateClip(clipId, {
      text: { ...clip.text, ...text },
    });
  },

  // Selection
  selectClip: (clipId, addToSelection = false) => {
    set((state) => ({
      selectedClipIds: addToSelection
        ? state.selectedClipIds.includes(clipId)
          ? state.selectedClipIds
          : [...state.selectedClipIds, clipId]
        : [clipId],
    }));
  },

  selectClips: (clipIds) => set({ selectedClipIds: clipIds }),

  deselectClip: (clipId) => {
    set((state) => ({
      selectedClipIds: state.selectedClipIds.filter((id) => id !== clipId),
    }));
  },

  clearSelection: () => set({ selectedClipIds: [] }),

  selectTrack: (trackId) => set({ selectedTrackId: trackId }),

  selectAllClipsOnTrack: (trackId) => {
    const { clips } = get();
    set({ selectedClipIds: clips.filter((c) => c.trackId === trackId).map((c) => c.id) });
  },

  // Bulk operations
  deleteSelectedClips: () => {
    const { selectedClipIds, saveSnapshot } = get();
    if (selectedClipIds.length === 0) return;
    saveSnapshot('Delete clips');
    set((state) => ({
      clips: state.clips.filter((c) => !selectedClipIds.includes(c.id)),
      selectedClipIds: [],
    }));
  },

  duplicateSelectedClips: () => {
    const { selectedClipIds, clips, saveSnapshot, addClip } = get();
    if (selectedClipIds.length === 0) return [];

    saveSnapshot('Duplicate clips');
    const newIds: string[] = [];

    selectedClipIds.forEach((clipId) => {
      const clip = clips.find((c) => c.id === clipId);
      if (clip) {
        const newId = addClip({
          ...clip,
          name: `${clip.name} (copy)`,
          startTime: clip.startTime + clip.duration,
        });
        newIds.push(newId);
      }
    });

    set({ selectedClipIds: newIds });
    return newIds;
  },

  splitSelectedClips: (time) => {
    const { selectedClipIds, splitClip } = get();
    selectedClipIds.forEach((clipId) => {
      splitClip(clipId, time);
    });
  },

  // Zoom and scroll
  setZoom: (zoom) => set({ zoom: Math.max(0.1, Math.min(10, zoom)) }),
  zoomIn: () => get().setZoom(get().zoom * 1.25),
  zoomOut: () => get().setZoom(get().zoom / 1.25),

  fitToWindow: (containerWidth, totalDuration) => {
    if (totalDuration > 0 && containerWidth > 0) {
      const zoom = containerWidth / (totalDuration * 100);
      get().setZoom(zoom);
    }
  },

  setScrollPosition: (position) => set({ scrollPosition: Math.max(0, position) }),

  // Snap and ripple
  setSnapEnabled: (enabled) => set({ snapEnabled: enabled }),
  setRippleEnabled: (enabled) => set({ rippleEnabled: enabled }),

  // Undo/Redo
  saveSnapshot: (description) => {
    const { tracks, clips, undoStack, maxHistorySize } = get();
    const snapshot: TimelineSnapshot = {
      tracks: JSON.parse(JSON.stringify(tracks)),
      clips: JSON.parse(JSON.stringify(clips)),
      description,
    };

    const newStack = [...undoStack, snapshot].slice(-maxHistorySize);
    set({ undoStack: newStack, redoStack: [] });
  },

  undo: () => {
    const { undoStack, redoStack, tracks, clips } = get();
    if (undoStack.length === 0) return;

    const currentSnapshot: TimelineSnapshot = {
      tracks: JSON.parse(JSON.stringify(tracks)),
      clips: JSON.parse(JSON.stringify(clips)),
      description: 'Current state',
    };

    const previousSnapshot = undoStack[undoStack.length - 1];
    set({
      tracks: previousSnapshot.tracks,
      clips: previousSnapshot.clips,
      undoStack: undoStack.slice(0, -1),
      redoStack: [...redoStack, currentSnapshot],
      selectedClipIds: [],
    });
  },

  redo: () => {
    const { undoStack, redoStack, tracks, clips } = get();
    if (redoStack.length === 0) return;

    const currentSnapshot: TimelineSnapshot = {
      tracks: JSON.parse(JSON.stringify(tracks)),
      clips: JSON.parse(JSON.stringify(clips)),
      description: 'Current state',
    };

    const nextSnapshot = redoStack[redoStack.length - 1];
    set({
      tracks: nextSnapshot.tracks,
      clips: nextSnapshot.clips,
      undoStack: [...undoStack, currentSnapshot],
      redoStack: redoStack.slice(0, -1),
      selectedClipIds: [],
    });
  },

  canUndo: () => get().undoStack.length > 0,
  canRedo: () => get().redoStack.length > 0,

  clearHistory: () => set({ undoStack: [], redoStack: [] }),

  // Getters
  getClipById: (clipId) => get().clips.find((c) => c.id === clipId),
  getTrackById: (trackId) => get().tracks.find((t) => t.id === trackId),
  getClipsOnTrack: (trackId) => get().clips.filter((c) => c.trackId === trackId),
  getSelectedClips: () => {
    const { clips, selectedClipIds } = get();
    return clips.filter((c) => selectedClipIds.includes(c.id));
  },
  getTotalDuration: () => {
    const { clips } = get();
    if (clips.length === 0) return 10;
    return Math.max(...clips.map((c) => c.startTime + c.duration), 10);
  },
}));
