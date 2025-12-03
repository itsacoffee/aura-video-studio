/**
 * OpenCut Keyframes Store
 *
 * Manages keyframe animation state including keyframe tracks, interpolation,
 * and editing operations. Supports easing curves and bezier handles for
 * professional animation workflows.
 */

import { create } from 'zustand';

/** Easing types for keyframe interpolation */
export type EasingType =
  | 'linear'
  | 'ease-in'
  | 'ease-out'
  | 'ease-in-out'
  | 'ease-in-back'
  | 'ease-out-back'
  | 'ease-in-elastic'
  | 'ease-out-elastic'
  | 'ease-in-bounce'
  | 'ease-out-bounce'
  | 'bezier'
  | 'hold';

/** Bezier control handles for custom easing curves */
export interface BezierHandles {
  x1: number;
  y1: number;
  x2: number;
  y2: number;
}

/** Individual keyframe on a track */
export interface Keyframe {
  id: string;
  time: number;
  value: number | string;
  easing: EasingType;
  bezierHandles?: BezierHandles;
}

/** A track of keyframes for a specific property on a clip */
export interface KeyframeTrack {
  id: string;
  clipId: string;
  property: string;
  keyframes: Keyframe[];
  enabled: boolean;
}

function generateId(): string {
  return `${Date.now()}-${Math.random().toString(36).substring(2, 9)}`;
}

/**
 * Easing functions for value interpolation between keyframes
 */
const easingFunctions: Record<EasingType, (t: number) => number> = {
  linear: (t) => t,
  'ease-in': (t) => t * t,
  'ease-out': (t) => t * (2 - t),
  'ease-in-out': (t) => (t < 0.5 ? 2 * t * t : -1 + (4 - 2 * t) * t),
  'ease-in-back': (t) => t * t * (2.70158 * t - 1.70158),
  'ease-out-back': (t) => {
    const t1 = t - 1;
    return 1 + t1 * t1 * (2.70158 * t1 + 1.70158);
  },
  'ease-in-elastic': (t) => {
    if (t === 0 || t === 1) return t;
    return -Math.pow(2, 10 * (t - 1)) * Math.sin((t - 1.1) * 5 * Math.PI);
  },
  'ease-out-elastic': (t) => {
    if (t === 0 || t === 1) return t;
    return Math.pow(2, -10 * t) * Math.sin((t - 0.1) * 5 * Math.PI) + 1;
  },
  'ease-in-bounce': (t) => 1 - easingFunctions['ease-out-bounce'](1 - t),
  'ease-out-bounce': (t) => {
    if (t < 1 / 2.75) return 7.5625 * t * t;
    if (t < 2 / 2.75) {
      const t1 = t - 1.5 / 2.75;
      return 7.5625 * t1 * t1 + 0.75;
    }
    if (t < 2.5 / 2.75) {
      const t1 = t - 2.25 / 2.75;
      return 7.5625 * t1 * t1 + 0.9375;
    }
    const t1 = t - 2.625 / 2.75;
    return 7.5625 * t1 * t1 + 0.984375;
  },
  bezier: (t) => t,
  hold: () => 0,
};

/**
 * Calculate cubic bezier value for custom easing curves
 */
const BEZIER_ITERATIONS = 10;

function cubicBezier(t: number, handles: BezierHandles): number {
  const { x1, y1, x2, y2 } = handles;

  // Binary search for the t value that gives us the x coordinate
  let low = 0;
  let high = 1;
  let mid = 0.5;

  for (let i = 0; i < BEZIER_ITERATIONS; i++) {
    const x =
      3 * (1 - mid) * (1 - mid) * mid * x1 + 3 * (1 - mid) * mid * mid * x2 + mid * mid * mid;
    if (x < t) {
      low = mid;
    } else {
      high = mid;
    }
    mid = (low + high) / 2;
  }

  // Calculate y value at the found t
  return 3 * (1 - mid) * (1 - mid) * mid * y1 + 3 * (1 - mid) * mid * mid * y2 + mid * mid * mid;
}

/** Keyframe store state */
interface OpenCutKeyframesState {
  tracks: KeyframeTrack[];
  selectedKeyframeIds: string[];
  copiedKeyframes: Keyframe[];
}

/** Keyframe store actions */
interface OpenCutKeyframesActions {
  // Keyframe operations
  addKeyframe: (clipId: string, property: string, time: number, value: number | string) => string;
  removeKeyframe: (keyframeId: string) => void;
  updateKeyframe: (keyframeId: string, updates: Partial<Keyframe>) => void;
  moveKeyframe: (keyframeId: string, newTime: number) => void;

  // Track operations
  getTrackForProperty: (clipId: string, property: string) => KeyframeTrack | undefined;
  getOrCreateTrack: (clipId: string, property: string) => KeyframeTrack;
  removeTrack: (trackId: string) => void;
  toggleTrackEnabled: (trackId: string) => void;
  removeTracksForClip: (clipId: string) => void;

