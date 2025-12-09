/**
 * OpenCut Speed Ramp Store
 *
 * Manages speed ramping and time remapping for clips.
 * Supports constant speed changes, speed ramping with keyframes,
 * reverse playback, and freeze frames.
 */

import { create } from 'zustand';

function generateId(): string {
  return `speed-kf-${Date.now()}-${Math.random().toString(36).substring(2, 9)}`;
}

/** Easing types for speed keyframe interpolation */
export type SpeedEasing = 'linear' | 'ease-in' | 'ease-out' | 'ease-in-out';

/** Speed keyframe for ramping */
export interface SpeedKeyframe {
  id: string;
  clipId: string;
  /** Time within clip (0-1 normalized) */
  time: number;
  /** Speed at this point (0.1 to 16x) */
  speed: number;
  /** Easing curve to this keyframe */
  easing: SpeedEasing;
}

/** Speed ramp preset types */
export type SpeedRampPreset =
  | 'smooth-slow-mo'
  | 'smooth-speed-up'
  | 'dramatic-pause'
  | 'ramp-up-down'
  | 'flash'
  | 'reverse-ramp';

/** Speed ramp state */
interface SpeedRampState {
  keyframes: SpeedKeyframe[];
  /** Selected keyframe for editing */
  selectedKeyframeId: string | null;
}

/** Speed ramp actions */
interface SpeedRampActions {
  // Keyframe management
  addSpeedKeyframe: (clipId: string, time: number, speed: number) => string;
  removeSpeedKeyframe: (keyframeId: string) => void;
  updateSpeedKeyframe: (keyframeId: string, updates: Partial<SpeedKeyframe>) => void;
  clearKeyframesForClip: (clipId: string) => void;

  // Queries
  getSpeedAtTime: (clipId: string, normalizedTime: number) => number;
  getKeyframesForClip: (clipId: string) => SpeedKeyframe[];
  getKeyframeById: (keyframeId: string) => SpeedKeyframe | undefined;

  // Selection
  selectKeyframe: (keyframeId: string | null) => void;
  getSelectedKeyframe: () => SpeedKeyframe | undefined;

  // Presets
  applySpeedRampPreset: (clipId: string, preset: SpeedRampPreset) => void;

  // Utility
  interpolateSpeed: (fromSpeed: number, toSpeed: number, t: number, easing: SpeedEasing) => number;
}

export type SpeedRampStore = SpeedRampState & SpeedRampActions;

/** Easing functions for speed interpolation */
const easingFunctions: Record<SpeedEasing, (t: number) => number> = {
  linear: (t) => t,
  'ease-in': (t) => t * t,
  'ease-out': (t) => t * (2 - t),
  'ease-in-out': (t) => (t < 0.5 ? 2 * t * t : -1 + (4 - 2 * t) * t),
};

/** Speed ramp preset configurations */
const presetConfigurations: Record<SpeedRampPreset, { time: number; speed: number }[]> = {
  'smooth-slow-mo': [
    { time: 0, speed: 1 },
    { time: 0.3, speed: 0.25 },
    { time: 0.7, speed: 0.25 },
    { time: 1, speed: 1 },
  ],
  'smooth-speed-up': [
    { time: 0, speed: 1 },
    { time: 0.5, speed: 2 },
    { time: 1, speed: 4 },
  ],
  'dramatic-pause': [
    { time: 0, speed: 2 },
    { time: 0.4, speed: 0.1 },
    { time: 0.6, speed: 0.1 },
    { time: 1, speed: 2 },
  ],
  'ramp-up-down': [
    { time: 0, speed: 0.5 },
    { time: 0.5, speed: 3 },
    { time: 1, speed: 0.5 },
  ],
  flash: [
    { time: 0, speed: 1 },
    { time: 0.45, speed: 1 },
    { time: 0.5, speed: 8 },
    { time: 0.55, speed: 1 },
    { time: 1, speed: 1 },
  ],
  'reverse-ramp': [
    { time: 0, speed: 1 },
    { time: 0.5, speed: -1 },
    { time: 1, speed: 1 },
  ],
};

