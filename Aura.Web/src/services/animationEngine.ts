/**
 * Animation Engine Service
 * Core animation system for keyframes, easing, and motion paths
 */

import { Keyframe } from '../types/effects';

// Easing function types
export type EasingFunction = (t: number) => number;

// Easing functions
export const Easing = {
  linear: (t: number): number => t,

  easeIn: (t: number): number => t * t,

  easeOut: (t: number): number => t * (2 - t),

  easeInOut: (t: number): number => {
    if (t < 0.5) return 2 * t * t;
    return -1 + (4 - 2 * t) * t;
  },

  bezier: (p1: number, _p2: number, p3: number, _p4: number): EasingFunction => {
    return (t: number): number => {
      // Cubic bezier easing
      const t2 = t * t;
      const t3 = t2 * t;
      const mt = 1 - t;
      const mt2 = mt * mt;
      const mt3 = mt2 * mt;

      return mt3 * 0 + 3 * mt2 * t * p1 + 3 * mt * t2 * p3 + t3 * 1;
    };
  },
};

// Get easing function from keyframe
export function getEasingFunction(keyframe: Keyframe): EasingFunction {
  const easingType = keyframe.easing || 'linear';

  switch (easingType) {
    case 'ease-in':
      return Easing.easeIn;
    case 'ease-out':
      return Easing.easeOut;
    case 'ease-in-out':
      return Easing.easeInOut;
    case 'bezier':
      if (keyframe.bezier) {
        const [p1, p2, p3, p4] = keyframe.bezier;
        return Easing.bezier(p1, p2, p3, p4);
      }
      return Easing.linear;
    case 'linear':
    default:
      return Easing.linear;
  }
}

// Interpolate between two values using easing
export function interpolate(
  startValue: number,
  endValue: number,
  progress: number,
  easingFn: EasingFunction = Easing.linear
): number {
  const t = easingFn(progress);
  return startValue + (endValue - startValue) * t;
}

// Evaluate keyframes at a specific time
export function evaluateKeyframes(keyframes: Keyframe[], time: number): number | string | boolean {
  if (keyframes.length === 0) {
    return 0;
  }

  // Sort keyframes by time
  const sorted = [...keyframes].sort((a, b) => a.time - b.time);

  // Before first keyframe
  if (time <= sorted[0].time) {
    return sorted[0].value;
  }

  // After last keyframe
  if (time >= sorted[sorted.length - 1].time) {
    return sorted[sorted.length - 1].value;
  }

  // Find the two keyframes to interpolate between
  for (let i = 0; i < sorted.length - 1; i++) {
    const current = sorted[i];
    const next = sorted[i + 1];

    if (time >= current.time && time <= next.time) {
      // For non-numeric values, use step interpolation
      if (typeof current.value !== 'number' || typeof next.value !== 'number') {
        return current.value;
      }

      // Calculate interpolation progress
      const duration = next.time - current.time;
      const elapsed = time - current.time;
      const progress = duration > 0 ? elapsed / duration : 0;

      // Get easing function from the current keyframe
      const easingFn = getEasingFunction(current);

      // Interpolate
      return interpolate(current.value, next.value, progress, easingFn);
    }
  }

  return sorted[0].value;
}

// Motion path point
export interface MotionPathPoint {
  x: number;
  y: number;
  time: number;
  // Optional tangent handles for bezier curves
  handleIn?: { x: number; y: number };
  handleOut?: { x: number; y: number };
}

// Motion path
export interface MotionPath {
  id: string;
  points: MotionPathPoint[];
  closed?: boolean;
  autoOrient?: boolean; // Automatically orient object along path
}

// Evaluate position along a motion path at a specific time
// Helper function to perform bezier interpolation between two points
function interpolateBezier(
  current: MotionPathPoint,
  next: MotionPathPoint,
  t: number,
  autoOrient: boolean
): { x: number; y: number; rotation?: number } {
  const x = cubicBezier(
    current.x,
    current.x + current.handleOut!.x,
    next.x + next.handleIn!.x,
    next.x,
    t
  );
  const y = cubicBezier(
    current.y,
    current.y + current.handleOut!.y,
    next.y + next.handleIn!.y,
    next.y,
    t
  );

  const rotation = autoOrient
    ? calculateTangentAngle(current, next, current.handleOut!, next.handleIn!, t)
    : undefined;

  return { x, y, rotation };
}

// Helper function to perform linear interpolation between two points
function interpolateLinear(
  current: MotionPathPoint,
  next: MotionPathPoint,
  t: number,
  autoOrient: boolean
): { x: number; y: number; rotation?: number } {
  const x = current.x + (next.x - current.x) * t;
  const y = current.y + (next.y - current.y) * t;

  const rotation = autoOrient
    ? Math.atan2(next.y - current.y, next.x - current.x) * (180 / Math.PI)
    : undefined;

  return { x, y, rotation };
}