  // Value interpolation
  getValueAtTime: (clipId: string, property: string, time: number) => number | string | undefined;

  // Selection operations
  selectKeyframe: (keyframeId: string, addToSelection?: boolean) => void;
  selectKeyframesInRange: (startTime: number, endTime: number, clipId?: string) => void;
  clearKeyframeSelection: () => void;
  getSelectedKeyframes: () => Keyframe[];

  // Clipboard operations
  copySelectedKeyframes: () => void;
  pasteKeyframes: (clipId: string, property: string, timeOffset: number) => void;
  deleteSelectedKeyframes: () => void;

  // Bulk operations
  setEasingForSelected: (easing: EasingType) => void;

  // Query operations
  getKeyframesForClip: (clipId: string) => Keyframe[];
  hasKeyframes: (clipId: string, property?: string) => boolean;
  getTracksForClip: (clipId: string) => KeyframeTrack[];
  getKeyframeAtTime: (
    clipId: string,
    property: string,
    time: number,
    tolerance?: number
  ) => Keyframe | undefined;
  getAdjacentKeyframes: (
    clipId: string,
    property: string,
    time: number
  ) => { prev?: Keyframe; next?: Keyframe };
}

export type OpenCutKeyframesStore = OpenCutKeyframesState & OpenCutKeyframesActions;

