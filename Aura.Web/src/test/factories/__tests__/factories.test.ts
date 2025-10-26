import { describe, it, expect } from 'vitest';
import {
  createMockTimelineClip,
  createMockTrack,
  createMockChapterMarker,
  createMockTextOverlay,
  createMockClips,
  createMockTracks,
} from '../timelineFactories';

describe('Timeline Factories', () => {
  describe('createMockTimelineClip', () => {
    it('should create a clip with default values', () => {
      const clip = createMockTimelineClip();
      
      expect(clip).toMatchObject({
        id: expect.any(String),
        sourcePath: expect.any(String),
        sourceIn: expect.any(Number),
        sourceOut: expect.any(Number),
        timelineStart: expect.any(Number),
        trackId: expect.any(String),
      });
    });

    it('should allow overriding values', () => {
      const clip = createMockTimelineClip({
        id: 'custom-clip',
        timelineStart: 30,
      });
      
      expect(clip.id).toBe('custom-clip');
      expect(clip.timelineStart).toBe(30);
    });
  });

  describe('createMockTrack', () => {
    it('should create a track with default values', () => {
      const track = createMockTrack();
      
      expect(track).toMatchObject({
        id: expect.any(String),
        name: expect.any(String),
        type: expect.stringMatching(/^(video|audio)$/),
        clips: expect.any(Array),
      });
    });

    it('should allow overriding values', () => {
      const track = createMockTrack({
        type: 'audio',
        muted: true,
      });
      
      expect(track.type).toBe('audio');
      expect(track.muted).toBe(true);
    });
  });

  describe('createMockChapterMarker', () => {
    it('should create a marker with default values', () => {
      const marker = createMockChapterMarker();
      
      expect(marker).toMatchObject({
        id: expect.any(String),
        title: expect.any(String),
        time: expect.any(Number),
      });
    });

    it('should allow overriding values', () => {
      const marker = createMockChapterMarker({
        title: 'Custom Chapter',
        time: 60,
      });
      
      expect(marker.title).toBe('Custom Chapter');
      expect(marker.time).toBe(60);
    });
  });

  describe('createMockTextOverlay', () => {
    it('should create an overlay with default values', () => {
      const overlay = createMockTextOverlay();
      
      expect(overlay).toMatchObject({
        id: expect.any(String),
        type: expect.stringMatching(/^(title|lowerThird|callout)$/),
        text: expect.any(String),
        inTime: expect.any(Number),
        outTime: expect.any(Number),
        alignment: expect.any(String),
        fontSize: expect.any(Number),
        fontColor: expect.any(String),
      });
    });

    it('should allow overriding values', () => {
      const overlay = createMockTextOverlay({
        type: 'lowerThird',
        text: 'Custom Text',
        fontSize: 32,
      });
      
      expect(overlay.type).toBe('lowerThird');
      expect(overlay.text).toBe('Custom Text');
      expect(overlay.fontSize).toBe(32);
    });
  });

  describe('createMockClips', () => {
    it('should create multiple clips', () => {
      const clips = createMockClips(3);
      
      expect(clips).toHaveLength(3);
      expect(clips[0].id).toBe('clip-1');
      expect(clips[1].id).toBe('clip-2');
      expect(clips[2].id).toBe('clip-3');
    });

    it('should apply base overrides to all clips', () => {
      const clips = createMockClips(2, { trackId: 'custom-track' });
      
      expect(clips[0].trackId).toBe('custom-track');
      expect(clips[1].trackId).toBe('custom-track');
    });

    it('should increment timelineStart for each clip', () => {
      const clips = createMockClips(3);
      
      expect(clips[0].timelineStart).toBe(0);
      expect(clips[1].timelineStart).toBe(10);
      expect(clips[2].timelineStart).toBe(20);
    });
  });

  describe('createMockTracks', () => {
    it('should create multiple tracks', () => {
      const tracks = createMockTracks(2);
      
      expect(tracks).toHaveLength(2);
      expect(tracks[0].id).toBe('track-1');
      expect(tracks[1].id).toBe('track-2');
    });

    it('should apply base overrides to all tracks', () => {
      const tracks = createMockTracks(2, { type: 'audio' });
      
      expect(tracks[0].type).toBe('audio');
      expect(tracks[1].type).toBe('audio');
    });
  });
});
