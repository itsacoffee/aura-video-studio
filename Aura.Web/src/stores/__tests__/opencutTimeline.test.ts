/**
 * OpenCut Timeline Store Tests - Magnetic Timeline Features
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { useOpenCutTimelineStore } from '../opencutTimeline';

describe('OpenCutTimelineStore - Magnetic Timeline', () => {
  beforeEach(() => {
    // Reset store before each test
    useOpenCutTimelineStore.setState({
      tracks: [
        {
          id: 'track-video-1',
          type: 'video',
          name: 'Video 1',
          order: 0,
          height: 56,
          muted: false,
          solo: false,
          locked: false,
          visible: true,
        },
        {
          id: 'track-audio-1',
          type: 'audio',
          name: 'Audio 1',
          order: 1,
          height: 56,
          muted: false,
          solo: false,
          locked: false,
          visible: true,
        },
      ],
      clips: [],
      selectedClipIds: [],
      selectedTrackId: null,
      zoom: 1,
      scrollPosition: 0,
      snapEnabled: true,
      rippleEnabled: false,
      undoStack: [],
      redoStack: [],
      maxHistorySize: 50,
      magneticTimelineEnabled: true,
      snapToClips: true,
      snapTolerance: 10,
    });
  });

  describe('Magnetic Timeline State', () => {
    it('should have magnetic timeline enabled by default', () => {
      const { magneticTimelineEnabled } = useOpenCutTimelineStore.getState();
      expect(magneticTimelineEnabled).toBe(true);
    });

    it('should toggle magnetic timeline', () => {
      const { setMagneticTimeline } = useOpenCutTimelineStore.getState();

      setMagneticTimeline(false);
      expect(useOpenCutTimelineStore.getState().magneticTimelineEnabled).toBe(false);

      setMagneticTimeline(true);
      expect(useOpenCutTimelineStore.getState().magneticTimelineEnabled).toBe(true);
    });

    it('should toggle snap to clips', () => {
      const { setSnapToClips } = useOpenCutTimelineStore.getState();

      setSnapToClips(false);
      expect(useOpenCutTimelineStore.getState().snapToClips).toBe(false);

      setSnapToClips(true);
      expect(useOpenCutTimelineStore.getState().snapToClips).toBe(true);
    });

    it('should set snap tolerance within bounds', () => {
      const { setSnapTolerance } = useOpenCutTimelineStore.getState();

      setSnapTolerance(25);
      expect(useOpenCutTimelineStore.getState().snapTolerance).toBe(25);

      // Below minimum
      setSnapTolerance(0);
      expect(useOpenCutTimelineStore.getState().snapTolerance).toBe(1);

      // Above maximum
      setSnapTolerance(100);
      expect(useOpenCutTimelineStore.getState().snapTolerance).toBe(50);
    });
  });

  describe('Gap Detection', () => {
    it('should find gaps between clips on a track', () => {
      const { addClip, findGaps } = useOpenCutTimelineStore.getState();

      // Add clips with gaps
      addClip({
        trackId: 'track-video-1',
        type: 'video',
        name: 'Clip 1',
        mediaId: null,
        startTime: 0,
        duration: 2,
        inPoint: 0,
        outPoint: 2,
        transform: {
          scaleX: 100,
          scaleY: 100,
          positionX: 0,
          positionY: 0,
          rotation: 0,
          opacity: 100,
          anchorX: 50,
          anchorY: 50,
        },
        blendMode: 'normal',
        speed: 1,
        reversed: false,
        timeRemapEnabled: false,
        locked: false,
      });

      addClip({
        trackId: 'track-video-1',
        type: 'video',
        name: 'Clip 2',
        mediaId: null,
        startTime: 5, // Gap from 2 to 5
        duration: 3,
        inPoint: 0,
        outPoint: 3,
        transform: {
          scaleX: 100,
          scaleY: 100,
          positionX: 0,
          positionY: 0,
          rotation: 0,
          opacity: 100,
          anchorX: 50,
          anchorY: 50,
        },
        blendMode: 'normal',
        speed: 1,
        reversed: false,
        timeRemapEnabled: false,
        locked: false,
      });

      const gaps = useOpenCutTimelineStore.getState().findGaps('track-video-1');
      expect(gaps.length).toBe(1);
      expect(gaps[0].start).toBe(2);
      expect(gaps[0].end).toBe(5);
    });

    it('should return empty array when no gaps exist', () => {
      const { addClip, findGaps } = useOpenCutTimelineStore.getState();

      // Add adjacent clips
      addClip({
        trackId: 'track-video-1',
        type: 'video',
        name: 'Clip 1',
        mediaId: null,
        startTime: 0,
        duration: 2,
        inPoint: 0,
        outPoint: 2,
        transform: {
          scaleX: 100,
          scaleY: 100,
          positionX: 0,
          positionY: 0,
          rotation: 0,
          opacity: 100,
          anchorX: 50,
          anchorY: 50,
        },
        blendMode: 'normal',
        speed: 1,
        reversed: false,
        timeRemapEnabled: false,
        locked: false,
      });

      addClip({
        trackId: 'track-video-1',
        type: 'video',
        name: 'Clip 2',
        mediaId: null,
        startTime: 2, // Adjacent to first clip
        duration: 3,
        inPoint: 0,
        outPoint: 3,
        transform: {
          scaleX: 100,
          scaleY: 100,
          positionX: 0,
          positionY: 0,
          rotation: 0,
          opacity: 100,
          anchorX: 50,
          anchorY: 50,
        },
        blendMode: 'normal',
        speed: 1,
        reversed: false,
        timeRemapEnabled: false,
        locked: false,
      });

      const gaps = useOpenCutTimelineStore.getState().findGaps('track-video-1');
      expect(gaps.length).toBe(0);
    });
  });

  describe('Close Gap', () => {
    it('should close a gap by shifting subsequent clips', () => {
      const { addClip, closeGap } = useOpenCutTimelineStore.getState();

      addClip({
        trackId: 'track-video-1',
        type: 'video',
        name: 'Clip 1',
        mediaId: null,
        startTime: 0,
        duration: 2,
        inPoint: 0,
        outPoint: 2,
        transform: {
          scaleX: 100,
          scaleY: 100,
          positionX: 0,
          positionY: 0,
          rotation: 0,
          opacity: 100,
          anchorX: 50,
          anchorY: 50,
        },
        blendMode: 'normal',
        speed: 1,
        reversed: false,
        timeRemapEnabled: false,
        locked: false,
      });

      addClip({
        trackId: 'track-video-1',
        type: 'video',
        name: 'Clip 2',
        mediaId: null,
        startTime: 5,
        duration: 3,
        inPoint: 0,
        outPoint: 3,
        transform: {
          scaleX: 100,
          scaleY: 100,
          positionX: 0,
          positionY: 0,
          rotation: 0,
          opacity: 100,
          anchorX: 50,
          anchorY: 50,
        },
        blendMode: 'normal',
        speed: 1,
        reversed: false,
        timeRemapEnabled: false,
        locked: false,
      });

      useOpenCutTimelineStore.getState().closeGap('track-video-1', 2, 5);

      const clips = useOpenCutTimelineStore.getState().clips;
      const clip2 = clips.find((c) => c.name === 'Clip 2');
      expect(clip2?.startTime).toBe(2); // Shifted from 5 to 2
    });
  });

  describe('Ripple Delete', () => {
    it('should remove clip and shift subsequent clips', () => {
      const { addClip, rippleDelete } = useOpenCutTimelineStore.getState();

      addClip({
        trackId: 'track-video-1',
        type: 'video',
        name: 'Clip 1',
        mediaId: null,
        startTime: 0,
        duration: 2,
        inPoint: 0,
        outPoint: 2,
        transform: {
          scaleX: 100,
          scaleY: 100,
          positionX: 0,
          positionY: 0,
          rotation: 0,
          opacity: 100,
          anchorX: 50,
          anchorY: 50,
        },
        blendMode: 'normal',
        speed: 1,
        reversed: false,
        timeRemapEnabled: false,
        locked: false,
      });

      const clip2Id = addClip({
        trackId: 'track-video-1',
        type: 'video',
        name: 'Clip 2',
        mediaId: null,
        startTime: 2,
        duration: 3,
        inPoint: 0,
        outPoint: 3,
        transform: {
          scaleX: 100,
          scaleY: 100,
          positionX: 0,
          positionY: 0,
          rotation: 0,
          opacity: 100,
          anchorX: 50,
          anchorY: 50,
        },
        blendMode: 'normal',
        speed: 1,
        reversed: false,
        timeRemapEnabled: false,
        locked: false,
      });

      addClip({
        trackId: 'track-video-1',
        type: 'video',
        name: 'Clip 3',
        mediaId: null,
        startTime: 5,
        duration: 2,
        inPoint: 0,
        outPoint: 2,
        transform: {
          scaleX: 100,
          scaleY: 100,
          positionX: 0,
          positionY: 0,
          rotation: 0,
          opacity: 100,
          anchorX: 50,
          anchorY: 50,
        },
        blendMode: 'normal',
        speed: 1,
        reversed: false,
        timeRemapEnabled: false,
        locked: false,
      });

      // Delete Clip 2, which should shift Clip 3 back by 3 seconds
      useOpenCutTimelineStore.getState().rippleDelete(clip2Id);

      const clips = useOpenCutTimelineStore.getState().clips;
      expect(clips.length).toBe(2);

      const clip3 = clips.find((c) => c.name === 'Clip 3');
      expect(clip3?.startTime).toBe(2); // Shifted from 5 to 2 (5 - 3 duration of deleted clip)
    });
  });

  describe('Ripple Insert', () => {
    it('should push clips forward when inserting', () => {
      const { addClip, rippleInsert } = useOpenCutTimelineStore.getState();

      addClip({
        trackId: 'track-video-1',
        type: 'video',
        name: 'Clip 1',
        mediaId: null,
        startTime: 0,
        duration: 2,
        inPoint: 0,
        outPoint: 2,
        transform: {
          scaleX: 100,
          scaleY: 100,
          positionX: 0,
          positionY: 0,
          rotation: 0,
          opacity: 100,
          anchorX: 50,
          anchorY: 50,
        },
        blendMode: 'normal',
        speed: 1,
        reversed: false,
        timeRemapEnabled: false,
        locked: false,
      });

      addClip({
        trackId: 'track-video-1',
        type: 'video',
        name: 'Clip 2',
        mediaId: null,
        startTime: 2,
        duration: 3,
        inPoint: 0,
        outPoint: 3,
        transform: {
          scaleX: 100,
          scaleY: 100,
          positionX: 0,
          positionY: 0,
          rotation: 0,
          opacity: 100,
          anchorX: 50,
          anchorY: 50,
        },
        blendMode: 'normal',
        speed: 1,
        reversed: false,
        timeRemapEnabled: false,
        locked: false,
      });

      // Insert 4 seconds at time 1
      useOpenCutTimelineStore.getState().rippleInsert('track-video-1', 1, 4);

      const clips = useOpenCutTimelineStore.getState().clips;
      const clip1 = clips.find((c) => c.name === 'Clip 1');
      const clip2 = clips.find((c) => c.name === 'Clip 2');

      // Clip 1 starts at 0, before the insert point, so unchanged
      expect(clip1?.startTime).toBe(0);
      // Clip 2 starts at 2, after the insert point, so shifted by 4
      expect(clip2?.startTime).toBe(6);
    });
  });

  describe('Snap Points', () => {
    it('should return snap points from all clips', () => {
      const { addClip, getSnapPoints } = useOpenCutTimelineStore.getState();

      addClip({
        trackId: 'track-video-1',
        type: 'video',
        name: 'Clip 1',
        mediaId: null,
        startTime: 2,
        duration: 3,
        inPoint: 0,
        outPoint: 3,
        transform: {
          scaleX: 100,
          scaleY: 100,
          positionX: 0,
          positionY: 0,
          rotation: 0,
          opacity: 100,
          anchorX: 50,
          anchorY: 50,
        },
        blendMode: 'normal',
        speed: 1,
        reversed: false,
        timeRemapEnabled: false,
        locked: false,
      });

      addClip({
        trackId: 'track-video-1',
        type: 'video',
        name: 'Clip 2',
        mediaId: null,
        startTime: 7,
        duration: 2,
        inPoint: 0,
        outPoint: 2,
        transform: {
          scaleX: 100,
          scaleY: 100,
          positionX: 0,
          positionY: 0,
          rotation: 0,
          opacity: 100,
          anchorX: 50,
          anchorY: 50,
        },
        blendMode: 'normal',
        speed: 1,
        reversed: false,
        timeRemapEnabled: false,
        locked: false,
      });

      const snapPoints = useOpenCutTimelineStore.getState().getSnapPoints();

      // Should include: 0 (timeline start), 2 (clip1 start), 5 (clip1 end), 7 (clip2 start), 9 (clip2 end)
      expect(snapPoints).toContain(0);
      expect(snapPoints).toContain(2);
      expect(snapPoints).toContain(5);
      expect(snapPoints).toContain(7);
      expect(snapPoints).toContain(9);
    });

    it('should exclude specified clip from snap points', () => {
      const { addClip, getSnapPoints } = useOpenCutTimelineStore.getState();

      const clip1Id = addClip({
        trackId: 'track-video-1',
        type: 'video',
        name: 'Clip 1',
        mediaId: null,
        startTime: 2,
        duration: 3,
        inPoint: 0,
        outPoint: 3,
        transform: {
          scaleX: 100,
          scaleY: 100,
          positionX: 0,
          positionY: 0,
          rotation: 0,
          opacity: 100,
          anchorX: 50,
          anchorY: 50,
        },
        blendMode: 'normal',
        speed: 1,
        reversed: false,
        timeRemapEnabled: false,
        locked: false,
      });

      addClip({
        trackId: 'track-video-1',
        type: 'video',
        name: 'Clip 2',
        mediaId: null,
        startTime: 7,
        duration: 2,
        inPoint: 0,
        outPoint: 2,
        transform: {
          scaleX: 100,
          scaleY: 100,
          positionX: 0,
          positionY: 0,
          rotation: 0,
          opacity: 100,
          anchorX: 50,
          anchorY: 50,
        },
        blendMode: 'normal',
        speed: 1,
        reversed: false,
        timeRemapEnabled: false,
        locked: false,
      });

      const snapPoints = useOpenCutTimelineStore.getState().getSnapPoints(clip1Id);

      // Should not include clip1's points (2, 5) but should include clip2's (7, 9)
      expect(snapPoints).not.toContain(2);
      expect(snapPoints).not.toContain(5);
      expect(snapPoints).toContain(7);
      expect(snapPoints).toContain(9);
    });
  });

  describe('Find Nearest Snap Point', () => {
    it('should find nearest snap point within tolerance', () => {
      const { addClip, findNearestSnapPoint, setSnapTolerance } =
        useOpenCutTimelineStore.getState();

      // Set a large tolerance for testing
      setSnapTolerance(50);

      addClip({
        trackId: 'track-video-1',
        type: 'video',
        name: 'Clip 1',
        mediaId: null,
        startTime: 5,
        duration: 3,
        inPoint: 0,
        outPoint: 3,
        transform: {
          scaleX: 100,
          scaleY: 100,
          positionX: 0,
          positionY: 0,
          rotation: 0,
          opacity: 100,
          anchorX: 50,
          anchorY: 50,
        },
        blendMode: 'normal',
        speed: 1,
        reversed: false,
        timeRemapEnabled: false,
        locked: false,
      });

      // Time 4.9 should snap to 5 (clip start)
      const nearestPoint = useOpenCutTimelineStore.getState().findNearestSnapPoint(4.9);
      expect(nearestPoint).toBe(5);
    });

    it('should return null when no snap point within tolerance', () => {
      const { addClip, findNearestSnapPoint, setSnapTolerance } =
        useOpenCutTimelineStore.getState();

      setSnapTolerance(5); // Small tolerance

      addClip({
        trackId: 'track-video-1',
        type: 'video',
        name: 'Clip 1',
        mediaId: null,
        startTime: 5,
        duration: 3,
        inPoint: 0,
        outPoint: 3,
        transform: {
          scaleX: 100,
          scaleY: 100,
          positionX: 0,
          positionY: 0,
          rotation: 0,
          opacity: 100,
          anchorX: 50,
          anchorY: 50,
        },
        blendMode: 'normal',
        speed: 1,
        reversed: false,
        timeRemapEnabled: false,
        locked: false,
      });

      // Time 3 should not snap to 5 (too far away with small tolerance)
      const nearestPoint = useOpenCutTimelineStore.getState().findNearestSnapPoint(3);
      expect(nearestPoint).toBeNull();
    });

    it('should return null when snap to clips is disabled', () => {
      const { addClip, findNearestSnapPoint, setSnapToClips } = useOpenCutTimelineStore.getState();

      setSnapToClips(false);

      addClip({
        trackId: 'track-video-1',
        type: 'video',
        name: 'Clip 1',
        mediaId: null,
        startTime: 5,
        duration: 3,
        inPoint: 0,
        outPoint: 3,
        transform: {
          scaleX: 100,
          scaleY: 100,
          positionX: 0,
          positionY: 0,
          rotation: 0,
          opacity: 100,
          anchorX: 50,
          anchorY: 50,
        },
        blendMode: 'normal',
        speed: 1,
        reversed: false,
        timeRemapEnabled: false,
        locked: false,
      });

      const nearestPoint = useOpenCutTimelineStore.getState().findNearestSnapPoint(4.9);
      expect(nearestPoint).toBeNull();
    });
  });

  describe('Insert Clip With Ripple', () => {
    it('should insert clip and shift subsequent clips', () => {
      const { addClip, insertClipWithRipple } = useOpenCutTimelineStore.getState();

      addClip({
        trackId: 'track-video-1',
        type: 'video',
        name: 'Clip 1',
        mediaId: null,
        startTime: 0,
        duration: 2,
        inPoint: 0,
        outPoint: 2,
        transform: {
          scaleX: 100,
          scaleY: 100,
          positionX: 0,
          positionY: 0,
          rotation: 0,
          opacity: 100,
          anchorX: 50,
          anchorY: 50,
        },
        blendMode: 'normal',
        speed: 1,
        reversed: false,
        timeRemapEnabled: false,
        locked: false,
      });

      addClip({
        trackId: 'track-video-1',
        type: 'video',
        name: 'Clip 3',
        mediaId: null,
        startTime: 2,
        duration: 3,
        inPoint: 0,
        outPoint: 3,
        transform: {
          scaleX: 100,
          scaleY: 100,
          positionX: 0,
          positionY: 0,
          rotation: 0,
          opacity: 100,
          anchorX: 50,
          anchorY: 50,
        },
        blendMode: 'normal',
        speed: 1,
        reversed: false,
        timeRemapEnabled: false,
        locked: false,
      });

      // Insert a new clip at time 2 with duration 4
      useOpenCutTimelineStore.getState().insertClipWithRipple(
        {
          trackId: 'track-video-1',
          type: 'video',
          name: 'Clip 2',
          mediaId: null,
          startTime: 2,
          duration: 4,
          inPoint: 0,
          outPoint: 4,
          transform: {
            scaleX: 100,
            scaleY: 100,
            positionX: 0,
            positionY: 0,
            rotation: 0,
            opacity: 100,
            anchorX: 50,
            anchorY: 50,
          },
          blendMode: 'normal',
          speed: 1,
          reversed: false,
          timeRemapEnabled: false,
          locked: false,
        },
        2
      );

      const clips = useOpenCutTimelineStore.getState().clips;
      expect(clips.length).toBe(3);

      const clip1 = clips.find((c) => c.name === 'Clip 1');
      const clip2 = clips.find((c) => c.name === 'Clip 2');
      const clip3 = clips.find((c) => c.name === 'Clip 3');

      expect(clip1?.startTime).toBe(0);
      expect(clip2?.startTime).toBe(2);
      expect(clip3?.startTime).toBe(6); // Shifted from 2 to 6 (by 4 seconds)
    });
  });

  describe('Overwrite Clip', () => {
    it('should overwrite existing clips when placing new clip', () => {
      const { addClip, overwriteClip } = useOpenCutTimelineStore.getState();

      addClip({
        trackId: 'track-video-1',
        type: 'video',
        name: 'Clip 1',
        mediaId: null,
        startTime: 0,
        duration: 5,
        inPoint: 0,
        outPoint: 5,
        transform: {
          scaleX: 100,
          scaleY: 100,
          positionX: 0,
          positionY: 0,
          rotation: 0,
          opacity: 100,
          anchorX: 50,
          anchorY: 50,
        },
        blendMode: 'normal',
        speed: 1,
        reversed: false,
        timeRemapEnabled: false,
        locked: false,
      });

      // Overwrite with a new clip from time 2 to 4
      useOpenCutTimelineStore.getState().overwriteClip({
        trackId: 'track-video-1',
        type: 'video',
        name: 'New Clip',
        mediaId: null,
        startTime: 2,
        duration: 2,
        inPoint: 0,
        outPoint: 2,
        transform: {
          scaleX: 100,
          scaleY: 100,
          positionX: 0,
          positionY: 0,
          rotation: 0,
          opacity: 100,
          anchorX: 50,
          anchorY: 50,
        },
        blendMode: 'normal',
        speed: 1,
        reversed: false,
        timeRemapEnabled: false,
        locked: false,
      });

      const clips = useOpenCutTimelineStore.getState().clips;

      // Should have 3 clips: part of Clip 1 (0-2), New Clip (2-4), rest of Clip 1 (4-5)
      expect(clips.length).toBe(3);

      const newClip = clips.find((c) => c.name === 'New Clip');
      expect(newClip?.startTime).toBe(2);
      expect(newClip?.duration).toBe(2);
    });
  });

  describe('Close All Gaps', () => {
    it('should close all gaps on all tracks', () => {
      const { addClip, closeAllGaps } = useOpenCutTimelineStore.getState();

      // Add clips with gaps on video track
      addClip({
        trackId: 'track-video-1',
        type: 'video',
        name: 'Video 1',
        mediaId: null,
        startTime: 0,
        duration: 2,
        inPoint: 0,
        outPoint: 2,
        transform: {
          scaleX: 100,
          scaleY: 100,
          positionX: 0,
          positionY: 0,
          rotation: 0,
          opacity: 100,
          anchorX: 50,
          anchorY: 50,
        },
        blendMode: 'normal',
        speed: 1,
        reversed: false,
        timeRemapEnabled: false,
        locked: false,
      });

      addClip({
        trackId: 'track-video-1',
        type: 'video',
        name: 'Video 2',
        mediaId: null,
        startTime: 5, // Gap from 2 to 5
        duration: 3,
        inPoint: 0,
        outPoint: 3,
        transform: {
          scaleX: 100,
          scaleY: 100,
          positionX: 0,
          positionY: 0,
          rotation: 0,
          opacity: 100,
          anchorX: 50,
          anchorY: 50,
        },
        blendMode: 'normal',
        speed: 1,
        reversed: false,
        timeRemapEnabled: false,
        locked: false,
      });

      // Add clips with gaps on audio track
      addClip({
        trackId: 'track-audio-1',
        type: 'audio',
        name: 'Audio 1',
        mediaId: null,
        startTime: 0,
        duration: 1,
        inPoint: 0,
        outPoint: 1,
        transform: {
          scaleX: 100,
          scaleY: 100,
          positionX: 0,
          positionY: 0,
          rotation: 0,
          opacity: 100,
          anchorX: 50,
          anchorY: 50,
        },
        blendMode: 'normal',
        audio: { volume: 100, pan: 0, fadeInDuration: 0, fadeOutDuration: 0, muted: false },
        speed: 1,
        reversed: false,
        timeRemapEnabled: false,
        locked: false,
      });

      addClip({
        trackId: 'track-audio-1',
        type: 'audio',
        name: 'Audio 2',
        mediaId: null,
        startTime: 3, // Gap from 1 to 3
        duration: 2,
        inPoint: 0,
        outPoint: 2,
        transform: {
          scaleX: 100,
          scaleY: 100,
          positionX: 0,
          positionY: 0,
          rotation: 0,
          opacity: 100,
          anchorX: 50,
          anchorY: 50,
        },
        blendMode: 'normal',
        audio: { volume: 100, pan: 0, fadeInDuration: 0, fadeOutDuration: 0, muted: false },
        speed: 1,
        reversed: false,
        timeRemapEnabled: false,
        locked: false,
      });

      useOpenCutTimelineStore.getState().closeAllGaps();

      const clips = useOpenCutTimelineStore.getState().clips;

      const video2 = clips.find((c) => c.name === 'Video 2');
      const audio2 = clips.find((c) => c.name === 'Audio 2');

      expect(video2?.startTime).toBe(2); // Gap closed
      expect(audio2?.startTime).toBe(1); // Gap closed
    });
  });
});
