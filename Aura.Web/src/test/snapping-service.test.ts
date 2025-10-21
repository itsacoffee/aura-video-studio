/**
 * Tests for SnappingService
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { SnappingService } from '../services/timeline/SnappingService';

describe('SnappingService', () => {
  let service: SnappingService;

  beforeEach(() => {
    service = new SnappingService();
  });

  describe('calculateSnapPosition', () => {
    it('should snap to closest point within threshold', () => {
      const snapPoints = [
        { position: 10, type: 'playhead' as const, priority: 1 },
        { position: 20, type: 'scene-start' as const, priority: 2 },
      ];

      // At 50 pixels per second, 8 pixels = 0.16 seconds threshold
      // So 10.1 is within threshold
      const result = service.calculateSnapPosition(10.1, snapPoints, 50);
      
      expect(result.snapped).toBe(true);
      expect(result.position).toBe(10);
    });

    it('should not snap if outside threshold', () => {
      const snapPoints = [
        { position: 10, type: 'playhead' as const, priority: 1 },
      ];

      const result = service.calculateSnapPosition(15, snapPoints, 50);
      
      expect(result.snapped).toBe(false);
      expect(result.position).toBe(15);
    });

    it('should snap to highest priority point when multiple in range', () => {
      const snapPoints = [
        { position: 10, type: 'playhead' as const, priority: 1 },
        { position: 10.1, type: 'scene-start' as const, priority: 2 },
      ];

      const result = service.calculateSnapPosition(10.05, snapPoints, 100);
      
      expect(result.snapped).toBe(true);
      expect(result.position).toBe(10); // Playhead has priority 1
    });

    it('should not snap when disabled', () => {
      service.setEnabled(false);
      
      const snapPoints = [
        { position: 10, type: 'playhead' as const, priority: 1 },
      ];

      const result = service.calculateSnapPosition(10.1, snapPoints, 50);
      
      expect(result.snapped).toBe(false);
    });
  });

  describe('generateSnapPoints', () => {
    it('should generate playhead snap point', () => {
      const points = service.generateSnapPoints(15, [], [], 5, 60);
      
      const playheadPoint = points.find(p => p.type === 'playhead');
      expect(playheadPoint).toBeDefined();
      expect(playheadPoint?.position).toBe(15);
      expect(playheadPoint?.priority).toBe(1);
    });

    it('should generate scene start and end points', () => {
      const points = service.generateSnapPoints(
        0,
        [0, 10, 20],
        [10, 20, 30],
        5,
        60
      );
      
      const sceneStarts = points.filter(p => p.type === 'scene-start');
      const sceneEnds = points.filter(p => p.type === 'scene-end');
      
      expect(sceneStarts).toHaveLength(3);
      expect(sceneEnds).toHaveLength(3);
    });

    it('should generate grid points at specified interval', () => {
      const points = service.generateSnapPoints(0, [], [], 10, 30);
      
      const gridPoints = points.filter(p => p.type === 'grid');
      expect(gridPoints.length).toBeGreaterThan(0);
      expect(gridPoints[0].position).toBe(0);
      expect(gridPoints[1]?.position).toBe(10);
    });

    it('should include marker points', () => {
      const points = service.generateSnapPoints(0, [], [], 5, 60, [15, 30, 45]);
      
      const markerPoints = points.filter(p => p.type === 'marker');
      expect(markerPoints).toHaveLength(3);
    });
  });

  describe('getGridInterval', () => {
    it('should return 1 second for high zoom', () => {
      expect(service.getGridInterval(100)).toBe(1);
    });

    it('should return 5 seconds for medium zoom', () => {
      expect(service.getGridInterval(20)).toBe(5);
    });

    it('should return 10 seconds for lower zoom', () => {
      expect(service.getGridInterval(10)).toBe(10);
    });

    it('should return 30 seconds for low zoom', () => {
      expect(service.getGridInterval(5)).toBe(30);
    });

    it('should return 60 seconds for very low zoom', () => {
      expect(service.getGridInterval(2)).toBe(60);
    });
  });

  describe('setSnapThreshold', () => {
    it('should update snap threshold', () => {
      service.setSnapThreshold(16);
      
      const snapPoints = [
        { position: 10, type: 'playhead' as const, priority: 1 },
      ];

      // Should snap with new threshold
      const result1 = service.calculateSnapPosition(10.3, snapPoints, 50);
      expect(result1.snapped).toBe(true);
    });
  });

  describe('setEnabled', () => {
    it('should disable snapping', () => {
      service.setEnabled(false);
      
      const snapPoints = [
        { position: 10, type: 'playhead' as const, priority: 1 },
      ];

      const result = service.calculateSnapPosition(10.1, snapPoints, 50);
      expect(result.snapped).toBe(false);
    });

    it('should re-enable snapping', () => {
      service.setEnabled(false);
      service.setEnabled(true);
      
      const snapPoints = [
        { position: 10, type: 'playhead' as const, priority: 1 },
      ];

      const result = service.calculateSnapPosition(10.1, snapPoints, 50);
      expect(result.snapped).toBe(true);
    });
  });
});
