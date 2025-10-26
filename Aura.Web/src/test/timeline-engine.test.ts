import { describe, it, expect } from 'vitest';
import {
  secondsToFrames,
  framesToSeconds,
  snapToFrame,
  formatTimecode,
  formatFrameNumber,
  formatSeconds,
  findNearestSnapPoint,
  calculateSnapPoints,
  applyRippleEdit,
  findGaps,
  closeGaps,
  calculateRulerInterval,
  TimelineDisplayMode,
} from '../services/timelineEngine';

describe('timelineEngine', () => {
  describe('Frame conversions', () => {
    it('should convert seconds to frames', () => {
      expect(secondsToFrames(1, 30)).toBe(30);
      expect(secondsToFrames(1.5, 30)).toBe(45);
      expect(secondsToFrames(0.5, 30)).toBe(15);
    });

    it('should convert frames to seconds', () => {
      expect(framesToSeconds(30, 30)).toBe(1);
      expect(framesToSeconds(45, 30)).toBe(1.5);
      expect(framesToSeconds(15, 30)).toBe(0.5);
    });

    it('should snap to frame boundaries', () => {
      expect(snapToFrame(1.01, 30)).toBeCloseTo(1.0, 2);
      expect(snapToFrame(1.49, 30)).toBeCloseTo(1.5, 2);
      expect(snapToFrame(1.51, 30)).toBeCloseTo(1.5, 2);
    });
  });

  describe('Time formatting', () => {
    it('should format timecode HH:MM:SS:FF', () => {
      expect(formatTimecode(0, 30)).toBe('00:00:00:00');
      expect(formatTimecode(1, 30)).toBe('00:00:01:00');
      expect(formatTimecode(60, 30)).toBe('00:01:00:00');
      expect(formatTimecode(3600, 30)).toBe('01:00:00:00');
      expect(formatTimecode(1.5, 30)).toBe('00:00:01:15');
    });

    it('should format frame numbers', () => {
      expect(formatFrameNumber(0, 30)).toBe('0');
      expect(formatFrameNumber(1, 30)).toBe('30');
      expect(formatFrameNumber(1.5, 30)).toBe('45');
    });

    it('should format seconds', () => {
      expect(formatSeconds(0)).toBe('0.00s');
      expect(formatSeconds(1.5)).toBe('1.50s');
      expect(formatSeconds(10.123)).toBe('10.12s');
    });
  });

  describe('Snap points', () => {
    it('should find nearest snap point within threshold', () => {
      const snapPoints = [
        { time: 1.0, type: 'clip-start' as const },
        { time: 2.0, type: 'clip-end' as const },
        { time: 5.0, type: 'playhead' as const },
      ];

      const result = findNearestSnapPoint(1.05, snapPoints, 0.1);
      expect(result).toBeTruthy();
      expect(result?.time).toBe(1.0);
    });

    it('should return null if no snap point within threshold', () => {
      const snapPoints = [
        { time: 1.0, type: 'clip-start' as const },
      ];

      const result = findNearestSnapPoint(2.0, snapPoints, 0.1);
      expect(result).toBeNull();
    });

    it('should calculate snap points from clips', () => {
      const clips = [
        { id: 'clip1', startTime: 1.0, duration: 2.0 },
        { id: 'clip2', startTime: 4.0, duration: 1.0 },
      ];

      const snapPoints = calculateSnapPoints(clips, 0, []);
      
      // Should have clip starts, ends, and playhead
      expect(snapPoints.length).toBeGreaterThanOrEqual(5);
      
      const clipStarts = snapPoints.filter(p => p.type === 'clip-start');
      expect(clipStarts.length).toBe(2);
      
      const clipEnds = snapPoints.filter(p => p.type === 'clip-end');
      expect(clipEnds.length).toBe(2);
    });
  });

  describe('Ripple edit', () => {
    it('should move clips after edit point', () => {
      const clips = [
        { id: '1', startTime: 0 },
        { id: '2', startTime: 2 },
        { id: '3', startTime: 4 },
      ];

      const result = applyRippleEdit(clips, 2, 1);
      
      expect(result[0].startTime).toBe(0); // Before edit point - unchanged
      expect(result[1].startTime).toBe(2); // At edit point - unchanged
      expect(result[2].startTime).toBe(5); // After edit point - moved by delta
    });

    it('should handle negative delta', () => {
      const clips = [
        { id: '1', startTime: 0 },
        { id: '2', startTime: 2 },
        { id: '3', startTime: 4 },
      ];

      const result = applyRippleEdit(clips, 2, -0.5);
      
      expect(result[2].startTime).toBe(3.5);
    });
  });

  describe('Gap detection and closing', () => {
    it('should find gaps between clips', () => {
      const clips = [
        { startTime: 0, duration: 1 },
        { startTime: 2, duration: 1 }, // Gap from 1 to 2
        { startTime: 5, duration: 1 }, // Gap from 3 to 5
      ];

      const gaps = findGaps(clips);
      
      expect(gaps.length).toBe(2);
      expect(gaps[0]).toEqual({ start: 1, end: 2 });
      expect(gaps[1]).toEqual({ start: 3, end: 5 });
    });

    it('should handle no gaps', () => {
      const clips = [
        { startTime: 0, duration: 1 },
        { startTime: 1, duration: 1 },
        { startTime: 2, duration: 1 },
      ];

      const gaps = findGaps(clips);
      expect(gaps.length).toBe(0);
    });

    it('should close gaps in timeline', () => {
      const clips = [
        { id: '1', startTime: 0, duration: 1 },
        { id: '2', startTime: 2, duration: 1 },
        { id: '3', startTime: 5, duration: 1 },
      ];

      const closed = closeGaps(clips);
      
      expect(closed[0].startTime).toBe(0);
      expect(closed[1].startTime).toBe(1); // Moved to close gap
      expect(closed[2].startTime).toBe(2); // Moved to close gap
    });

    it('should handle overlapping clips', () => {
      const clips = [
        { id: '1', startTime: 0, duration: 2 },
        { id: '2', startTime: 1, duration: 1 }, // Overlaps with clip 1
      ];

      const closed = closeGaps(clips);
      
      // Should maintain relative positions
      expect(closed[0].startTime).toBe(0);
      expect(closed[1].startTime).toBe(1);
    });
  });

  describe('Ruler intervals', () => {
    it('should calculate appropriate intervals for timecode mode', () => {
      const { majorInterval, minorInterval } = calculateRulerInterval(
        100,
        TimelineDisplayMode.TIMECODE,
        30
      );
      
      expect(majorInterval).toBeGreaterThan(0);
      expect(minorInterval).toBeGreaterThan(0);
      expect(majorInterval).toBeGreaterThanOrEqual(minorInterval);
    });

    it('should adjust intervals based on zoom', () => {
      const highZoom = calculateRulerInterval(150, TimelineDisplayMode.TIMECODE, 30);
      const lowZoom = calculateRulerInterval(20, TimelineDisplayMode.TIMECODE, 30);
      
      // High zoom should have smaller intervals for more detail
      expect(highZoom.majorInterval).toBeLessThan(lowZoom.majorInterval);
    });

    it('should handle frames display mode', () => {
      const { majorInterval, minorInterval } = calculateRulerInterval(
        100,
        TimelineDisplayMode.FRAMES,
        30
      );
      
      // In frames mode, intervals should be based on frame boundaries
      expect(majorInterval).toBeGreaterThan(0);
      expect(minorInterval).toBeGreaterThan(0);
      expect(majorInterval).toBeGreaterThanOrEqual(minorInterval);
    });
  });
});