export const useOpenCutKeyframesStore = create<OpenCutKeyframesStore>((set, get) => ({
  tracks: [],
  selectedKeyframeIds: [],
  copiedKeyframes: [],

  addKeyframe: (clipId, property, time, value) => {
    const track = get().getOrCreateTrack(clipId, property);
    const id = `kf-${generateId()}`;
    const newKeyframe: Keyframe = { id, time, value, easing: 'ease-out' };

    set((state) => ({
      tracks: state.tracks.map((t) => {
        if (t.id === track.id) {
          // Check if keyframe exists at same time (within tolerance)
          const existingIndex = t.keyframes.findIndex((k) => Math.abs(k.time - time) < 0.001);
          const newKeyframes =
            existingIndex >= 0
              ? t.keyframes.map((k, i) => (i === existingIndex ? newKeyframe : k))
              : [...t.keyframes, newKeyframe].sort((a, b) => a.time - b.time);
          return { ...t, keyframes: newKeyframes };
        }
        return t;
      }),
    }));
    return id;
  },

  removeKeyframe: (keyframeId) => {
    set((state) => ({
      tracks: state.tracks
        .map((t) => ({ ...t, keyframes: t.keyframes.filter((k) => k.id !== keyframeId) }))
        .filter((t) => t.keyframes.length > 0),
      selectedKeyframeIds: state.selectedKeyframeIds.filter((id) => id !== keyframeId),
    }));
  },

  updateKeyframe: (keyframeId, updates) => {
    set((state) => ({
      tracks: state.tracks.map((t) => ({
        ...t,
        keyframes: t.keyframes.map((k) => (k.id === keyframeId ? { ...k, ...updates } : k)),
      })),
    }));
  },

  moveKeyframe: (keyframeId, newTime) => {
    set((state) => ({
      tracks: state.tracks.map((t) => ({
        ...t,
        keyframes: t.keyframes
          .map((k) => (k.id === keyframeId ? { ...k, time: Math.max(0, newTime) } : k))
          .sort((a, b) => a.time - b.time),
      })),
    }));
  },

  getTrackForProperty: (clipId, property) => {
    return get().tracks.find((t) => t.clipId === clipId && t.property === property);
  },

  getOrCreateTrack: (clipId, property) => {
    const existing = get().getTrackForProperty(clipId, property);
    if (existing) return existing;

    const id = `track-${generateId()}`;
    const newTrack: KeyframeTrack = { id, clipId, property, keyframes: [], enabled: true };
    set((state) => ({ tracks: [...state.tracks, newTrack] }));
    return get().tracks.find((t) => t.id === id)!;
  },

  removeTrack: (trackId) => {
    set((state) => ({ tracks: state.tracks.filter((t) => t.id !== trackId) }));
  },

  toggleTrackEnabled: (trackId) => {
    set((state) => ({
      tracks: state.tracks.map((t) => (t.id === trackId ? { ...t, enabled: !t.enabled } : t)),
    }));
  },

  removeTracksForClip: (clipId) => {
    set((state) => ({ tracks: state.tracks.filter((t) => t.clipId !== clipId) }));
  },

  getValueAtTime: (clipId, property, time) => {
    const track = get().getTrackForProperty(clipId, property);
    if (!track || !track.enabled || track.keyframes.length === 0) return undefined;

    const kfs = track.keyframes;
    if (time <= kfs[0].time) return kfs[0].value;
    if (time >= kfs[kfs.length - 1].time) return kfs[kfs.length - 1].value;

    for (let i = 0; i < kfs.length - 1; i++) {
      if (time >= kfs[i].time && time <= kfs[i + 1].time) {
        // Hold easing just returns the start value
        if (kfs[i].easing === 'hold') return kfs[i].value;

        // Calculate interpolation factor
        const t = (time - kfs[i].time) / (kfs[i + 1].time - kfs[i].time);

        // Apply easing
        let easedT: number;
        if (kfs[i].easing === 'bezier' && kfs[i].bezierHandles) {
          easedT = cubicBezier(t, kfs[i].bezierHandles);
        } else {
          easedT = easingFunctions[kfs[i].easing](t);
        }

        // Interpolate values
        const v1 = Number(kfs[i].value);
        const v2 = Number(kfs[i + 1].value);

        // Check if values are numbers
        if (!isNaN(v1) && !isNaN(v2)) {
          return v1 + (v2 - v1) * easedT;
        }

        // For non-numeric values, return the first value until the next keyframe
        return kfs[i].value;
      }
    }
    return undefined;
  },

  selectKeyframe: (keyframeId, addToSelection = false) => {
    set((state) => ({
      selectedKeyframeIds: addToSelection
        ? state.selectedKeyframeIds.includes(keyframeId)
          ? state.selectedKeyframeIds
          : [...state.selectedKeyframeIds, keyframeId]
        : [keyframeId],
    }));
  },

  selectKeyframesInRange: (startTime, endTime, clipId) => {
    const { tracks } = get();
    const ids: string[] = [];
    tracks.forEach((t) => {
      if (clipId && t.clipId !== clipId) return;
      t.keyframes.forEach((k) => {
        if (k.time >= startTime && k.time <= endTime) ids.push(k.id);
      });
    });
    set({ selectedKeyframeIds: ids });
  },

  clearKeyframeSelection: () => set({ selectedKeyframeIds: [] }),

  getSelectedKeyframes: () => {
    const { tracks, selectedKeyframeIds } = get();
    const keyframes: Keyframe[] = [];
    tracks.forEach((t) => {
      t.keyframes.forEach((k) => {
        if (selectedKeyframeIds.includes(k.id)) keyframes.push(k);
      });
    });
    return keyframes;
  },

  copySelectedKeyframes: () => {
    set({ copiedKeyframes: get().getSelectedKeyframes() });
  },

  pasteKeyframes: (clipId, property, timeOffset) => {
    const { copiedKeyframes, addKeyframe, updateKeyframe } = get();
    if (copiedKeyframes.length === 0) return;

    const minTime = Math.min(...copiedKeyframes.map((k) => k.time));
    copiedKeyframes.forEach((k) => {
      const newId = addKeyframe(clipId, property, k.time - minTime + timeOffset, k.value);
      // Copy easing settings
      updateKeyframe(newId, { easing: k.easing, bezierHandles: k.bezierHandles });
    });
  },

  deleteSelectedKeyframes: () => {
    const { selectedKeyframeIds, removeKeyframe } = get();
    selectedKeyframeIds.forEach(removeKeyframe);
  },

  setEasingForSelected: (easing) => {
    const { selectedKeyframeIds, updateKeyframe } = get();
    selectedKeyframeIds.forEach((id) => updateKeyframe(id, { easing }));
  },

  getKeyframesForClip: (clipId) => {
    const keyframes: Keyframe[] = [];
    get()
      .tracks.filter((t) => t.clipId === clipId)
      .forEach((t) => keyframes.push(...t.keyframes));
    return keyframes;
  },

  hasKeyframes: (clipId, property) => {
    const tracks = get().tracks.filter((t) => t.clipId === clipId);
    if (property) return tracks.some((t) => t.property === property && t.keyframes.length > 0);
    return tracks.some((t) => t.keyframes.length > 0);
  },

  getTracksForClip: (clipId) => get().tracks.filter((t) => t.clipId === clipId),

  getKeyframeAtTime: (clipId, property, time, tolerance = 0.05) => {
    const track = get().getTrackForProperty(clipId, property);
    if (!track) return undefined;
    return track.keyframes.find((k) => Math.abs(k.time - time) <= tolerance);
  },

  getAdjacentKeyframes: (clipId, property, time) => {
    const track = get().getTrackForProperty(clipId, property);
    if (!track || track.keyframes.length === 0) return {};

    let prev: Keyframe | undefined;
    let next: Keyframe | undefined;

    for (const k of track.keyframes) {
      if (k.time < time) {
        prev = k;
      } else if (k.time > time && !next) {
        next = k;
        break;
      }
    }

    return { prev, next };
  },
}));
