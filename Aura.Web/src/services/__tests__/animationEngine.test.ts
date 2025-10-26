/**
 * Animation Engine Tests
 * Test easing functions, keyframe evaluation, and motion paths
 */

import { describe, it, expect } from 'vitest';
import type { Keyframe } from '../../types/effects';
import {
  Easing,
  getEasingFunction,
  interpolate,
  evaluateKeyframes,
  evaluateMotionPath,
  calculateWorldTransform,
  AnimationUtils,
} from '../animationEngine';
import type {
  MotionPath,
  MotionPathPoint,
  TransformProperties,
  LayerParent,
} from '../animationEngine';

describe('Animation Engine', () => {
  describe('Easing Functions', () => {
    it('should return linear easing', () => {
      expect(Easing.linear(0)).toBe(0);
      expect(Easing.linear(0.5)).toBe(0.5);
      expect(Easing.linear(1)).toBe(1);
    });

    it('should return ease-in easing', () => {
      expect(Easing.easeIn(0)).toBe(0);
      expect(Easing.easeIn(0.5)).toBe(0.25);
      expect(Easing.easeIn(1)).toBe(1);
    });

    it('should return ease-out easing', () => {
      expect(Easing.easeOut(0)).toBe(0);
      expect(Easing.easeOut(1)).toBe(1);
      const mid = Easing.easeOut(0.5);
      expect(mid).toBeGreaterThan(0.5);
      expect(mid).toBeLessThan(1);
    });

    it('should return ease-in-out easing', () => {
      expect(Easing.easeInOut(0)).toBe(0);
      expect(Easing.easeInOut(0.25)).toBeLessThan(0.25);
      expect(Easing.easeInOut(0.75)).toBeGreaterThan(0.75);
      expect(Easing.easeInOut(1)).toBe(1);
    });
  });

  describe('getEasingFunction', () => {
    it('should return linear easing by default', () => {
      const keyframe: Keyframe = { time: 0, value: 0 };
      const easingFn = getEasingFunction(keyframe);
      expect(easingFn(0.5)).toBe(0.5);
    });

    it('should return ease-in easing', () => {
      const keyframe: Keyframe = { time: 0, value: 0, easing: 'ease-in' };
      const easingFn = getEasingFunction(keyframe);
      expect(easingFn(0.5)).toBe(0.25);
    });

    it('should return ease-out easing', () => {
      const keyframe: Keyframe = { time: 0, value: 0, easing: 'ease-out' };
      const easingFn = getEasingFunction(keyframe);
      expect(easingFn(0.5)).toBeGreaterThan(0.5);
    });
  });

  describe('interpolate', () => {
    it('should interpolate between two values linearly', () => {
      expect(interpolate(0, 100, 0)).toBe(0);
      expect(interpolate(0, 100, 0.5)).toBe(50);
      expect(interpolate(0, 100, 1)).toBe(100);
    });

    it('should interpolate with custom easing', () => {
      const result = interpolate(0, 100, 0.5, Easing.easeIn);
      expect(result).toBe(25);
    });
  });

  describe('evaluateKeyframes', () => {
    it('should return 0 for empty keyframes', () => {
      expect(evaluateKeyframes([], 0)).toBe(0);
    });

    it('should return first value when time is before first keyframe', () => {
      const keyframes: Keyframe[] = [
        { time: 1, value: 10 },
        { time: 2, value: 20 },
      ];
      expect(evaluateKeyframes(keyframes, 0)).toBe(10);
    });

    it('should return last value when time is after last keyframe', () => {
      const keyframes: Keyframe[] = [
        { time: 1, value: 10 },
        { time: 2, value: 20 },
      ];
      expect(evaluateKeyframes(keyframes, 3)).toBe(20);
    });

    it('should interpolate between keyframes', () => {
      const keyframes: Keyframe[] = [
        { time: 0, value: 0 },
        { time: 2, value: 100 },
      ];
      expect(evaluateKeyframes(keyframes, 1)).toBe(50);
    });

    it('should handle non-numeric values with step interpolation', () => {
      const keyframes: Keyframe[] = [
        { time: 0, value: 'red' },
        { time: 2, value: 'blue' },
      ];
      expect(evaluateKeyframes(keyframes, 1)).toBe('red');
      expect(evaluateKeyframes(keyframes, 2)).toBe('blue');
    });
  });

  describe('evaluateMotionPath', () => {
    it('should return first point when time is before first point', () => {
      const path: MotionPath = {
        id: 'path1',
        points: [
          { x: 0, y: 0, time: 1 },
          { x: 100, y: 100, time: 2 },
        ],
      };
      const result = evaluateMotionPath(path, 0);
      expect(result).toEqual({ x: 0, y: 0 });
    });

    it('should return last point when time is after last point', () => {
      const path: MotionPath = {
        id: 'path1',
        points: [
          { x: 0, y: 0, time: 1 },
          { x: 100, y: 100, time: 2 },
        ],
      };
      const result = evaluateMotionPath(path, 3);
      expect(result).toEqual({ x: 100, y: 100 });
    });

    it('should interpolate position along path', () => {
      const path: MotionPath = {
        id: 'path1',
        points: [
          { x: 0, y: 0, time: 0 },
          { x: 100, y: 100, time: 2 },
        ],
      };
      const result = evaluateMotionPath(path, 1);
      expect(result.x).toBe(50);
      expect(result.y).toBe(50);
    });

    it('should calculate rotation for auto-orient', () => {
      const path: MotionPath = {
        id: 'path1',
        points: [
          { x: 0, y: 0, time: 0 },
          { x: 100, y: 0, time: 1 },
        ],
        autoOrient: true,
      };
      const result = evaluateMotionPath(path, 0.5);
      expect(result.rotation).toBeDefined();
      expect(result.rotation).toBe(0); // Moving right = 0 degrees
    });
  });

  describe('calculateWorldTransform', () => {
    it('should return local transform when no parent', () => {
      const local: TransformProperties = {
        x: 10,
        y: 20,
        scaleX: 1,
        scaleY: 1,
        rotation: 0,
        opacity: 1,
      };
      const parentMap = new Map<string, LayerParent>();
      const transformMap = new Map<string, TransformProperties>();

      const result = calculateWorldTransform('layer1', local, parentMap, transformMap);
      expect(result).toEqual(local);
    });

    it('should combine parent and child transforms', () => {
      const parentTransform: TransformProperties = {
        x: 100,
        y: 100,
        scaleX: 2,
        scaleY: 2,
        rotation: 0,
        opacity: 0.5,
      };
      const childTransform: TransformProperties = {
        x: 10,
        y: 10,
        scaleX: 1,
        scaleY: 1,
        rotation: 45,
        opacity: 0.8,
      };

      const parentMap = new Map<string, LayerParent>([
        ['child1', { layerId: 'child1', parentId: 'parent1' }],
      ]);
      const transformMap = new Map<string, TransformProperties>([['parent1', parentTransform]]);

      const result = calculateWorldTransform('child1', childTransform, parentMap, transformMap);

      expect(result.x).toBe(120); // 100 + 10 * 2
      expect(result.y).toBe(120); // 100 + 10 * 2
      expect(result.scaleX).toBe(2); // 2 * 1
      expect(result.scaleY).toBe(2); // 2 * 1
      expect(result.rotation).toBe(45); // 0 + 45
      expect(result.opacity).toBe(0.4); // 0.5 * 0.8
    });
  });

  describe('AnimationUtils', () => {
    it('should create keyframe', () => {
      const keyframe = AnimationUtils.createKeyframe(1, 100, 'ease-in');
      expect(keyframe).toEqual({
        time: 1,
        value: 100,
        easing: 'ease-in',
      });
    });

    it('should add keyframe and maintain time order', () => {
      const keyframes: Keyframe[] = [
        { time: 0, value: 0 },
        { time: 2, value: 100 },
      ];
      const newKeyframe: Keyframe = { time: 1, value: 50 };
      const result = AnimationUtils.addKeyframe(keyframes, newKeyframe);

      expect(result).toHaveLength(3);
      expect(result[0].time).toBe(0);
      expect(result[1].time).toBe(1);
      expect(result[2].time).toBe(2);
    });

    it('should replace keyframe at same time', () => {
      const keyframes: Keyframe[] = [
        { time: 0, value: 0 },
        { time: 1, value: 50 },
      ];
      const newKeyframe: Keyframe = { time: 1, value: 75 };
      const result = AnimationUtils.addKeyframe(keyframes, newKeyframe);

      expect(result).toHaveLength(2);
      expect(result[1].value).toBe(75);
    });

    it('should remove keyframe', () => {
      const keyframes: Keyframe[] = [
        { time: 0, value: 0 },
        { time: 1, value: 50 },
        { time: 2, value: 100 },
      ];
      const result = AnimationUtils.removeKeyframe(keyframes, 1);

      expect(result).toHaveLength(2);
      expect(result[0].time).toBe(0);
      expect(result[1].time).toBe(2);
    });

    it('should update keyframe', () => {
      const keyframes: Keyframe[] = [
        { time: 0, value: 0 },
        { time: 1, value: 50, easing: 'linear' },
      ];
      const result = AnimationUtils.updateKeyframe(keyframes, 1, { easing: 'ease-in' });

      expect(result[1].easing).toBe('ease-in');
      expect(result[1].value).toBe(50);
    });
  });
});
