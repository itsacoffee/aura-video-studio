/**
 * Tests for opencutCaptions store
 *
 * Tests for the captions store functionality including track management,
 * caption CRUD operations, and import/export features.
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { useOpenCutCaptionsStore, DEFAULT_CAPTION_STYLE } from '../opencutCaptions';

describe('opencutCaptions store', () => {
  beforeEach(() => {
    useOpenCutCaptionsStore.getState().reset();
  });

  describe('Track Management', () => {
    it('starts with empty tracks', () => {
      const { tracks } = useOpenCutCaptionsStore.getState();
      expect(tracks).toHaveLength(0);
    });

    it('adds a track with default style', () => {
      const { addTrack } = useOpenCutCaptionsStore.getState();

      const trackId = addTrack('English', 'en');

      const { tracks } = useOpenCutCaptionsStore.getState();
      expect(tracks).toHaveLength(1);
      expect(tracks[0].id).toBe(trackId);
      expect(tracks[0].name).toBe('English');
      expect(tracks[0].language).toBe('en');
      expect(tracks[0].visible).toBe(true);
      expect(tracks[0].locked).toBe(false);
      expect(tracks[0].style).toEqual(DEFAULT_CAPTION_STYLE);
    });

    it('adds a track with custom style', () => {
      const { addTrack } = useOpenCutCaptionsStore.getState();

      const customStyle = { fontSize: 32, color: '#FF0000' };
      addTrack('Custom', 'en', customStyle);

      const { tracks } = useOpenCutCaptionsStore.getState();
      expect(tracks[0].style.fontSize).toBe(32);
      expect(tracks[0].style.color).toBe('#FF0000');
      expect(tracks[0].style.fontFamily).toBe(DEFAULT_CAPTION_STYLE.fontFamily);
    });

    it('updates track properties', () => {
      const { addTrack, updateTrack } = useOpenCutCaptionsStore.getState();

      const trackId = addTrack('English', 'en');
      updateTrack(trackId, { name: 'Spanish', language: 'es', visible: false });

      const { tracks } = useOpenCutCaptionsStore.getState();
      expect(tracks[0].name).toBe('Spanish');
      expect(tracks[0].language).toBe('es');
      expect(tracks[0].visible).toBe(false);
    });

    it('removes a track and its captions', () => {
      const { addTrack, addCaption, removeTrack, getCaptionsForTrack } =
        useOpenCutCaptionsStore.getState();

      const trackId = addTrack('English', 'en');
      addCaption(trackId, 0, 2, 'Hello');
      addCaption(trackId, 2, 4, 'World');

      removeTrack(trackId);

      const { tracks } = useOpenCutCaptionsStore.getState();
      expect(tracks).toHaveLength(0);
      expect(getCaptionsForTrack(trackId)).toHaveLength(0);
    });

    it('selects and deselects tracks', () => {
      const { addTrack, selectTrack } = useOpenCutCaptionsStore.getState();

      const trackId = addTrack('English', 'en');

      expect(useOpenCutCaptionsStore.getState().selectedTrackId).toBe(trackId);

      selectTrack(null);
      expect(useOpenCutCaptionsStore.getState().selectedTrackId).toBeNull();
    });

    it('sets active track', () => {
      const { addTrack, setActiveTrack } = useOpenCutCaptionsStore.getState();

      addTrack('English', 'en');
      const trackId2 = addTrack('Spanish', 'es');

      setActiveTrack(trackId2);

      expect(useOpenCutCaptionsStore.getState().activeTrackId).toBe(trackId2);
    });
  });

  describe('Caption Management', () => {
    let trackId: string;

    beforeEach(() => {
      const { addTrack } = useOpenCutCaptionsStore.getState();
      trackId = addTrack('English', 'en');
    });

    it('adds a caption', () => {
      const { addCaption, getCaptionsForTrack } = useOpenCutCaptionsStore.getState();

      const captionId = addCaption(trackId, 0, 2, 'Hello world');

      const captions = getCaptionsForTrack(trackId);
      expect(captions).toHaveLength(1);
      expect(captions[0].id).toBe(captionId);
      expect(captions[0].text).toBe('Hello world');
      expect(captions[0].startTime).toBe(0);
      expect(captions[0].endTime).toBe(2);
    });

    it('adds caption with speaker', () => {
      const { addCaption, getCaptionsForTrack } = useOpenCutCaptionsStore.getState();

      addCaption(trackId, 0, 2, 'Hello', 'Speaker 1');

      const captions = getCaptionsForTrack(trackId);
      expect(captions[0].speaker).toBe('Speaker 1');
    });

    it('updates caption', () => {
      const { addCaption, updateCaption, getCaptionsForTrack } = useOpenCutCaptionsStore.getState();

      const captionId = addCaption(trackId, 0, 2, 'Hello');
      updateCaption(captionId, { text: 'Updated text', endTime: 3 });

      const captions = getCaptionsForTrack(trackId);
      expect(captions[0].text).toBe('Updated text');
      expect(captions[0].endTime).toBe(3);
    });

    it('removes caption', () => {
      const { addCaption, removeCaption, getCaptionsForTrack } = useOpenCutCaptionsStore.getState();

      const captionId = addCaption(trackId, 0, 2, 'Hello');
      removeCaption(captionId);

      expect(getCaptionsForTrack(trackId)).toHaveLength(0);
    });

    it('sorts captions by start time', () => {
      const { addCaption, getCaptionsForTrack } = useOpenCutCaptionsStore.getState();

      addCaption(trackId, 4, 6, 'Third');
      addCaption(trackId, 0, 2, 'First');
      addCaption(trackId, 2, 4, 'Second');

      const captions = getCaptionsForTrack(trackId);
      expect(captions[0].text).toBe('First');
      expect(captions[1].text).toBe('Second');
      expect(captions[2].text).toBe('Third');
    });

    it('batch adds captions', () => {
      const { addCaptions, getCaptionsForTrack } = useOpenCutCaptionsStore.getState();

      addCaptions(trackId, [
        { startTime: 0, endTime: 2, text: 'First' },
        { startTime: 2, endTime: 4, text: 'Second' },
        { startTime: 4, endTime: 6, text: 'Third' },
      ]);

      expect(getCaptionsForTrack(trackId)).toHaveLength(3);
    });

    it('clears track captions', () => {
      const { addCaption, clearTrackCaptions, getCaptionsForTrack } =
        useOpenCutCaptionsStore.getState();

      addCaption(trackId, 0, 2, 'Hello');
      addCaption(trackId, 2, 4, 'World');

      clearTrackCaptions(trackId);

      expect(getCaptionsForTrack(trackId)).toHaveLength(0);
    });
  });

  describe('Caption Queries', () => {
    let trackId: string;

    beforeEach(() => {
      const { addTrack, addCaption } = useOpenCutCaptionsStore.getState();
      trackId = addTrack('English', 'en');
      addCaption(trackId, 0, 2, 'First');
      addCaption(trackId, 2, 4, 'Second');
      addCaption(trackId, 4, 6, 'Third');
    });

    it('gets caption at specific time', () => {
      const { getCaptionAtTime } = useOpenCutCaptionsStore.getState();

      const caption = getCaptionAtTime(trackId, 1);
      expect(caption?.text).toBe('First');

      const caption2 = getCaptionAtTime(trackId, 3);
      expect(caption2?.text).toBe('Second');

      const caption3 = getCaptionAtTime(trackId, 5);
      expect(caption3?.text).toBe('Third');
    });

    it('returns null for time without caption', () => {
      const { getCaptionAtTime } = useOpenCutCaptionsStore.getState();

      const caption = getCaptionAtTime(trackId, 10);
      expect(caption).toBeNull();
    });

    it('gets visible captions at time', () => {
      const { getVisibleCaptions, addTrack, addCaption } = useOpenCutCaptionsStore.getState();

      const track2 = addTrack('Spanish', 'es');
      addCaption(track2, 0, 2, 'Spanish First');

      const visible = getVisibleCaptions(1);
      expect(visible).toHaveLength(2);
    });

    it('excludes hidden track captions', () => {
      const { getVisibleCaptions, updateTrack } = useOpenCutCaptionsStore.getState();

      updateTrack(trackId, { visible: false });

      const visible = getVisibleCaptions(1);
      expect(visible).toHaveLength(0);
    });
  });

  describe('Import/Export SRT', () => {
    let trackId: string;

    beforeEach(() => {
      const { addTrack } = useOpenCutCaptionsStore.getState();
      trackId = addTrack('English', 'en');
    });

    it('exports to SRT format', () => {
      const { addCaption, exportSrt } = useOpenCutCaptionsStore.getState();

      addCaption(trackId, 0, 2, 'Hello');
      addCaption(trackId, 2, 4, 'World');

      const srt = exportSrt(trackId);

      expect(srt).toContain('1\n');
      expect(srt).toContain('2\n');
      expect(srt).toContain('00:00:00,000 --> 00:00:02,000');
      expect(srt).toContain('00:00:02,000 --> 00:00:04,000');
      expect(srt).toContain('Hello');
      expect(srt).toContain('World');
    });

    it('imports SRT format', () => {
      const { importSrt, getCaptionsForTrack } = useOpenCutCaptionsStore.getState();

      const srtContent = `1
00:00:00,000 --> 00:00:02,000
Hello

2
00:00:02,000 --> 00:00:04,000
World
`;

      importSrt(trackId, srtContent);

      const captions = getCaptionsForTrack(trackId);
      expect(captions).toHaveLength(2);
      expect(captions[0].text).toBe('Hello');
      expect(captions[0].startTime).toBe(0);
      expect(captions[1].text).toBe('World');
      expect(captions[1].startTime).toBe(2);
    });
  });

  describe('Import/Export VTT', () => {
    let trackId: string;

    beforeEach(() => {
      const { addTrack } = useOpenCutCaptionsStore.getState();
      trackId = addTrack('English', 'en');
    });

    it('exports to VTT format', () => {
      const { addCaption, exportVtt } = useOpenCutCaptionsStore.getState();

      addCaption(trackId, 0, 2, 'Hello');
      addCaption(trackId, 2, 4, 'World');

      const vtt = exportVtt(trackId);

      expect(vtt).toContain('WEBVTT');
      expect(vtt).toContain('00:00:00.000 --> 00:00:02.000');
      expect(vtt).toContain('00:00:02.000 --> 00:00:04.000');
      expect(vtt).toContain('Hello');
      expect(vtt).toContain('World');
    });

    it('imports VTT format', () => {
      const { importVtt, getCaptionsForTrack } = useOpenCutCaptionsStore.getState();

      const vttContent = `WEBVTT

00:00:00.000 --> 00:00:02.000
Hello

00:00:02.000 --> 00:00:04.000
World
`;

      importVtt(trackId, vttContent);

      const captions = getCaptionsForTrack(trackId);
      expect(captions).toHaveLength(2);
      expect(captions[0].text).toBe('Hello');
      expect(captions[1].text).toBe('World');
    });
  });

  describe('Reset', () => {
    it('clears all state', () => {
      const { addTrack, addCaption, reset } = useOpenCutCaptionsStore.getState();

      const trackId = addTrack('English', 'en');
      addCaption(trackId, 0, 2, 'Hello');

      reset();

      const state = useOpenCutCaptionsStore.getState();
      expect(state.tracks).toHaveLength(0);
      expect(state.captions).toHaveLength(0);
      expect(state.selectedTrackId).toBeNull();
      expect(state.selectedCaptionId).toBeNull();
      expect(state.activeTrackId).toBeNull();
    });
  });
});
