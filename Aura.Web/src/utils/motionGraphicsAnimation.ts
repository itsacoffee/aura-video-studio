/**
 * Motion Graphics Animation Utilities
 *
 * Animation helper functions including easing implementations,
 * interpolation utilities, and animation state management.
 * Supports spring physics and 60fps timing.
 */

import type { EasingPreset, AnimationKeyframe, AnimationProperty } from '../types/motionGraphics';

/**
 * Easing function type
 */
type EasingFunction = (t: number) => number;

/**
 * Standard easing functions
 */
export const easingFunctions: Record<EasingPreset, EasingFunction> = {
  // Linear
  linear: (t) => t,

  // Basic easing
  easeIn: (t) => t * t,
  easeOut: (t) => t * (2 - t),
  easeInOut: (t) => (t < 0.5 ? 2 * t * t : -1 + (4 - 2 * t) * t),

  // Quadratic
  easeInQuad: (t) => t * t,
  easeOutQuad: (t) => t * (2 - t),
  easeInOutQuad: (t) => (t < 0.5 ? 2 * t * t : -1 + (4 - 2 * t) * t),

  // Cubic
  easeInCubic: (t) => t * t * t,
  easeOutCubic: (t) => {
    const t1 = t - 1;
    return t1 * t1 * t1 + 1;
  },
  easeInOutCubic: (t) => (t < 0.5 ? 4 * t * t * t : (t - 1) * (2 * t - 2) * (2 * t - 2) + 1),

  // Quartic
  easeInQuart: (t) => t * t * t * t,
  easeOutQuart: (t) => {
    const t1 = t - 1;
    return 1 - t1 * t1 * t1 * t1;
  },
  easeInOutQuart: (t) => {
    const t1 = t - 1;
    return t < 0.5 ? 8 * t * t * t * t : 1 - 8 * t1 * t1 * t1 * t1;
  },

  // Quintic
  easeInQuint: (t) => t * t * t * t * t,
  easeOutQuint: (t) => {
    const t1 = t - 1;
    return 1 + t1 * t1 * t1 * t1 * t1;
  },
  easeInOutQuint: (t) => {
    const t1 = t - 1;
    return t < 0.5 ? 16 * t * t * t * t * t : 1 + 16 * t1 * t1 * t1 * t1 * t1;
  },

  // Exponential
  easeInExpo: (t) => (t === 0 ? 0 : Math.pow(2, 10 * (t - 1))),
  easeOutExpo: (t) => (t === 1 ? 1 : 1 - Math.pow(2, -10 * t)),
  easeInOutExpo: (t) => {
    if (t === 0) return 0;
    if (t === 1) return 1;
    if (t < 0.5) return Math.pow(2, 20 * t - 10) / 2;
    return (2 - Math.pow(2, -20 * t + 10)) / 2;
  },

  // Circular
  easeInCirc: (t) => 1 - Math.sqrt(1 - t * t),
  easeOutCirc: (t) => Math.sqrt(1 - (t - 1) * (t - 1)),
  easeInOutCirc: (t) =>
    t < 0.5 ? (1 - Math.sqrt(1 - 4 * t * t)) / 2 : (Math.sqrt(1 - Math.pow(-2 * t + 2, 2)) + 1) / 2,

  // Back (overshoot)
  easeInBack: (t) => {
    const c1 = 1.70158;
    const c3 = c1 + 1;
    return c3 * t * t * t - c1 * t * t;
  },
  easeOutBack: (t) => {
    const c1 = 1.70158;
    const c3 = c1 + 1;
    return 1 + c3 * Math.pow(t - 1, 3) + c1 * Math.pow(t - 1, 2);
  },
  easeInOutBack: (t) => {
    const c1 = 1.70158;
    const c2 = c1 * 1.525;
    return t < 0.5
      ? (Math.pow(2 * t, 2) * ((c2 + 1) * 2 * t - c2)) / 2
      : (Math.pow(2 * t - 2, 2) * ((c2 + 1) * (t * 2 - 2) + c2) + 2) / 2;
  },

  // Spring physics
  springGentle: (t) => {
    const c4 = (2 * Math.PI) / 3;
    return t === 0 ? 0 : t === 1 ? 1 : Math.pow(2, -8 * t) * Math.sin((t * 10 - 0.75) * c4) + 1;
  },
  springBouncy: (t) => {
    const c4 = (2 * Math.PI) / 2.5;
    return t === 0 ? 0 : t === 1 ? 1 : Math.pow(2, -10 * t) * Math.sin((t * 10 - 0.75) * c4) + 1;
  },
  springStiff: (t) => {
    const c4 = (2 * Math.PI) / 4;
    return t === 0 ? 0 : t === 1 ? 1 : Math.pow(2, -12 * t) * Math.sin((t * 10 - 0.75) * c4) + 1;
  },
};

/**
 * Get easing function by name
 */
