/**
 * Motion Graphics Animation Utilities
 *
 * Animation helper functions including easing implementations,
 * interpolation utilities, and keyframe evaluation.
 */

import type { EasingPreset } from '../types/motionGraphics';

/**
 * Bounce out easing function - defined separately to avoid circular reference
 */
function bounceOut(t: number): number {
  const n1 = 7.5625;
  const d1 = 2.75;
  if (t < 1 / d1) {
    return n1 * t * t;
  } else if (t < 2 / d1) {
    return n1 * (t -= 1.5 / d1) * t + 0.75;
  } else if (t < 2.5 / d1) {
    return n1 * (t -= 2.25 / d1) * t + 0.9375;
  } else {
    return n1 * (t -= 2.625 / d1) * t + 0.984375;
  }
}

/**
 * Standard easing functions
 */
const easingFunctions: Record<string, (t: number) => number> = {
  // Linear
  linear: (t) => t,

  // Quadratic
  easeInQuad: (t) => t * t,
  easeOutQuad: (t) => t * (2 - t),
  easeInOutQuad: (t) => (t < 0.5 ? 2 * t * t : -1 + (4 - 2 * t) * t),

  // Cubic
  easeIn: (t) => t * t * t,
  easeInCubic: (t) => t * t * t,
  easeOut: (t) => 1 - Math.pow(1 - t, 3),
  easeOutCubic: (t) => 1 - Math.pow(1 - t, 3),
  easeInOut: (t) => (t < 0.5 ? 4 * t * t * t : 1 - Math.pow(-2 * t + 2, 3) / 2),
  easeInOutCubic: (t) => (t < 0.5 ? 4 * t * t * t : 1 - Math.pow(-2 * t + 2, 3) / 2),

  // Quartic
  easeInQuart: (t) => t * t * t * t,
  easeOutQuart: (t) => 1 - Math.pow(1 - t, 4),
  easeInOutQuart: (t) => (t < 0.5 ? 8 * t * t * t * t : 1 - Math.pow(-2 * t + 2, 4) / 2),

  // Quintic
  easeInQuint: (t) => t * t * t * t * t,
  easeOutQuint: (t) => 1 - Math.pow(1 - t, 5),
  easeInOutQuint: (t) => (t < 0.5 ? 16 * t * t * t * t * t : 1 - Math.pow(-2 * t + 2, 5) / 2),

  // Exponential
  easeInExpo: (t) => (t === 0 ? 0 : Math.pow(2, 10 * t - 10)),
  easeOutExpo: (t) => (t === 1 ? 1 : 1 - Math.pow(2, -10 * t)),
  easeInOutExpo: (t) => {
    if (t === 0) return 0;
    if (t === 1) return 1;
    return t < 0.5 ? Math.pow(2, 20 * t - 10) / 2 : (2 - Math.pow(2, -20 * t + 10)) / 2;
  },

  // Circular
  easeInCirc: (t) => 1 - Math.sqrt(1 - t * t),
  easeOutCirc: (t) => Math.sqrt(1 - Math.pow(t - 1, 2)),
  easeInOutCirc: (t) =>
    t < 0.5
      ? (1 - Math.sqrt(1 - Math.pow(2 * t, 2))) / 2
      : (Math.sqrt(1 - Math.pow(-2 * t + 2, 2)) + 1) / 2,

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

  // Elastic
  easeInElastic: (t) => {
    if (t === 0) return 0;
    if (t === 1) return 1;
    const c4 = (2 * Math.PI) / 3;
    return -Math.pow(2, 10 * t - 10) * Math.sin((t * 10 - 10.75) * c4);
  },
  easeOutElastic: (t) => {
    if (t === 0) return 0;
    if (t === 1) return 1;
    const c4 = (2 * Math.PI) / 3;
    return Math.pow(2, -10 * t) * Math.sin((t * 10 - 0.75) * c4) + 1;
  },
  easeInOutElastic: (t) => {
    if (t === 0) return 0;
    if (t === 1) return 1;
    const c5 = (2 * Math.PI) / 4.5;
    return t < 0.5
      ? -(Math.pow(2, 20 * t - 10) * Math.sin((20 * t - 11.125) * c5)) / 2
      : (Math.pow(2, -20 * t + 10) * Math.sin((20 * t - 11.125) * c5)) / 2 + 1;
  },

  // Bounce - using standalone bounceOut function to avoid circular reference
  easeInBounce: (t) => 1 - bounceOut(1 - t),
  easeOutBounce: bounceOut,
  easeInOutBounce: (t) =>
    t < 0.5 ? (1 - bounceOut(1 - 2 * t)) / 2 : (1 + bounceOut(2 * t - 1)) / 2,

  // Spring physics presets
  springGentle: (t) => {
    // Gentle spring with minimal overshoot
    const frequency = 1.5;
    const damping = 0.8;
    return 1 - Math.exp(-damping * t * 10) * Math.cos(frequency * Math.PI * t);
  },
  springBouncy: (t) => {
    // Bouncy spring with visible overshoot
    const frequency = 2.5;
    const damping = 0.6;
    return 1 - Math.exp(-damping * t * 8) * Math.cos(frequency * Math.PI * t);
  },
  springStiff: (t) => {
    // Stiff spring with quick settle
    const frequency = 3;
    const damping = 0.9;
    return 1 - Math.exp(-damping * t * 12) * Math.cos(frequency * Math.PI * t);
  },
};

