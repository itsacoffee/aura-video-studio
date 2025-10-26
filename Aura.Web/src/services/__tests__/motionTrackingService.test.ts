/**
 * Tests for motionTrackingService
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { MotionTracker } from '../motionTrackingService';

describe('motionTrackingService', () => {
  let tracker: MotionTracker;

  beforeEach(() => {
    tracker = new MotionTracker();
  });

  describe('startTracking', () => {
    it('should initialize a tracking point', () => {
      tracker.startTracking('point1', 'Test Point', 100, 100, 0);

      const path = tracker.getTrackingPath('point1');
      expect(path).not.toBeNull();
      expect(path?.name).toBe('Test Point');
      expect(path?.points).toHaveLength(1);
      expect(path?.points[0].x).toBe(100);
      expect(path?.points[0].y).toBe(100);
    });

    it('should allow multiple points with same ID at different times', () => {
      tracker.startTracking('point1', 'Test Point', 100, 100, 0);
      tracker.startTracking('point1', 'Test Point', 150, 150, 1);

      const path = tracker.getTrackingPath('point1');
      expect(path?.points).toHaveLength(2);
    });
  });

  describe('getTrackingPath', () => {
    it('should return null for non-existent point', () => {
      const path = tracker.getTrackingPath('nonexistent');
      expect(path).toBeNull();
    });

    it('should return tracking path with correct metadata', () => {
      tracker.startTracking('point1', 'Test Point', 100, 100, 0);
      tracker.startTracking('point1', 'Test Point', 150, 150, 1);

      const path = tracker.getTrackingPath('point1');
      expect(path).not.toBeNull();
      expect(path?.startFrame).toBe(0);
      expect(path?.endFrame).toBe(1);
    });
  });

  describe('getPositionAtTime', () => {
    beforeEach(() => {
      tracker.startTracking('point1', 'Test Point', 0, 0, 0);
      tracker.startTracking('point1', 'Test Point', 100, 100, 1);
    });

    it('should return null for non-existent point', () => {
      const pos = tracker.getPositionAtTime('nonexistent', 0.5);
      expect(pos).toBeNull();
    });

    it('should return first point position for time before start', () => {
      const pos = tracker.getPositionAtTime('point1', -1);
      expect(pos).toEqual({ x: 0, y: 0 });
    });

    it('should return last point position for time after end', () => {
      const pos = tracker.getPositionAtTime('point1', 2);
      expect(pos).toEqual({ x: 100, y: 100 });
    });

    it('should interpolate position for time between points', () => {
      const pos = tracker.getPositionAtTime('point1', 0.5);
      expect(pos).not.toBeNull();
      expect(pos!.x).toBe(50);
      expect(pos!.y).toBe(50);
    });

    it('should return exact position at keyframe time', () => {
      const pos = tracker.getPositionAtTime('point1', 1);
      expect(pos).toEqual({ x: 100, y: 100 });
    });
  });

  describe('clear', () => {
    it('should remove all tracking data', () => {
      tracker.startTracking('point1', 'Test Point 1', 100, 100, 0);
      tracker.startTracking('point2', 'Test Point 2', 200, 200, 0);

      tracker.clear();

      expect(tracker.getTrackingPath('point1')).toBeNull();
      expect(tracker.getTrackingPath('point2')).toBeNull();
    });
  });

  describe('removeTracking', () => {
    it('should remove specific tracking point', () => {
      tracker.startTracking('point1', 'Test Point 1', 100, 100, 0);
      tracker.startTracking('point2', 'Test Point 2', 200, 200, 0);

      tracker.removeTracking('point1');

      expect(tracker.getTrackingPath('point1')).toBeNull();
      expect(tracker.getTrackingPath('point2')).not.toBeNull();
    });
  });

  describe('exportTrackingData', () => {
    it('should export all tracking paths', () => {
      tracker.startTracking('point1', 'Test Point 1', 100, 100, 0);
      tracker.startTracking('point2', 'Test Point 2', 200, 200, 0);

      const data = tracker.exportTrackingData();

      expect(Object.keys(data)).toHaveLength(2);
      expect(data.point1).toBeDefined();
      expect(data.point2).toBeDefined();
    });

    it('should export empty object when no tracking data', () => {
      const data = tracker.exportTrackingData();
      expect(Object.keys(data)).toHaveLength(0);
    });
  });

  describe('importTrackingData', () => {
    it('should import tracking data correctly', () => {
      const exportedData = {
        point1: {
          id: 'point1',
          name: 'Test Point',
          points: [
            {
              id: 'point1-0',
              name: 'Test Point',
              x: 100,
              y: 100,
              confidence: 1,
              timestamp: 0,
            },
          ],
          startFrame: 0,
          endFrame: 0,
        },
      };

      tracker.importTrackingData(exportedData);

      const path = tracker.getTrackingPath('point1');
      expect(path).not.toBeNull();
      expect(path?.points).toHaveLength(1);
      expect(path?.points[0].x).toBe(100);
    });
  });
});