export const useSpeedRampStore = create<SpeedRampStore>((set, get) => ({
  keyframes: [],
  selectedKeyframeId: null,

  addSpeedKeyframe: (clipId, time, speed) => {
    const id = generateId();
    const keyframe: SpeedKeyframe = {
      id,
      clipId,
      time: Math.max(0, Math.min(1, time)),
      speed: Math.max(0.1, Math.min(16, speed)),
      easing: 'ease-in-out',
    };

    set((state) => ({
      keyframes: [...state.keyframes, keyframe].sort((a, b) => a.time - b.time),
    }));

    return id;
  },

  removeSpeedKeyframe: (keyframeId) => {
    set((state) => ({
      keyframes: state.keyframes.filter((kf) => kf.id !== keyframeId),
      selectedKeyframeId: state.selectedKeyframeId === keyframeId ? null : state.selectedKeyframeId,
    }));
  },

  updateSpeedKeyframe: (keyframeId, updates) => {
    set((state) => ({
      keyframes: state.keyframes
        .map((kf) => {
          if (kf.id !== keyframeId) return kf;

          const updated = { ...kf, ...updates };
          // Clamp values
          if (updates.time !== undefined) {
            updated.time = Math.max(0, Math.min(1, updates.time));
          }
          if (updates.speed !== undefined) {
            updated.speed = Math.max(0.1, Math.min(16, updates.speed));
          }
          return updated;
        })
        .sort((a, b) => a.time - b.time),
    }));
  },

  clearKeyframesForClip: (clipId) => {
    set((state) => ({
      keyframes: state.keyframes.filter((kf) => kf.clipId !== clipId),
      selectedKeyframeId:
        state.keyframes.find((kf) => kf.id === state.selectedKeyframeId)?.clipId === clipId
          ? null
          : state.selectedKeyframeId,
    }));
  },

  getSpeedAtTime: (clipId, normalizedTime) => {
    const keyframes = get().getKeyframesForClip(clipId);

    if (keyframes.length === 0) return 1;
    if (keyframes.length === 1) return keyframes[0].speed;

    // Find surrounding keyframes
    let before = keyframes[0];
    let after = keyframes[keyframes.length - 1];

    for (let i = 0; i < keyframes.length - 1; i++) {
      if (normalizedTime >= keyframes[i].time && normalizedTime <= keyframes[i + 1].time) {
        before = keyframes[i];
        after = keyframes[i + 1];
        break;
      }
    }

    if (normalizedTime <= before.time) return before.speed;
    if (normalizedTime >= after.time) return after.speed;

    // Interpolate using easing
    const t = (normalizedTime - before.time) / (after.time - before.time);
    return get().interpolateSpeed(before.speed, after.speed, t, after.easing);
  },

  getKeyframesForClip: (clipId) => {
    return get().keyframes.filter((kf) => kf.clipId === clipId);
  },

  getKeyframeById: (keyframeId) => {
    return get().keyframes.find((kf) => kf.id === keyframeId);
  },

  selectKeyframe: (keyframeId) => {
    set({ selectedKeyframeId: keyframeId });
  },

  getSelectedKeyframe: () => {
    const { keyframes, selectedKeyframeId } = get();
    return keyframes.find((kf) => kf.id === selectedKeyframeId);
  },

  applySpeedRampPreset: (clipId, preset) => {
    const { clearKeyframesForClip, addSpeedKeyframe } = get();
    clearKeyframesForClip(clipId);

    const config = presetConfigurations[preset];
    config.forEach(({ time, speed }) => {
      addSpeedKeyframe(clipId, time, speed);
    });
  },

  interpolateSpeed: (fromSpeed, toSpeed, t, easing) => {
    const easedT = easingFunctions[easing](t);
    return fromSpeed + (toSpeed - fromSpeed) * easedT;
  },
}));