/**
 * Evaluate an easing function at time t (0 to 1)
 * @param easing - Easing preset name
 * @param t - Progress value between 0 and 1
 * @returns Eased value between 0 and 1
 */
export function evaluateEasing(easing: EasingPreset | string, t: number): number {
  // Clamp t to [0, 1]
  const clampedT = Math.max(0, Math.min(1, t));

  const easingFn = easingFunctions[easing];
  if (easingFn) {
    return easingFn(clampedT);
  }

  // Default to easeOutCubic if easing not found
  return easingFunctions.easeOutCubic(clampedT);
}

/**
 * Interpolate between two values
 * @param start - Start value
 * @param end - End value
 * @param t - Progress (0 to 1)
 * @returns Interpolated value
 */
export function interpolate(start: number, end: number, t: number): number {
  return start + (end - start) * t;
}

/**
 * Interpolate between two colors
 * @param startColor - Start color in hex format (#RRGGBB)
 * @param endColor - End color in hex format (#RRGGBB)
 * @param t - Progress (0 to 1)
 * @returns Interpolated color in hex format
 */
export function interpolateColor(startColor: string, endColor: string, t: number): string {
  // Parse hex colors
  const parseHex = (hex: string): [number, number, number] => {
    const cleanHex = hex.replace('#', '');
    return [
      parseInt(cleanHex.substring(0, 2), 16),
      parseInt(cleanHex.substring(2, 4), 16),
      parseInt(cleanHex.substring(4, 6), 16),
    ];
  };

  const [r1, g1, b1] = parseHex(startColor);
  const [r2, g2, b2] = parseHex(endColor);

  const r = Math.round(interpolate(r1, r2, t));
  const g = Math.round(interpolate(g1, g2, t));
  const b = Math.round(interpolate(b1, b2, t));

  return `#${r.toString(16).padStart(2, '0')}${g.toString(16).padStart(2, '0')}${b.toString(16).padStart(2, '0')}`;
}

/**
 * Calculate staggered delay for an element in a sequence
 * @param index - Element index in sequence
 * @param totalCount - Total number of elements
 * @param totalDelay - Total stagger delay across all elements
 * @returns Delay for this element in seconds
 */
export function calculateStaggerDelay(
  index: number,
  totalCount: number,
  totalDelay: number
): number {
  if (totalCount <= 1) return 0;
  return (index / (totalCount - 1)) * totalDelay;
}

/**
 * Get elapsed time at 60fps frame intervals
 * @param frameCount - Number of frames elapsed
 * @returns Time in seconds
 */
export function frameToTime(frameCount: number): number {
  return frameCount / 60;
}

/**
 * Get frame count at 60fps for a given time
 * @param time - Time in seconds
 * @returns Frame count
 */
export function timeToFrame(time: number): number {
  return Math.round(time * 60);
}

/**
 * Create an animation loop callback using requestAnimationFrame
 * @param callback - Function to call each frame with elapsed time
 * @returns Object with start and stop methods
 */
export function createAnimationLoop(callback: (elapsed: number, delta: number) => void): {
  start: () => void;
  stop: () => void;
} {
  let animationId: number | null = null;
  let startTime: number | null = null;
  let lastTime: number | null = null;

  const animate = (currentTime: number) => {
    if (startTime === null) {
      startTime = currentTime;
      lastTime = currentTime;
    }

    const elapsed = (currentTime - startTime) / 1000;
    const delta = (currentTime - (lastTime ?? currentTime)) / 1000;
    lastTime = currentTime;

    callback(elapsed, delta);

    animationId = requestAnimationFrame(animate);
  };

  return {
    start: () => {
      if (animationId === null) {
        animationId = requestAnimationFrame(animate);
      }
    },
    stop: () => {
      if (animationId !== null) {
        cancelAnimationFrame(animationId);
        animationId = null;
        startTime = null;
        lastTime = null;
      }
    },
  };
}

/**
 * Bezier curve helper for custom easing
 * @param p1x - First control point X
 * @param p1y - First control point Y
 * @param p2x - Second control point X
 * @param p2y - Second control point Y
 * @returns Easing function
 */
export function cubicBezier(
  p1x: number,
  p1y: number,
  p2x: number,
  p2y: number
): (t: number) => number {
  // Attempt to find the t for a given x using Newton-Raphson
  const sampleCurveX = (t: number): number => {
    return 3 * (1 - t) * (1 - t) * t * p1x + 3 * (1 - t) * t * t * p2x + t * t * t;
  };

  const sampleCurveY = (t: number): number => {
    return 3 * (1 - t) * (1 - t) * t * p1y + 3 * (1 - t) * t * t * p2y + t * t * t;
  };

  const solveCurveX = (x: number): number => {
    let t = x;
    for (let i = 0; i < 8; i++) {
      const xEstimate = sampleCurveX(t) - x;
      if (Math.abs(xEstimate) < 0.001) return t;
      const dx =
        3 * (1 - t) * (1 - t) * p1x + 6 * (1 - t) * t * (p2x - p1x) + 3 * t * t * (1 - p2x);
      if (Math.abs(dx) < 0.000001) break;
      t -= xEstimate / dx;
    }
    return t;
  };

  return (x: number): number => {
    if (x <= 0) return 0;
    if (x >= 1) return 1;
    return sampleCurveY(solveCurveX(x));
  };
}

export default {
  evaluateEasing,
  interpolate,
  interpolateColor,
  calculateStaggerDelay,
  frameToTime,
  timeToFrame,
  createAnimationLoop,
  cubicBezier,
};
