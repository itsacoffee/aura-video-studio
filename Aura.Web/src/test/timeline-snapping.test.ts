import { describe, it, expect } from 'vitest';
import {
  calculateEnhancedSnapPoints,
  findNearestSnapPoint,
  SnapPointsOptions,
} from '../services/timelineEngine';

describe('Timeline Enhanced Snapping', () => {
  it('should calculate snap points from clips', () => {
    const options: SnapPointsOptions = {
      clips: [
        { id: 'clip1', startTime: 0, duration: 5 },
        { id: 'clip2', startTime: 5, duration: 5 },
      ],
      playheadTime: 2.5,
    };

    const snapPoints = calculateEnhancedSnapPoints(options);

    expect(snapPoints.length).toBeGreaterThan(0);

    const clipStarts = snapPoints.filter((p) => p.type === 'clip-start');
    expect(clipStarts).toHaveLength(2);

    const clipEnds = snapPoints.filter((p) => p.type === 'clip-end');
    expect(clipEnds).toHaveLength(2);

    const playheadPoints = snapPoints.filter((p) => p.type === 'playhead');
    expect(playheadPoints).toHaveLength(1);
    expect(playheadPoints[0].time).toBe(2.5);
  });

  it('should calculate snap points from markers', () => {
    const options: SnapPointsOptions = {
      clips: [],
      markers: [
        { time: 10, label: 'Chapter 1' },
        { time: 20, label: 'Chapter 2' },
      ],
    };

    const snapPoints = calculateEnhancedSnapPoints(options);

    const markerPoints = snapPoints.filter((p) => p.type === 'marker');
    expect(markerPoints).toHaveLength(2);
    expect(markerPoints[0].time).toBe(10);
    expect(markerPoints[0].label).toBe('Chapter 1');
    expect(markerPoints[1].time).toBe(20);
  });

  it('should calculate snap points from captions', () => {
    const options: SnapPointsOptions = {
      clips: [],
      captions: [
        { startTime: 5, endTime: 8, text: 'Hello world' },
        { startTime: 10, endTime: 12, text: 'Second caption' },
      ],
    };

    const snapPoints = calculateEnhancedSnapPoints(options);

    const captionPoints = snapPoints.filter((p) => p.type === 'caption');
    expect(captionPoints).toHaveLength(4);
    expect(captionPoints.some((p) => p.time === 5)).toBe(true);
    expect(captionPoints.some((p) => p.time === 8)).toBe(true);
    expect(captionPoints.some((p) => p.time === 10)).toBe(true);
    expect(captionPoints.some((p) => p.time === 12)).toBe(true);
  });

  it('should calculate snap points from audio peaks', () => {
    const options: SnapPointsOptions = {
      clips: [],
      audioPeaks: [
        { time: 2.5, intensity: 0.8 },
        { time: 7.3, intensity: 0.9 },
        { time: 15.1, intensity: 0.7 },
      ],
    };

    const snapPoints = calculateEnhancedSnapPoints(options);

    const audioPeakPoints = snapPoints.filter((p) => p.type === 'audio-peak');
    expect(audioPeakPoints).toHaveLength(3);
    expect(audioPeakPoints[0].time).toBe(2.5);
    expect(audioPeakPoints[0].intensity).toBe(0.8);
    expect(audioPeakPoints[1].time).toBe(7.3);
    expect(audioPeakPoints[2].time).toBe(15.1);
  });

  it('should calculate snap points from scene boundaries', () => {
    const options: SnapPointsOptions = {
      clips: [],
      sceneBoundaries: [{ time: 30 }, { time: 60 }, { time: 90 }],
    };

    const snapPoints = calculateEnhancedSnapPoints(options);

    const scenePoints = snapPoints.filter((p) => p.type === 'scene-boundary');
    expect(scenePoints).toHaveLength(3);
    expect(scenePoints[0].time).toBe(30);
    expect(scenePoints[1].time).toBe(60);
    expect(scenePoints[2].time).toBe(90);
  });

  it('should respect enable flags for snap point types', () => {
    const options: SnapPointsOptions = {
      clips: [{ id: 'clip1', startTime: 0, duration: 5 }],
      markers: [{ time: 10 }],
      captions: [{ startTime: 5, endTime: 8, text: 'Test' }],
      enableClips: false,
      enableMarkers: true,
      enableCaptions: false,
    };

    const snapPoints = calculateEnhancedSnapPoints(options);

    const clipPoints = snapPoints.filter((p) => p.type === 'clip-start' || p.type === 'clip-end');
    expect(clipPoints).toHaveLength(0);

    const markerPoints = snapPoints.filter((p) => p.type === 'marker');
    expect(markerPoints).toHaveLength(1);

    const captionPoints = snapPoints.filter((p) => p.type === 'caption');
    expect(captionPoints).toHaveLength(0);
  });

  it('should find nearest snap point within threshold', () => {
    const snapPoints = [
      { time: 0, type: 'clip-start' as const },
      { time: 5, type: 'clip-end' as const },
      { time: 10, type: 'marker' as const },
      { time: 15, type: 'caption' as const },
    ];

    const nearest = findNearestSnapPoint(5.05, snapPoints, 0.1);
    expect(nearest).not.toBeNull();
    expect(nearest?.time).toBe(5);
    expect(nearest?.type).toBe('clip-end');
  });

  it('should return null if no snap point within threshold', () => {
    const snapPoints = [
      { time: 0, type: 'clip-start' as const },
      { time: 5, type: 'clip-end' as const },
      { time: 10, type: 'marker' as const },
    ];

    const nearest = findNearestSnapPoint(7.5, snapPoints, 0.1);
    expect(nearest).toBeNull();
  });

  it('should find nearest snap point among multiple candidates', () => {
    const snapPoints = [
      { time: 5, type: 'clip-end' as const },
      { time: 5.05, type: 'marker' as const },
      { time: 5.08, type: 'caption' as const },
    ];

    const nearest = findNearestSnapPoint(5.02, snapPoints, 0.1);
    expect(nearest).not.toBeNull();
    expect(nearest?.time).toBe(5);
    expect(nearest?.type).toBe('clip-end');
  });

  it('should include in and out points', () => {
    const options: SnapPointsOptions = {
      clips: [],
      inPoint: 10,
      outPoint: 20,
    };

    const snapPoints = calculateEnhancedSnapPoints(options);

    const inPoints = snapPoints.filter((p) => p.type === 'in-point');
    expect(inPoints).toHaveLength(1);
    expect(inPoints[0].time).toBe(10);

    const outPoints = snapPoints.filter((p) => p.type === 'out-point');
    expect(outPoints).toHaveLength(1);
    expect(outPoints[0].time).toBe(20);
  });

  it('should handle combined snap points', () => {
    const options: SnapPointsOptions = {
      clips: [{ id: 'clip1', startTime: 0, duration: 5 }],
      markers: [{ time: 5, label: 'Marker at clip end' }],
      captions: [{ startTime: 5, endTime: 8, text: 'Caption starts at clip end' }],
      audioPeaks: [{ time: 5, intensity: 0.9 }],
      sceneBoundaries: [{ time: 5 }],
    };

    const snapPoints = calculateEnhancedSnapPoints(options);

    const pointsAtFive = snapPoints.filter((p) => p.time === 5);
    expect(pointsAtFive.length).toBeGreaterThanOrEqual(5);

    const types = new Set(pointsAtFive.map((p) => p.type));
    expect(types.has('clip-end')).toBe(true);
    expect(types.has('marker')).toBe(true);
    expect(types.has('caption')).toBe(true);
    expect(types.has('audio-peak')).toBe(true);
    expect(types.has('scene-boundary')).toBe(true);
  });
});
