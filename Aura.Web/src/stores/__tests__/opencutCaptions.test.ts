/**
 * OpenCut Captions Store Tests
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { useOpenCutCaptionsStore } from '../opencutCaptions';

describe('OpenCutCaptionsStore', () => {
  beforeEach(() => {
    // Reset store before each test
    useOpenCutCaptionsStore.setState({
      tracks: [],
      selectedCaptionId: null,
      selectedTrackId: null,
      defaultStyle: {
        fontFamily: 'Inter, system-ui, sans-serif',
        fontSize: 24,
        fontWeight: 600,
        color: '#FFFFFF',
        textAlign: 'center',
        backgroundColor: '#000000',
        backgroundPadding: 8,
        strokeColor: '#000000',
        strokeWidth: 2,
        letterSpacing: 0,
        lineHeight: 1.4,
      },
    });
  });

  describe('Track Operations', () => {
    it('should add a track with default settings', () => {
      const { addTrack } = useOpenCutCaptionsStore.getState();

      const trackId = addTrack('English');

      const state = useOpenCutCaptionsStore.getState();
      expect(state.tracks.length).toBe(1);
      expect(state.tracks[0].name).toBe('English');
      expect(state.tracks[0].language).toBe('en');
      expect(state.tracks[0].isDefault).toBe(true);
      expect(state.tracks[0].visible).toBe(true);
      expect(state.tracks[0].locked).toBe(false);
    });

    it('should add a track with custom language', () => {
      const { addTrack } = useOpenCutCaptionsStore.getState();

      addTrack('Spanish', 'es');

      const state = useOpenCutCaptionsStore.getState();
      expect(state.tracks[0].language).toBe('es');
    });

    it('should set first track as default', () => {
      const { addTrack } = useOpenCutCaptionsStore.getState();

      addTrack('English');
      addTrack('Spanish', 'es');

      const state = useOpenCutCaptionsStore.getState();
      expect(state.tracks[0].isDefault).toBe(true);
      expect(state.tracks[1].isDefault).toBe(false);
    });

    it('should remove a track', () => {
      const { addTrack, removeTrack } = useOpenCutCaptionsStore.getState();

      const trackId = addTrack('English');
      expect(useOpenCutCaptionsStore.getState().tracks.length).toBe(1);

      useOpenCutCaptionsStore.getState().removeTrack(trackId);
      expect(useOpenCutCaptionsStore.getState().tracks.length).toBe(0);
    });

    it('should update a track', () => {
      const { addTrack, updateTrack } = useOpenCutCaptionsStore.getState();

      const trackId = addTrack('English');

      useOpenCutCaptionsStore.getState().updateTrack(trackId, { name: 'Updated' });

      const state = useOpenCutCaptionsStore.getState();
      expect(state.tracks[0].name).toBe('Updated');
    });

    it('should clear selected track when removing selected track', () => {
      const { addTrack, selectTrack, removeTrack } = useOpenCutCaptionsStore.getState();

      const trackId = addTrack('English');
      useOpenCutCaptionsStore.getState().selectTrack(trackId);
      expect(useOpenCutCaptionsStore.getState().selectedTrackId).toBe(trackId);

      useOpenCutCaptionsStore.getState().removeTrack(trackId);
      expect(useOpenCutCaptionsStore.getState().selectedTrackId).toBeNull();
    });

    it('should set track visibility', () => {
      const { addTrack, setTrackVisibility } = useOpenCutCaptionsStore.getState();

      const trackId = addTrack('English');
      expect(useOpenCutCaptionsStore.getState().tracks[0].visible).toBe(true);

      useOpenCutCaptionsStore.getState().setTrackVisibility(trackId, false);
      expect(useOpenCutCaptionsStore.getState().tracks[0].visible).toBe(false);
    });

    it('should set track locked state', () => {
      const { addTrack, setTrackLocked } = useOpenCutCaptionsStore.getState();

      const trackId = addTrack('English');
      expect(useOpenCutCaptionsStore.getState().tracks[0].locked).toBe(false);

      useOpenCutCaptionsStore.getState().setTrackLocked(trackId, true);
      expect(useOpenCutCaptionsStore.getState().tracks[0].locked).toBe(true);
    });

    it('should get visible tracks', () => {
      const { addTrack, setTrackVisibility, getVisibleTracks } = useOpenCutCaptionsStore.getState();

      const id1 = addTrack('English');
      const id2 = addTrack('Spanish', 'es');

      useOpenCutCaptionsStore.getState().setTrackVisibility(id1, false);

      const visible = useOpenCutCaptionsStore.getState().getVisibleTracks();
      expect(visible.length).toBe(1);
      expect(visible[0].id).toBe(id2);
    });
  });

  describe('Caption Operations', () => {
    it('should add a caption to a track', () => {
      const { addTrack, addCaption } = useOpenCutCaptionsStore.getState();

      const trackId = addTrack('English');
      const captionId = useOpenCutCaptionsStore.getState().addCaption(trackId, 0, 2, 'Hello world');

      const state = useOpenCutCaptionsStore.getState();
      expect(state.tracks[0].captions.length).toBe(1);
      expect(state.tracks[0].captions[0].text).toBe('Hello world');
      expect(state.tracks[0].captions[0].startTime).toBe(0);
      expect(state.tracks[0].captions[0].endTime).toBe(2);
      expect(state.tracks[0].captions[0].trackId).toBe(trackId);
    });

    it('should sort captions by start time when adding', () => {
      const { addTrack, addCaption } = useOpenCutCaptionsStore.getState();

      const trackId = addTrack('English');
      useOpenCutCaptionsStore.getState().addCaption(trackId, 5, 7, 'Second');
      useOpenCutCaptionsStore.getState().addCaption(trackId, 0, 2, 'First');
      useOpenCutCaptionsStore.getState().addCaption(trackId, 10, 12, 'Third');

      const state = useOpenCutCaptionsStore.getState();
      expect(state.tracks[0].captions[0].text).toBe('First');
      expect(state.tracks[0].captions[1].text).toBe('Second');
      expect(state.tracks[0].captions[2].text).toBe('Third');
    });

    it('should remove a caption', () => {
      const { addTrack, addCaption, removeCaption } = useOpenCutCaptionsStore.getState();

      const trackId = addTrack('English');
      const captionId = useOpenCutCaptionsStore.getState().addCaption(trackId, 0, 2, 'Hello');

      expect(useOpenCutCaptionsStore.getState().tracks[0].captions.length).toBe(1);

      useOpenCutCaptionsStore.getState().removeCaption(captionId);
      expect(useOpenCutCaptionsStore.getState().tracks[0].captions.length).toBe(0);
    });

    it('should update a caption', () => {
      const { addTrack, addCaption, updateCaption } = useOpenCutCaptionsStore.getState();

      const trackId = addTrack('English');
      const captionId = useOpenCutCaptionsStore.getState().addCaption(trackId, 0, 2, 'Hello');

      useOpenCutCaptionsStore.getState().updateCaption(captionId, { text: 'Updated' });

      const state = useOpenCutCaptionsStore.getState();
      expect(state.tracks[0].captions[0].text).toBe('Updated');
    });

    it('should set caption timing', () => {
      const { addTrack, addCaption, setCaptionTiming } = useOpenCutCaptionsStore.getState();

      const trackId = addTrack('English');
      const captionId = useOpenCutCaptionsStore.getState().addCaption(trackId, 0, 2, 'Hello');

      useOpenCutCaptionsStore.getState().setCaptionTiming(captionId, 1, 4);

      const state = useOpenCutCaptionsStore.getState();
      expect(state.tracks[0].captions[0].startTime).toBe(1);
      expect(state.tracks[0].captions[0].endTime).toBe(4);
    });

    it('should set caption style', () => {
      const { addTrack, addCaption, setCaptionStyle } = useOpenCutCaptionsStore.getState();

      const trackId = addTrack('English');
      const captionId = useOpenCutCaptionsStore.getState().addCaption(trackId, 0, 2, 'Hello');

      useOpenCutCaptionsStore
        .getState()
        .setCaptionStyle(captionId, { fontSize: 32, color: '#FF0000' });

      const state = useOpenCutCaptionsStore.getState();
      expect(state.tracks[0].captions[0].style?.fontSize).toBe(32);
      expect(state.tracks[0].captions[0].style?.color).toBe('#FF0000');
    });

    it('should clear selected caption when removing it', () => {
      const { addTrack, addCaption, selectCaption, removeCaption } =
        useOpenCutCaptionsStore.getState();

      const trackId = addTrack('English');
      const captionId = useOpenCutCaptionsStore.getState().addCaption(trackId, 0, 2, 'Hello');

      useOpenCutCaptionsStore.getState().selectCaption(captionId);
      expect(useOpenCutCaptionsStore.getState().selectedCaptionId).toBe(captionId);

      useOpenCutCaptionsStore.getState().removeCaption(captionId);
      expect(useOpenCutCaptionsStore.getState().selectedCaptionId).toBeNull();
    });
  });

  describe('Query Operations', () => {
    it('should get caption at time', () => {
      const { addTrack, addCaption, getCaptionAtTime } = useOpenCutCaptionsStore.getState();

      const trackId = addTrack('English');
      useOpenCutCaptionsStore.getState().addCaption(trackId, 0, 2, 'First');
      useOpenCutCaptionsStore.getState().addCaption(trackId, 3, 5, 'Second');

      const caption = useOpenCutCaptionsStore.getState().getCaptionAtTime(trackId, 1);
      expect(caption?.text).toBe('First');

      const caption2 = useOpenCutCaptionsStore.getState().getCaptionAtTime(trackId, 4);
      expect(caption2?.text).toBe('Second');

      const caption3 = useOpenCutCaptionsStore.getState().getCaptionAtTime(trackId, 2.5);
      expect(caption3).toBeUndefined();
    });

    it('should get caption by ID', () => {
      const { addTrack, addCaption, getCaptionById } = useOpenCutCaptionsStore.getState();

      const trackId = addTrack('English');
      const captionId = useOpenCutCaptionsStore.getState().addCaption(trackId, 0, 2, 'Hello');

      const caption = useOpenCutCaptionsStore.getState().getCaptionById(captionId);
      expect(caption?.text).toBe('Hello');

      const notFound = useOpenCutCaptionsStore.getState().getCaptionById('non-existent');
      expect(notFound).toBeUndefined();
    });

    it('should get track by ID', () => {
      const { addTrack, getTrackById } = useOpenCutCaptionsStore.getState();

      const trackId = addTrack('English');

      const track = useOpenCutCaptionsStore.getState().getTrackById(trackId);
      expect(track?.name).toBe('English');

      const notFound = useOpenCutCaptionsStore.getState().getTrackById('non-existent');
      expect(notFound).toBeUndefined();
    });

    it('should get captions for track', () => {
      const { addTrack, addCaption, getCaptionsForTrack } = useOpenCutCaptionsStore.getState();

      const trackId = addTrack('English');
      useOpenCutCaptionsStore.getState().addCaption(trackId, 0, 2, 'First');
      useOpenCutCaptionsStore.getState().addCaption(trackId, 3, 5, 'Second');

      const captions = useOpenCutCaptionsStore.getState().getCaptionsForTrack(trackId);
      expect(captions.length).toBe(2);
    });
  });

  describe('Split and Merge Operations', () => {
    it('should split a caption', () => {
      const { addTrack, addCaption, splitCaption } = useOpenCutCaptionsStore.getState();

      const trackId = addTrack('English');
      const captionId = useOpenCutCaptionsStore
        .getState()
        .addCaption(trackId, 0, 4, 'Hello world from test');

      const result = useOpenCutCaptionsStore.getState().splitCaption(captionId, 2);

      expect(result).not.toBeNull();
      const [id1, id2] = result!;

      const state = useOpenCutCaptionsStore.getState();
      expect(state.tracks[0].captions.length).toBe(2);

      const caption1 = state.tracks[0].captions.find((c) => c.id === id1);
      const caption2 = state.tracks[0].captions.find((c) => c.id === id2);

      expect(caption1?.endTime).toBe(2);
      expect(caption2?.startTime).toBe(2);
      expect(caption1?.text).toBe('Hello world');
      expect(caption2?.text).toBe('from test');
    });

    it('should return null when split time is outside caption bounds', () => {
      const { addTrack, addCaption, splitCaption } = useOpenCutCaptionsStore.getState();

      const trackId = addTrack('English');
      const captionId = useOpenCutCaptionsStore.getState().addCaption(trackId, 0, 4, 'Hello');

      const result1 = useOpenCutCaptionsStore.getState().splitCaption(captionId, 0);
      expect(result1).toBeNull();

      const result2 = useOpenCutCaptionsStore.getState().splitCaption(captionId, 4);
      expect(result2).toBeNull();

      const result3 = useOpenCutCaptionsStore.getState().splitCaption(captionId, 5);
      expect(result3).toBeNull();
    });

    it('should merge two captions', () => {
      const { addTrack, addCaption, mergeCaptions } = useOpenCutCaptionsStore.getState();

      const trackId = addTrack('English');
      const id1 = useOpenCutCaptionsStore.getState().addCaption(trackId, 0, 2, 'Hello');
      const id2 = useOpenCutCaptionsStore.getState().addCaption(trackId, 2, 4, 'world');

      const mergedId = useOpenCutCaptionsStore.getState().mergeCaptions(id1, id2);

      expect(mergedId).toBe(id1);

      const state = useOpenCutCaptionsStore.getState();
      expect(state.tracks[0].captions.length).toBe(1);
      expect(state.tracks[0].captions[0].text).toBe('Hello world');
      expect(state.tracks[0].captions[0].startTime).toBe(0);
      expect(state.tracks[0].captions[0].endTime).toBe(4);
    });

    it('should return null when merging non-existent captions', () => {
      const { addTrack, addCaption, mergeCaptions } = useOpenCutCaptionsStore.getState();

      const trackId = addTrack('English');
      const id1 = useOpenCutCaptionsStore.getState().addCaption(trackId, 0, 2, 'Hello');

      const result = useOpenCutCaptionsStore.getState().mergeCaptions(id1, 'non-existent');
      expect(result).toBeNull();
    });
  });

  describe('SRT Import/Export', () => {
    it('should import SRT content', () => {
      const { addTrack, importSRT } = useOpenCutCaptionsStore.getState();

      const trackId = addTrack('English');
      const srtContent = `1
00:00:00,000 --> 00:00:02,000
Hello world

2
00:00:03,000 --> 00:00:05,500
This is a test`;

      useOpenCutCaptionsStore.getState().importSRT(trackId, srtContent);

      const state = useOpenCutCaptionsStore.getState();
      expect(state.tracks[0].captions.length).toBe(2);
      expect(state.tracks[0].captions[0].text).toBe('Hello world');
      expect(state.tracks[0].captions[0].startTime).toBe(0);
      expect(state.tracks[0].captions[0].endTime).toBe(2);
      expect(state.tracks[0].captions[1].text).toBe('This is a test');
      expect(state.tracks[0].captions[1].startTime).toBe(3);
      expect(state.tracks[0].captions[1].endTime).toBe(5.5);
    });

    it('should export SRT content', () => {
      const { addTrack, addCaption, exportSRT } = useOpenCutCaptionsStore.getState();

      const trackId = addTrack('English');
      useOpenCutCaptionsStore.getState().addCaption(trackId, 0, 2, 'Hello');
      useOpenCutCaptionsStore.getState().addCaption(trackId, 3, 5.5, 'World');

      const srt = useOpenCutCaptionsStore.getState().exportSRT(trackId);

      expect(srt).toContain('1\n00:00:00,000 --> 00:00:02,000\nHello');
      expect(srt).toContain('2\n00:00:03,000 --> 00:00:05,500\nWorld');
    });

    it('should return empty string when exporting non-existent track', () => {
      const { exportSRT } = useOpenCutCaptionsStore.getState();

      const result = exportSRT('non-existent');
      expect(result).toBe('');
    });
  });

  describe('VTT Import/Export', () => {
    it('should import VTT content', () => {
      const { addTrack, importVTT } = useOpenCutCaptionsStore.getState();

      const trackId = addTrack('English');
      const vttContent = `WEBVTT

00:00:00.000 --> 00:00:02.000
Hello world

00:00:03.000 --> 00:00:05.500
This is a test`;

      useOpenCutCaptionsStore.getState().importVTT(trackId, vttContent);

      const state = useOpenCutCaptionsStore.getState();
      expect(state.tracks[0].captions.length).toBe(2);
      expect(state.tracks[0].captions[0].text).toBe('Hello world');
      expect(state.tracks[0].captions[0].startTime).toBe(0);
      expect(state.tracks[0].captions[0].endTime).toBe(2);
      expect(state.tracks[0].captions[1].text).toBe('This is a test');
      expect(state.tracks[0].captions[1].startTime).toBe(3);
      expect(state.tracks[0].captions[1].endTime).toBe(5.5);
    });

    it('should export VTT content', () => {
      const { addTrack, addCaption, exportVTT } = useOpenCutCaptionsStore.getState();

      const trackId = addTrack('English');
      useOpenCutCaptionsStore.getState().addCaption(trackId, 0, 2, 'Hello');
      useOpenCutCaptionsStore.getState().addCaption(trackId, 3, 5.5, 'World');

      const vtt = useOpenCutCaptionsStore.getState().exportVTT(trackId);

      expect(vtt).toContain('WEBVTT');
      expect(vtt).toContain('00:00:00.000 --> 00:00:02.000\nHello');
      expect(vtt).toContain('00:00:03.000 --> 00:00:05.500\nWorld');
    });

    it('should return empty string when exporting non-existent track', () => {
      const { exportVTT } = useOpenCutCaptionsStore.getState();

      const result = exportVTT('non-existent');
      expect(result).toBe('');
    });
  });

  describe('Default Style Operations', () => {
    it('should update default style', () => {
      const { setDefaultStyle } = useOpenCutCaptionsStore.getState();

      useOpenCutCaptionsStore.getState().setDefaultStyle({ fontSize: 32, color: '#FF0000' });

      const state = useOpenCutCaptionsStore.getState();
      expect(state.defaultStyle.fontSize).toBe(32);
      expect(state.defaultStyle.color).toBe('#FF0000');
      // Other properties should remain unchanged
      expect(state.defaultStyle.fontFamily).toBe('Inter, system-ui, sans-serif');
    });
  });

  describe('Selection Operations', () => {
    it('should select a caption', () => {
      const { addTrack, addCaption, selectCaption } = useOpenCutCaptionsStore.getState();

      const trackId = addTrack('English');
      const captionId = useOpenCutCaptionsStore.getState().addCaption(trackId, 0, 2, 'Hello');

      useOpenCutCaptionsStore.getState().selectCaption(captionId);

      expect(useOpenCutCaptionsStore.getState().selectedCaptionId).toBe(captionId);
    });

    it('should clear caption selection', () => {
      const { addTrack, addCaption, selectCaption } = useOpenCutCaptionsStore.getState();

      const trackId = addTrack('English');
      const captionId = useOpenCutCaptionsStore.getState().addCaption(trackId, 0, 2, 'Hello');

      useOpenCutCaptionsStore.getState().selectCaption(captionId);
      useOpenCutCaptionsStore.getState().selectCaption(null);

      expect(useOpenCutCaptionsStore.getState().selectedCaptionId).toBeNull();
    });

    it('should select a track', () => {
      const { addTrack, selectTrack } = useOpenCutCaptionsStore.getState();

      const trackId = addTrack('English');

      useOpenCutCaptionsStore.getState().selectTrack(trackId);

      expect(useOpenCutCaptionsStore.getState().selectedTrackId).toBe(trackId);
    });

    it('should clear track selection', () => {
      const { addTrack, selectTrack } = useOpenCutCaptionsStore.getState();

      const trackId = addTrack('English');

      useOpenCutCaptionsStore.getState().selectTrack(trackId);
      useOpenCutCaptionsStore.getState().selectTrack(null);

      expect(useOpenCutCaptionsStore.getState().selectedTrackId).toBeNull();
    });
  });
});