export function evaluateMotionPath(
  path: MotionPath,
  time: number
): { x: number; y: number; rotation?: number } {
  if (path.points.length === 0) {
    return { x: 0, y: 0 };
  }

  const sorted = [...path.points].sort((a, b) => a.time - b.time);

  // Before first point
  if (time <= sorted[0].time) {
    return { x: sorted[0].x, y: sorted[0].y };
  }

  // After last point
  if (time >= sorted[sorted.length - 1].time) {
    const last = sorted[sorted.length - 1];
    return { x: last.x, y: last.y };
  }

  // Find the two points to interpolate between
  for (let i = 0; i < sorted.length - 1; i++) {
    const current = sorted[i];
    const next = sorted[i + 1];

    if (time >= current.time && time <= next.time) {
      const duration = next.time - current.time;
      const elapsed = time - current.time;
      const t = duration > 0 ? elapsed / duration : 0;

      // Use bezier interpolation if handles exist, otherwise linear
      if (current.handleOut && next.handleIn) {
        return interpolateBezier(current, next, t, path.autoOrient || false);
      }
      return interpolateLinear(current, next, t, path.autoOrient || false);
    }
  }

  return { x: sorted[0].x, y: sorted[0].y };
}

// Cubic bezier interpolation
function cubicBezier(p0: number, p1: number, p2: number, p3: number, t: number): number {
  const t2 = t * t;
  const t3 = t2 * t;
  const mt = 1 - t;
  const mt2 = mt * mt;
  const mt3 = mt2 * mt;

  return mt3 * p0 + 3 * mt2 * t * p1 + 3 * mt * t2 * p2 + t3 * p3;
}

// Calculate tangent angle for auto-orientation
function calculateTangentAngle(
  p0: MotionPathPoint,
  p3: MotionPathPoint,
  handleOut: { x: number; y: number },
  handleIn: { x: number; y: number },
  t: number
): number {
  // Derivative of cubic bezier
  const p1x = p0.x + handleOut.x;
  const p1y = p0.y + handleOut.y;
  const p2x = p3.x + handleIn.x;
  const p2y = p3.y + handleIn.y;

  const mt = 1 - t;
  const dx = 3 * mt * mt * (p1x - p0.x) + 6 * mt * t * (p2x - p1x) + 3 * t * t * (p3.x - p2x);
  const dy = 3 * mt * mt * (p1y - p0.y) + 6 * mt * t * (p2y - p1y) + 3 * t * t * (p3.y - p2y);

  return Math.atan2(dy, dx) * (180 / Math.PI);
}

// Layer parenting system
export interface LayerParent {
  layerId: string;
  parentId: string | null;
}

export interface TransformProperties {
  x: number;
  y: number;
  scaleX: number;
  scaleY: number;
  rotation: number;
  opacity: number;
}

// Calculate world transform by combining parent transforms
export function calculateWorldTransform(
  layerId: string,
  localTransform: TransformProperties,
  parentMap: Map<string, LayerParent>,
  transformMap: Map<string, TransformProperties>
): TransformProperties {
  const parent = parentMap.get(layerId);

  if (!parent || !parent.parentId) {
    return localTransform;
  }

  const parentTransform = transformMap.get(parent.parentId);
  if (!parentTransform) {
    return localTransform;
  }

  // Recursively get parent's world transform
  const parentWorld = calculateWorldTransform(
    parent.parentId,
    parentTransform,
    parentMap,
    transformMap
  );

  // Combine transforms
  const worldX = parentWorld.x + localTransform.x * parentWorld.scaleX;
  const worldY = parentWorld.y + localTransform.y * parentWorld.scaleY;
  const worldScaleX = parentWorld.scaleX * localTransform.scaleX;
  const worldScaleY = parentWorld.scaleY * localTransform.scaleY;
  const worldRotation = parentWorld.rotation + localTransform.rotation;
  const worldOpacity = parentWorld.opacity * localTransform.opacity;

  return {
    x: worldX,
    y: worldY,
    scaleX: worldScaleX,
    scaleY: worldScaleY,
    rotation: worldRotation,
    opacity: worldOpacity,
  };
}

// Animation utilities
export const AnimationUtils = {
  // Create a keyframe
  createKeyframe: (
    time: number,
    value: number | string | boolean,
    easing: Keyframe['easing'] = 'linear'
  ): Keyframe => ({
    time,
    value,
    easing,
  }),

  // Add keyframe to array, maintaining time order
  addKeyframe: (keyframes: Keyframe[], newKeyframe: Keyframe): Keyframe[] => {
    const filtered = keyframes.filter((k) => k.time !== newKeyframe.time);
    return [...filtered, newKeyframe].sort((a, b) => a.time - b.time);
  },

  // Remove keyframe at specific time
  removeKeyframe: (keyframes: Keyframe[], time: number): Keyframe[] => {
    return keyframes.filter((k) => k.time !== time);
  },

  // Update keyframe value
  updateKeyframe: (keyframes: Keyframe[], time: number, updates: Partial<Keyframe>): Keyframe[] => {
    return keyframes.map((k) => (k.time === time ? { ...k, ...updates } : k));
  },
};
