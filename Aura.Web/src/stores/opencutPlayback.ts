/**
 * OpenCut Playback Store
 *
 * Manages playback state for the OpenCut editor including play/pause,
 * current time, duration, volume, and playback speed controls.
 */

import { create } from 'zustand';

export interface OpenCutPlaybackState {
  isPlaying: boolean;
  currentTime: number;
  duration: number;
  volume: number;
  muted: boolean;
  previousVolume: number;
  speed: number;
}

export interface OpenCutPlaybackActions {
  play: () => void;
  pause: () => void;
  toggle: () => void;
  seek: (time: number) => void;
  setCurrentTime: (time: number) => void;
  setDuration: (duration: number) => void;
  setVolume: (volume: number) => void;
  setSpeed: (speed: number) => void;
  mute: () => void;
  unmute: () => void;
  toggleMute: () => void;
  skipBackward: (seconds?: number) => void;
  skipForward: (seconds?: number) => void;
  goToStart: () => void;
  goToEnd: () => void;
}

export type OpenCutPlaybackStore = OpenCutPlaybackState & OpenCutPlaybackActions;

let playbackTimer: number | null = null;
let lastUpdate = 0;

const startTimer = (store: () => OpenCutPlaybackStore) => {
  if (playbackTimer) cancelAnimationFrame(playbackTimer);

  const updateTime = () => {
    const state = store();
    if (state.isPlaying && state.currentTime < state.duration) {
      const now = performance.now();
      const delta = (now - lastUpdate) / 1000;
      lastUpdate = now;

      const newTime = state.currentTime + delta * state.speed;

      if (newTime >= state.duration) {
        state.pause();
        state.setCurrentTime(state.duration);
        window.dispatchEvent(
          new CustomEvent('opencut-playback-ended', {
            detail: { time: state.duration },
          })
        );
      } else {
        state.setCurrentTime(newTime);
        window.dispatchEvent(
          new CustomEvent('opencut-playback-update', {
            detail: { time: newTime },
          })
        );
      }
    }
    playbackTimer = requestAnimationFrame(updateTime);
  };

  lastUpdate = performance.now();
  playbackTimer = requestAnimationFrame(updateTime);
};

const stopTimer = () => {
  if (playbackTimer) {
    cancelAnimationFrame(playbackTimer);
    playbackTimer = null;
  }
};

export const useOpenCutPlaybackStore = create<OpenCutPlaybackStore>((set, get) => ({
  isPlaying: false,
  currentTime: 0,
  duration: 10,
  volume: 1,
  muted: false,
  previousVolume: 1,
  speed: 1.0,

  play: () => {
    const state = get();
    if (state.currentTime >= state.duration && state.duration > 0) {
      get().seek(0);
    }
    set({ isPlaying: true });
    startTimer(get);
  },

  pause: () => {
    set({ isPlaying: false });
    stopTimer();
  },

  toggle: () => {
    const { isPlaying } = get();
    if (isPlaying) {
      get().pause();
    } else {
      get().play();
    }
  },

  seek: (time: number) => {
    const { duration } = get();
    const clampedTime = Math.max(0, Math.min(duration, time));
    set({ currentTime: clampedTime });

    window.dispatchEvent(
      new CustomEvent('opencut-playback-seek', {
        detail: { time: clampedTime },
      })
    );
  },

  setCurrentTime: (time: number) => set({ currentTime: time }),

  setDuration: (duration: number) => set({ duration: Math.max(0, duration) }),

  setVolume: (volume: number) =>
    set((state) => ({
      volume: Math.max(0, Math.min(1, volume)),
      muted: volume === 0,
      previousVolume: volume > 0 ? volume : state.previousVolume,
    })),

  setSpeed: (speed: number) => {
    const newSpeed = Math.max(0.25, Math.min(4.0, speed));
    set({ speed: newSpeed });

    window.dispatchEvent(
      new CustomEvent('opencut-playback-speed', {
        detail: { speed: newSpeed },
      })
    );
  },

  mute: () => {
    const { volume, previousVolume } = get();
    set({
      muted: true,
      previousVolume: volume > 0 ? volume : previousVolume,
      volume: 0,
    });
  },

  unmute: () => {
    const { previousVolume } = get();
    set({ muted: false, volume: previousVolume ?? 1 });
  },

  toggleMute: () => {
    const { muted } = get();
    if (muted) {
      get().unmute();
    } else {
      get().mute();
    }
  },

  skipBackward: (seconds = 5) => {
    const { currentTime, seek } = get();
    seek(Math.max(0, currentTime - seconds));
  },

  skipForward: (seconds = 5) => {
    const { currentTime, duration, seek } = get();
    seek(Math.min(duration, currentTime + seconds));
  },

  goToStart: () => {
    get().seek(0);
  },

  goToEnd: () => {
    const { duration } = get();
    get().seek(duration);
  },
}));