export function getEasingFunction(easing: EasingPreset): EasingFunction {
  return easingFunctions[easing] || easingFunctions.linear;
}

/**
 * Apply easing to a value
 */
export function applyEasing(t: number, easing: EasingPreset): number {
  const clampedT = Math.max(0, Math.min(1, t));
  return getEasingFunction(easing)(clampedT);
}

/**
 * Interpolate between two values
 */
export function lerp(start: number, end: number, t: number): number {
  return start + (end - start) * t;
}

/**
 * Evaluate keyframes at a given time
 */
export function evaluateKeyframes(
  keyframes: AnimationKeyframe[],
  time: number,
  defaultEasing: EasingPreset = 'linear'
): number {
  if (keyframes.length === 0) return 0;
  if (keyframes.length === 1) return keyframes[0].value;

  // Sort keyframes by time
  const sorted = [...keyframes].sort((a, b) => a.time - b.time);

  // Before first keyframe
  if (time <= sorted[0].time) return sorted[0].value;

  // After last keyframe
  if (time >= sorted[sorted.length - 1].time) return sorted[sorted.length - 1].value;

  // Find surrounding keyframes
  let prevKeyframe = sorted[0];
  let nextKeyframe = sorted[1];

  for (let i = 1; i < sorted.length; i++) {
    if (sorted[i].time >= time) {
      prevKeyframe = sorted[i - 1];
      nextKeyframe = sorted[i];
      break;
    }
  }

  // Calculate progress between keyframes
  const duration = nextKeyframe.time - prevKeyframe.time;
  const progress = duration > 0 ? (time - prevKeyframe.time) / duration : 0;

  // Apply easing
  const easing = prevKeyframe.easing || defaultEasing;
  const easedProgress = applyEasing(progress, easing);

  // Interpolate value
  return lerp(prevKeyframe.value, nextKeyframe.value, easedProgress);
}

/**
 * Evaluate an animation property at a given time
 */
export function evaluateAnimationProperty(property: AnimationProperty, time: number): number {
  return evaluateKeyframes(property.keyframes, time);
}

/**
 * Calculate stagger delay for a specific element index
 */
export function calculateStaggerDelay(
  index: number,
  totalElements: number,
  staggerDuration: number,
  direction: 'forward' | 'reverse' | 'center' = 'forward'
): number {
  if (totalElements <= 1) return 0;

  const delayPerElement = staggerDuration / (totalElements - 1);

  switch (direction) {
    case 'reverse':
      return delayPerElement * (totalElements - 1 - index);
    case 'center': {
      const center = (totalElements - 1) / 2;
      return Math.abs(index - center) * delayPerElement;
    }
    case 'forward':
    default:
      return delayPerElement * index;
  }
}

/**
 * Animation state machine states
 */
export type AnimationPhase = 'idle' | 'enter' | 'hold' | 'exit' | 'complete';

/**
 * Animation state
 */
export interface AnimationState {
  phase: AnimationPhase;
  progress: number; // 0-1 within current phase
  startTime: number;
  currentTime: number;
}

/**
 * Create initial animation state
 */
export function createAnimationState(): AnimationState {
  return {
    phase: 'idle',
    progress: 0,
    startTime: 0,
    currentTime: 0,
  };
}

/**
 * Update animation state based on current time
 */
export function updateAnimationState(
  state: AnimationState,
  currentTime: number,
  enterDuration: number,
  holdDuration: number,
  exitDuration: number
): AnimationState {
  const elapsed = currentTime - state.startTime;
  const totalDuration = enterDuration + holdDuration + exitDuration;

  // Not started yet
  if (elapsed < 0) {
    return { ...state, phase: 'idle', progress: 0, currentTime };
  }

  // Enter phase
  if (elapsed < enterDuration) {
    return {
      ...state,
      phase: 'enter',
      progress: enterDuration > 0 ? elapsed / enterDuration : 1,
      currentTime,
    };
  }

  // Hold phase
  if (elapsed < enterDuration + holdDuration) {
    const holdElapsed = elapsed - enterDuration;
    return {
      ...state,
      phase: 'hold',
      progress: holdDuration > 0 ? holdElapsed / holdDuration : 1,
      currentTime,
    };
  }

  // Exit phase
  if (elapsed < totalDuration) {
    const exitElapsed = elapsed - enterDuration - holdDuration;
    return {
      ...state,
      phase: 'exit',
      progress: exitDuration > 0 ? exitElapsed / exitDuration : 1,
      currentTime,
    };
  }

  // Complete
  return { ...state, phase: 'complete', progress: 1, currentTime };
}

/**
 * 60fps timing utilities
 */
export const FRAME_DURATION = 1000 / 60; // ~16.67ms

/**
 * Get frame-aligned time
 */
export function getFrameAlignedTime(time: number, fps: number = 60): number {
  const frameDuration = 1 / fps;
  return Math.round(time / frameDuration) * frameDuration;
}

/**
 * Convert time to frame number
 */
export function timeToFrame(time: number, fps: number = 60): number {
  return Math.floor(time * fps);
}

/**
 * Convert frame number to time
 */
export function frameToTime(frame: number, fps: number = 60): number {
  return frame / fps;
}

/**
 * Request animation frame wrapper with timing
 */
export function createAnimationLoop(
  callback: (time: number, deltaTime: number) => void | boolean,
  fps: number = 60
): { start: () => void; stop: () => void } {
  let animationFrameId: number | null = null;
  let lastTime: number = 0;
  let running = false;

  const frameDuration = 1000 / fps;

  const loop = (timestamp: number) => {
    if (!running) return;

    const deltaTime = timestamp - lastTime;

    // Only update if enough time has passed (for frame rate limiting)
    if (deltaTime >= frameDuration * 0.9) {
      const result = callback(timestamp / 1000, deltaTime / 1000);
      lastTime = timestamp;

      // Stop if callback returns false
      if (result === false) {
        running = false;
        return;
      }
    }

    animationFrameId = requestAnimationFrame(loop);
  };

  return {
    start: () => {
      if (running) return;
      running = true;
      lastTime = performance.now();
      animationFrameId = requestAnimationFrame(loop);
    },
    stop: () => {
      running = false;
      if (animationFrameId !== null) {
        cancelAnimationFrame(animationFrameId);
        animationFrameId = null;
      }
    },
  };
}

/**
 * Color interpolation utilities
 */

/**
 * Parse hex color to RGB
 */
export function hexToRgb(hex: string): { r: number; g: number; b: number } | null {
  const result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);
  return result
    ? {
        r: parseInt(result[1], 16),
        g: parseInt(result[2], 16),
        b: parseInt(result[3], 16),
      }
    : null;
}

/**
 * Convert RGB to hex
 */
export function rgbToHex(r: number, g: number, b: number): string {
  return '#' + [r, g, b].map((x) => Math.round(x).toString(16).padStart(2, '0')).join('');
}

/**
 * Interpolate between two colors
 */
export function lerpColor(color1: string, color2: string, t: number): string {
  const rgb1 = hexToRgb(color1);
  const rgb2 = hexToRgb(color2);

  if (!rgb1 || !rgb2) return color1;

  const r = lerp(rgb1.r, rgb2.r, t);
  const g = lerp(rgb1.g, rgb2.g, t);
  const b = lerp(rgb1.b, rgb2.b, t);

  return rgbToHex(r, g, b);
}

/**
 * Transform interpolation
 */
export interface TransformValues {
  x: number;
  y: number;
  scaleX: number;
  scaleY: number;
  rotation: number;
  opacity: number;
}

/**
 * Interpolate between two transform states
 */
export function lerpTransform(
  from: TransformValues,
  to: TransformValues,
  t: number,
  easing: EasingPreset = 'linear'
): TransformValues {
  const easedT = applyEasing(t, easing);

  return {
    x: lerp(from.x, to.x, easedT),
    y: lerp(from.y, to.y, easedT),
    scaleX: lerp(from.scaleX, to.scaleX, easedT),
    scaleY: lerp(from.scaleY, to.scaleY, easedT),
    rotation: lerp(from.rotation, to.rotation, easedT),
    opacity: lerp(from.opacity, to.opacity, easedT),
  };
}

/**
 * Get default transform values for different animation styles
 */
export function getAnimationStartTransform(
  style: string,
  direction: string = 'left',
  _blur: number = 0,
  scale: number = 1,
  _rotation: number = 0
): TransformValues {
  const baseTransform: TransformValues = {
    x: 0,
    y: 0,
    scaleX: 100,
    scaleY: 100,
    rotation: 0,
    opacity: 100,
  };

  switch (style) {
    case 'slide':
      switch (direction) {
        case 'left':
          return { ...baseTransform, x: -100, opacity: 0 };
        case 'right':
          return { ...baseTransform, x: 100, opacity: 0 };
        case 'up':
          return { ...baseTransform, y: -100, opacity: 0 };
        case 'down':
          return { ...baseTransform, y: 100, opacity: 0 };
        default:
          return { ...baseTransform, x: -100, opacity: 0 };
      }
    case 'fade':
      return { ...baseTransform, opacity: 0 };
    case 'scale':
      return { ...baseTransform, scaleX: scale * 100, scaleY: scale * 100, opacity: 0 };
    case 'blur':
      return { ...baseTransform, opacity: 0 };
    case 'bounce':
      return { ...baseTransform, y: 50, opacity: 0 };
    case 'elastic':
      return { ...baseTransform, scaleX: 0, scaleY: 0, opacity: 0 };
    case 'spring':
      return { ...baseTransform, y: -30, opacity: 0 };
    case 'reveal':
      return { ...baseTransform, opacity: 0 };
    default:
      return { ...baseTransform, opacity: 0 };
  }
}
